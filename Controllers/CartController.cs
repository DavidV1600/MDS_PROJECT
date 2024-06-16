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
            public string CarrefourTotal { get; set; }
            public string KauflandTotal { get; set; }
        }

        public class CartItem
        {
            public string ItemName { get; set; }
            public string Quantity { get; set; }
            public string Unit { get; set; }
            public string CarrefourMessage { get; set; }
            public string KauflandMessage { get; set; }
            public string CarrefourStoreItemName { get; set; }
            public string KauflandStoreItemName { get; set; }
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
        public async Task<IActionResult> Index(List<CartItem> items, bool exactItemName = false)
        {
            var cartViewModel = new CartViewModel { Items = items };
            decimal carrefourTotal = 0;
            decimal kauflandTotal = 0;

            foreach (var item in items)
            {
                Stopwatch stopwatch = Stopwatch.StartNew();
                var existingProducts = _db.Products
                                          .Where(p => p.Searched == item.ItemName)
                                          .ToList();
                stopwatch.Stop();
                Debug.WriteLine($"Database query for {item.ItemName} took {stopwatch.ElapsedMilliseconds} ms");

                var carrefourMessage = "Not found in Carrefour";
                var kauflandMessage = "Not found in Kaufland";
                var carrefourStoreItemName = string.Empty;
                var kauflandStoreItemName = string.Empty;

                if (existingProducts.Any())
                {
                    var carrefourItems = existingProducts.Where(p => p.Store == "Carrefour").Select(p => new ItemResult(p)).ToList();
                    var kauflandItems = existingProducts.Where(p => p.Store == "Kaufland").Select(p => new ItemResult(p)).ToList();

                    carrefourItems = FilterItems(carrefourItems, item.Quantity);
                    kauflandItems = FilterItems(kauflandItems, item.Quantity);

                    var cheapestCarrefourItem = carrefourItems.OrderBy(p => ParsePrice(p.Price)).FirstOrDefault();
                    var cheapestKauflandItem = kauflandItems.OrderBy(p => ParsePrice(p.Price)).FirstOrDefault();

                    if (cheapestCarrefourItem != null)
                    {
                        carrefourTotal += ParsePrice(cheapestCarrefourItem.Price);
                        carrefourMessage = $"{cheapestCarrefourItem.ItemName}: {cheapestCarrefourItem.Price} Lei";
                        carrefourStoreItemName = cheapestCarrefourItem.ItemName;
                    }

                    if (cheapestKauflandItem != null)
                    {
                        kauflandTotal += ParsePrice(cheapestKauflandItem.Price);
                        kauflandMessage = $"{cheapestKauflandItem.ItemName}: {cheapestKauflandItem.Price} Lei";
                        kauflandStoreItemName = cheapestKauflandItem.ItemName;
                    }
                }
                else
                {
                    var carrefourTask = GetSearchResult("Carrefour.py", item.ItemName, exactItemName);
                    var kauflandTask = GetSearchResult("Kaufland.py", item.ItemName, exactItemName);

                    await Task.WhenAll(carrefourTask, kauflandTask);

                    var carrefourResults = ParseResults(carrefourTask.Result, "Carrefour");
                    var kauflandResults = ParseResults(kauflandTask.Result, "Kaufland");

                    carrefourResults = FilterItems(carrefourResults, item.Quantity);
                    kauflandResults = FilterItems(kauflandResults, item.Quantity);

                    var cheapestCarrefourItem = carrefourResults.OrderBy(p => ParsePrice(p.Price)).FirstOrDefault();
                    var cheapestKauflandItem = kauflandResults.OrderBy(p => ParsePrice(p.Price)).FirstOrDefault();

                    if (cheapestCarrefourItem != null)
                    {
                        carrefourTotal += ParsePrice(cheapestCarrefourItem.Price);
                        carrefourMessage = $"{cheapestCarrefourItem.ItemName}: {cheapestCarrefourItem.Price} Lei";
                        carrefourStoreItemName = cheapestCarrefourItem.ItemName;
                        SaveProductToDatabase(cheapestCarrefourItem, item.ItemName);
                    }

                    if (cheapestKauflandItem != null)
                    {
                        kauflandTotal += ParsePrice(cheapestKauflandItem.Price);
                        kauflandMessage = $"{cheapestKauflandItem.ItemName}: {cheapestKauflandItem.Price} Lei";
                        kauflandStoreItemName = cheapestKauflandItem.ItemName;
                        SaveProductToDatabase(cheapestKauflandItem, item.ItemName);
                    }
                }

                item.CarrefourMessage = carrefourMessage;
                item.KauflandMessage = kauflandMessage;
                item.CarrefourStoreItemName = carrefourStoreItemName;
                item.KauflandStoreItemName = kauflandStoreItemName;
            }

            cartViewModel.CarrefourTotal = carrefourTotal.ToString("F2", CultureInfo.InvariantCulture);
            cartViewModel.KauflandTotal = kauflandTotal.ToString("F2", CultureInfo.InvariantCulture);

            return View(cartViewModel);
        }

        private async Task<string> GetSearchResult(string scriptPath, string query, bool exactItemName)
        {
            string pythonExePath = _configuration["PathVariables:PythonExePath"];
            string scriptFolderPath = _configuration["PathVariables:ScriptFolderPath"];
            string fullScriptPath = Path.Combine(scriptFolderPath, exactItemName ? scriptPath.Replace(".py", "Exact.py") : scriptPath);

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

        private List<ItemResult> FilterItems(List<ItemResult> items, string quantity)
        {
            var normalizedQuantities = GetEquivalentQuantities(quantity);
            return items.Where(p => normalizedQuantities.Contains(NormalizeQuantity(p.Quantity))).ToList();
        }

        private List<string> GetEquivalentQuantities(string quantity)
        {
            var normalizedQuantity = NormalizeQuantity(quantity);
            var equivalents = new List<string> { normalizedQuantity };

            if (decimal.TryParse(normalizedQuantity, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal qty))
            {
                equivalents.Add((qty * 1000).ToString("F0", CultureInfo.InvariantCulture));
                equivalents.Add((qty / 1000).ToString("F3", CultureInfo.InvariantCulture));
                equivalents.Add(qty.ToString("F1", CultureInfo.InvariantCulture).Replace('.', ','));
                equivalents.Add((qty * 1000).ToString(CultureInfo.InvariantCulture));
                equivalents.Add((qty / 1000).ToString(CultureInfo.InvariantCulture));
            }

            return equivalents;
        }

        private string NormalizeQuantity(string quantity)
        {
            return quantity.Replace(',', '.');
        }

        private decimal ParsePrice(string price)
        {
            var cleanedPrice = Regex.Replace(price, @"[^\d,\.]", "");
            return decimal.Parse(cleanedPrice.Replace(',', '.'), CultureInfo.InvariantCulture);
        }

        private List<ItemResult> ParseResults(string results, string store)
        {
            string pattern = store == "Carrefour"
                ? @"Product: (.+?) (\d*[\.,]?\d+)\s*(\w+), Price: (\d+[\.,]?\d*) Lei"
                : @"Product Name: (.+?)\r\nProduct Subtitle: (.+?)\r\nProduct Price: (\d+[\.,]?\d*)\r\nProduct Quantity: (.+)";

            MatchCollection matches = Regex.Matches(results, pattern);
            return matches.Cast<Match>().Select(m =>
            {
                if (store == "Carrefour")
                {
                    if (m.Groups.Count != 5)
                    {
                        Debug.WriteLine($"Unexpected match format for Carrefour: {m.Value}");
                        return null;
                    }
                    return new ItemResult
                    {
                        ItemName = m.Groups[1].Value.Trim(),
                        Quantity = m.Groups[2].Value.Trim(),
                        MeasureQuantity = m.Groups[3].Value.Trim(),
                        Price = m.Groups[4].Value.Trim(),
                        Store = store
                    };
                }
                else
                {
                    if (m.Groups.Count != 5)
                    {
                        Debug.WriteLine($"Unexpected match format for Kaufland: {m.Value}");
                        return null;
                    }
                    var quantitySplit = m.Groups[4].Value.Trim().Split(' ');
                    if (quantitySplit.Length != 2)
                    {
                        Debug.WriteLine($"Unexpected quantity format for Kaufland: {m.Groups[4].Value}");
                        return null;
                    }
                    return new ItemResult
                    {
                        ItemName = m.Groups[1].Value.Trim() + " " + m.Groups[2].Value.Trim(),
                        Quantity = quantitySplit[0].Trim(),
                        MeasureQuantity = quantitySplit[1].Trim(),
                        Price = m.Groups[3].Value.Trim() + " Lei",
                        Store = store
                    };
                }
            }).Where(item => item != null).ToList();
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
