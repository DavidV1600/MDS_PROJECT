using MDS_PROJECT.Data;
using MDS_PROJECT.Models;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace MDS_PROJECT.Controllers
{
    public class CartController : Controller
    {
        public class CartViewModel
        {
            public List<CartItem> Items { get; set; } = new List<CartItem>();
            public string CarrefourTotal { get; set; }
            public string KauflandTotal { get; set; }
            public string AuchanTotal { get; set; }
            public string MegaTotal { get; set; }
        }

        public class CartItem
        {
            public string ItemName { get; set; }
            public string Quantity { get; set; }
            public string Unit { get; set; }
            public string CarrefourMessage { get; set; }
            public string KauflandMessage { get; set; }
            public string AuchanMessage { get; set; }
            public string MegaMessage { get; set; }
            public string CarrefourStoreItemName { get; set; }
            public string KauflandStoreItemName { get; set; }
            public string AuchanStoreItemName { get; set; }
            public string MegaStoreItemName { get; set; }
            public int Multiplier { get; set; } = 1;
        }

        public class ItemResult
        {
            public string ItemName { get; set; }
            public string Quantity { get; set; }
            public string MeasureQuantity { get; set; }
            public string Price { get; set; }
            public string Store { get; set; }
            public string Searched { get; set; }
            public int Multiplier { get; set; } = 1;

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
            await CalculateTotals(cartViewModel, items, exactItemName);
            return View(cartViewModel);
        }

        private async Task CalculateTotals(CartViewModel cartViewModel, List<CartItem> items, bool exactItemName)
        {
            decimal carrefourTotal = 0;
            decimal kauflandTotal = 0;
            decimal auchanTotal = 0;
            decimal megaTotal = 0;

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
                var auchanMessage = "Not found in Auchan";
                var megaMessage = "Not found in Mega";
                var carrefourStoreItemName = string.Empty;
                var kauflandStoreItemName = string.Empty;
                var auchanStoreItemName = string.Empty;
                var megaStoreItemName = string.Empty;

                if (existingProducts.Any())
                {
                    var carrefourItems = existingProducts.Where(p => p.Store == "Carrefour").Select(p => new ItemResult(p)).ToList();
                    var kauflandItems = existingProducts.Where(p => p.Store == "Kaufland").Select(p => new ItemResult(p)).ToList();
                    var auchanItems = existingProducts.Where(p => p.Store == "Auchan").Select(p => new ItemResult(p)).ToList();
                    var megaItems = existingProducts.Where(p => p.Store == "Mega").Select(p => new ItemResult(p)).ToList();

                    carrefourItems = FilterItems(carrefourItems, item.Quantity);
                    kauflandItems = FilterItems(kauflandItems, item.Quantity);
                    auchanItems = FilterItems(auchanItems, item.Quantity);
                    megaItems = FilterItems(megaItems, item.Quantity);

                    var cheapestCarrefourItem = carrefourItems.OrderBy(p => TryParsePrice(p.Price)).FirstOrDefault();
                    var cheapestKauflandItem = kauflandItems.OrderBy(p => TryParsePrice(p.Price)).FirstOrDefault();
                    var cheapestAuchanItem = auchanItems.OrderBy(p => TryParsePrice(p.Price)).FirstOrDefault();
                    var cheapestMegaItem = megaItems.OrderBy(p => TryParsePrice(p.Price)).FirstOrDefault();

                    if (cheapestCarrefourItem != null)
                    {
                        carrefourTotal += TryParsePrice(cheapestCarrefourItem.Price) * item.Multiplier;
                        carrefourMessage = $"{cheapestCarrefourItem.ItemName}: {cheapestCarrefourItem.Price} Lei";
                        carrefourStoreItemName = cheapestCarrefourItem.ItemName;
                    }

                    if (cheapestKauflandItem != null)
                    {
                        kauflandTotal += TryParsePrice(cheapestKauflandItem.Price) * item.Multiplier;
                        kauflandMessage = $"{cheapestKauflandItem.ItemName}: {cheapestKauflandItem.Price} Lei";
                        kauflandStoreItemName = cheapestKauflandItem.ItemName;
                    }

                    if (cheapestAuchanItem != null)
                    {
                        auchanTotal += TryParsePrice(cheapestAuchanItem.Price) * item.Multiplier;
                        auchanMessage = $"{cheapestAuchanItem.ItemName}: {cheapestAuchanItem.Price} Lei";
                        auchanStoreItemName = cheapestAuchanItem.ItemName;
                    }

                    if (cheapestMegaItem != null)
                    {
                        megaTotal += TryParsePrice(cheapestMegaItem.Price) * item.Multiplier;
                        megaMessage = $"{cheapestMegaItem.ItemName}: {cheapestMegaItem.Price} Lei";
                        megaStoreItemName = cheapestMegaItem.ItemName;
                    }
                }
                else
                {
                    var carrefourTask = GetSearchResult("Carrefour.py", item.ItemName, exactItemName);
                    var kauflandTask = GetSearchResult("Kaufland.py", item.ItemName, exactItemName);
                    var auchanTask = GetSearchResult("Auchan.py", item.ItemName, exactItemName);
                    var megaTask = GetSearchResult("Mega.py", item.ItemName, exactItemName);

                    await Task.WhenAll(carrefourTask, kauflandTask, auchanTask, megaTask);

                    var carrefourResults = ParseResults(carrefourTask.Result, "Carrefour");
                    var kauflandResults = ParseResults(kauflandTask.Result, "Kaufland");
                    var auchanResults = ParseAuchanResults(auchanTask.Result);
                    var megaResults = ParseMegaResults(megaTask.Result);

                    carrefourResults = FilterItems(carrefourResults, item.Quantity);
                    kauflandResults = FilterItems(kauflandResults, item.Quantity);
                    auchanResults = FilterItems(auchanResults, item.Quantity);
                    megaResults = FilterItems(megaResults, item.Quantity);

                    var cheapestCarrefourItem = carrefourResults.OrderBy(p => TryParsePrice(p.Price)).FirstOrDefault();
                    var cheapestKauflandItem = kauflandResults.OrderBy(p => TryParsePrice(p.Price)).FirstOrDefault();
                    var cheapestAuchanItem = auchanResults.OrderBy(p => TryParsePrice(p.Price)).FirstOrDefault();
                    var cheapestMegaItem = megaResults.OrderBy(p => TryParsePrice(p.Price)).FirstOrDefault();

                    if (cheapestCarrefourItem != null)
                    {
                        carrefourTotal += TryParsePrice(cheapestCarrefourItem.Price) * item.Multiplier;
                        carrefourMessage = $"{cheapestCarrefourItem.ItemName}: {cheapestCarrefourItem.Price} Lei";
                        carrefourStoreItemName = cheapestCarrefourItem.ItemName;
                        SaveProductToDatabase(cheapestCarrefourItem, item.ItemName);
                    }

                    if (cheapestKauflandItem != null)
                    {
                        kauflandTotal += TryParsePrice(cheapestKauflandItem.Price) * item.Multiplier;
                        kauflandMessage = $"{cheapestKauflandItem.ItemName}: {cheapestKauflandItem.Price} Lei";
                        kauflandStoreItemName = cheapestKauflandItem.ItemName;
                        SaveProductToDatabase(cheapestKauflandItem, item.ItemName);
                    }

                    if (cheapestAuchanItem != null)
                    {
                        auchanTotal += TryParsePrice(cheapestAuchanItem.Price) * item.Multiplier;
                        auchanMessage = $"{cheapestAuchanItem.ItemName}: {cheapestAuchanItem.Price} Lei";
                        auchanStoreItemName = cheapestAuchanItem.ItemName;
                        SaveProductToDatabase(cheapestAuchanItem, item.ItemName);
                    }

                    if (cheapestMegaItem != null)
                    {
                        megaTotal += TryParsePrice(cheapestMegaItem.Price) * item.Multiplier;
                        megaMessage = $"{cheapestMegaItem.ItemName}: {cheapestMegaItem.Price} Lei";
                        megaStoreItemName = cheapestMegaItem.ItemName;
                        SaveProductToDatabase(cheapestMegaItem, item.ItemName);
                    }
                }

                item.CarrefourMessage = carrefourMessage;
                item.KauflandMessage = kauflandMessage;
                item.AuchanMessage = auchanMessage;
                item.MegaMessage = megaMessage;
                item.CarrefourStoreItemName = carrefourStoreItemName;
                item.KauflandStoreItemName = kauflandStoreItemName;
                item.AuchanStoreItemName = auchanStoreItemName;
                item.MegaStoreItemName = megaStoreItemName;
            }

            cartViewModel.CarrefourTotal = carrefourTotal.ToString("F2", CultureInfo.InvariantCulture);
            cartViewModel.KauflandTotal = kauflandTotal.ToString("F2", CultureInfo.InvariantCulture);
            cartViewModel.AuchanTotal = auchanTotal.ToString("F2", CultureInfo.InvariantCulture);
            cartViewModel.MegaTotal = megaTotal.ToString("F2", CultureInfo.InvariantCulture);
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
            if (string.IsNullOrEmpty(quantity))
            {
                return string.Empty;
            }
            return quantity.Replace(',', '.');
        }

        private decimal ParsePrice(string price)
        {
            var cleanedPrice = Regex.Replace(price, @"[^\d,\.]", "");
            return decimal.Parse(cleanedPrice.Replace(',', '.'), CultureInfo.InvariantCulture);
        }

        private decimal TryParsePrice(string price)
        {
            var cleanedPrice = Regex.Replace(price, @"[^\d,\.]", "");
            if (decimal.TryParse(cleanedPrice.Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out var parsedPrice))
            {
                return parsedPrice;
            }
            return decimal.MaxValue;
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

        private List<ItemResult> ParseAuchanResults(string results)
        {
            List<ItemResult> auchanResults = new List<ItemResult>();
            var lines = results.Split(new string[] { "--------------------------------------------------" }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var line in lines)
            {
                string pattern = @"^(.*(?:\r?\n.*?)*?),\s*([\d,]+)\s*lei(?:\s*\+[\d,]+leigarantie)?(?:\r?\n\s*[\d,]+\s*lei)?$";
                Match match = Regex.Match(line.Trim(), pattern, RegexOptions.Multiline);

                if (match.Success)
                {
                    string itemName = match.Groups[1].Value.Trim().Replace("\r\n", " ");
                    string price = match.Groups[2].Value.Trim();
                    string quantityPattern = @"(\d+\.?\d*)\s*(l|kg|g|ml)";

                    Match quantityMatch = Regex.Match(itemName, quantityPattern);
                    string quantity = quantityMatch.Success ? quantityMatch.Groups[1].Value.Trim() : "";
                    string measureQuantity = quantityMatch.Success ? quantityMatch.Groups[2].Value.Trim() : "";

                    auchanResults.Add(new ItemResult
                    {
                        ItemName = itemName,
                        Quantity = quantity,
                        MeasureQuantity = measureQuantity,
                        Price = price + " Lei",
                        Store = "Auchan"
                    });
                }
            }

            return auchanResults;
        }

        private List<ItemResult> ParseMegaResults(string results)
        {
            List<ItemResult> megaResults = new List<ItemResult>();
            var lines = results.Split(new string[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var line in lines)
            {
                string pattern = @"^Text for <a> element in li element \d+: (.+)$";
                Match match = Regex.Match(line.Trim(), pattern);

                if (match.Success)
                {
                    string itemName = match.Groups[1].Value.Trim();
                    string quantityPattern = @"(\d+\.?\d*)\s*(l|kg|g|ml)";

                    Match quantityMatch = Regex.Match(itemName, quantityPattern);
                    string quantity = quantityMatch.Success ? quantityMatch.Groups[1].Value.Trim() : "";
                    string measureQuantity = quantityMatch.Success ? quantityMatch.Groups[2].Value.Trim() : "";

                    megaResults.Add(new ItemResult
                    {
                        ItemName = itemName,
                        Quantity = quantity,
                        MeasureQuantity = measureQuantity,
                        Price = "Unknown",
                        Store = "Mega"
                    });
                }
            }

            return megaResults;
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
