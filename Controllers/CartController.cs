using MDS_PROJECT.Data;
using MDS_PROJECT.Models;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Globalization;

namespace MDS_PROJECT.Controllers
{
    public class CartController : Controller
    {
        public class CartViewModel
        {
            public List<CartItem> Items { get; set; } = new List<CartItem>();
            public decimal CarrefourTotal { get; set; }
            public decimal KauflandTotal { get; set; }
        }

        public class CartItem
        {
            public string ItemName { get; set; }
            public int Quantity { get; set; }
            public string Unit { get; set; }
        }

        public class ItemResult
        {
            public string ItemName { get; set; }
            public string Quantity { get; set; }
            public string MeasureQuantity { get; set; }
            public string Price { get; set; }
            public string Store { get; set; }
            public string Searched { get; set; }

            public ItemResult() { }

            public ItemResult(Product product)
            {
                ItemName = product.ItemName;
                Quantity = product.Quantity;
                MeasureQuantity = product.MeasureQuantity;
                Price = product.Price;
                Store = product.Store;
                Searched = product.Searched;
            }
        }

        private readonly ApplicationDbContext _db;
        private readonly IConfiguration _configuration;

        public CartController(ApplicationDbContext db, IConfiguration configuration)
        {
            _db = db;
            _configuration = configuration;
        }

        public IActionResult Index()
        {
            var viewModel = new CartViewModel();
            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> Index(List<CartItem> items)
        {
            var cartViewModel = new CartViewModel { Items = items };

            foreach (var item in items)
            {
                var existingProducts = _db.Products.Where(p => p.ItemName == item.ItemName).ToList();
                var existingItemResults = existingProducts.Select(p => new ItemResult(p)).ToList();

                if (existingItemResults.Any())
                {
                    Debug.WriteLine($"Found items in the database for {item.ItemName}:");
                    foreach (var product in existingItemResults)
                    {
                        Debug.WriteLine($"{product.Store}: {product.ItemName} - {product.Quantity} {product.MeasureQuantity} - {product.Price} Lei");
                    }

                    var carrefourItems = existingItemResults.Where(p => p.Store == "Carrefour").ToList();
                    var kauflandItems = existingItemResults.Where(p => p.Store == "Kaufland").ToList();

                    carrefourItems = FilterItems(carrefourItems, item.Quantity);
                    kauflandItems = FilterItems(kauflandItems, item.Quantity);

                    var cheapestCarrefourItem = carrefourItems.OrderBy(p => ParsePrice(p.Price)).FirstOrDefault();
                    var cheapestKauflandItem = kauflandItems.OrderBy(p => ParsePrice(p.Price)).FirstOrDefault();

                    if (cheapestCarrefourItem != null)
                    {
                        cartViewModel.CarrefourTotal += ParsePrice(cheapestCarrefourItem.Price);
                    }

                    if (cheapestKauflandItem != null)
                    {
                        cartViewModel.KauflandTotal += ParsePrice(cheapestKauflandItem.Price);
                    }
                }
                else
                {
                    var carrefourTask = GetSearchResult("Carrefour.py", item.ItemName);
                    var kauflandTask = GetSearchResult("Kaufland.py", item.ItemName);

                    await Task.WhenAll(carrefourTask, kauflandTask);

                    var carrefourResults = ParseResults(carrefourTask.Result, "Carrefour");
                    var kauflandResults = ParseResults(kauflandTask.Result, "Kaufland");

                    Debug.WriteLine($"Found items from Python script for {item.ItemName}:");
                    Debug.WriteLine("Carrefour:");
                    foreach (var result in carrefourResults)
                    {
                        Debug.WriteLine($"{result.ItemName} - {result.Quantity} {result.MeasureQuantity} - {result.Price} Lei");
                    }

                    Debug.WriteLine("Kaufland:");
                    foreach (var result in kauflandResults)
                    {
                        Debug.WriteLine($"{result.ItemName} - {result.Quantity} {result.MeasureQuantity} - {result.Price} Lei");
                    }

                    carrefourResults = FilterItems(carrefourResults, item.Quantity);
                    kauflandResults = FilterItems(kauflandResults, item.Quantity);

                    var cheapestCarrefourItem = carrefourResults.OrderBy(p => ParsePrice(p.Price)).FirstOrDefault();
                    var cheapestKauflandItem = kauflandResults.OrderBy(p => ParsePrice(p.Price)).FirstOrDefault();

                    if (cheapestCarrefourItem != null)
                    {
                        cartViewModel.CarrefourTotal += ParsePrice(cheapestCarrefourItem.Price);
                        SaveProductToDatabase(cheapestCarrefourItem, item.ItemName);
                    }

                    if (cheapestKauflandItem != null)
                    {
                        cartViewModel.KauflandTotal += ParsePrice(cheapestKauflandItem.Price);
                        SaveProductToDatabase(cheapestKauflandItem, item.ItemName);
                    }
                }
            }

            return View(cartViewModel);
        }

        private List<ItemResult> FilterItems(List<ItemResult> items, int quantity)
        {
            return items.Where(p => int.TryParse(p.Quantity, out int itemQuantity) && itemQuantity >= quantity).ToList();
        }

        private decimal ParsePrice(string price)
        {
            // Remove any non-numeric characters (except for the decimal separator)
            var cleanedPrice = Regex.Replace(price, @"[^\d,\.]", "");
            return decimal.Parse(cleanedPrice, CultureInfo.InvariantCulture);
        }

        private async Task<string> GetSearchResult(string scriptPath, string query)
        {
            string pythonExePath = _configuration["PathVariables:PythonExePath"];
            string scriptFolderPath = _configuration["PathVariables:ScriptFolderPath"];
            string fullScriptPath = Path.Combine(scriptFolderPath, scriptPath);

            Debug.WriteLine($"Executing command: {pythonExePath} {fullScriptPath} {query}");

            ProcessStartInfo start = new ProcessStartInfo
            {
                FileName = pythonExePath,
                Arguments = $"{fullScriptPath} \"{query}\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                StandardOutputEncoding = Encoding.UTF8
            };

            using (Process process = Process.Start(start))
            {
                using (StreamReader outputReader = process.StandardOutput)
                using (StreamReader errorReader = process.StandardError)
                {
                    string result = await outputReader.ReadToEndAsync();
                    string error = await errorReader.ReadToEndAsync();

                    if (process.ExitCode != 0)
                    {
                        Debug.WriteLine($"Python script error output: {error}");
                        throw new Exception($"Python script error: {error}");
                    }

                    return result;
                }
            }
        }

        private List<ItemResult> ParseResults(string results, string store)
        {
            string pattern = store == "Carrefour"
                ? @"Product: (.+?) (\d*[\.,]?\d+)\s*(\w+), Price: (\d+[\.,]?\d*) Lei"
                : @"Product Name: (.+?)\r\nProduct Subtitle: (.+?)\r\nProduct Price: (\d+[\.,]?\d*)\r\nProduct Quantity: (.+)";

            MatchCollection matches = Regex.Matches(results, pattern);
            return matches.Cast<Match>().Select(m => new ItemResult
            {
                ItemName = store == "Carrefour" ? m.Groups[1].Value.Trim() : m.Groups[1].Value.Trim() + " " + m.Groups[2].Value.Trim(),
                Quantity = store == "Carrefour" ? m.Groups[2].Value.Trim() : m.Groups[4].Value.Split(' ')[0].Trim(),
                MeasureQuantity = store == "Carrefour" ? m.Groups[3].Value.Trim() : m.Groups[4].Value.Split(' ')[1].Trim(),
                Price = m.Groups[4].Value.Trim() + " Lei",
                Store = store
            }).ToList();
        }

        private void SaveProductToDatabase(ItemResult itemResult, string searchedItem)
        {
            var product = new Product
            {
                ItemName = itemResult.ItemName,
                Quantity = itemResult.Quantity,
                MeasureQuantity = itemResult.MeasureQuantity,
                Price = itemResult.Price,
                Store = itemResult.Store,
                Searched = searchedItem
            };

            if (!_db.Products.Any(p => p.ItemName == product.ItemName && p.Quantity == product.Quantity && p.MeasureQuantity == product.MeasureQuantity && p.Store == product.Store))
            {
                _db.Products.Add(product);
                _db.SaveChanges();
            }
        }
    }
}
