using MDS_PROJECT.Data;
using MDS_PROJECT.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Text;

namespace MDS_PROJECT.Controllers
{
    public class ProductController : Controller
    {
        // ViewModel representing the search results
        public class SearchViewModel
        {
            public List<ItemResult> CarrefourResults { get; set; } = new List<ItemResult>();
            public List<ItemResult> KauflandResults { get; set; } = new List<ItemResult>();
            public List<ItemResult> AuchanResults { get; set; } = new List<ItemResult>();
            public List<ItemResult> MegaResults { get; set; } = new List<ItemResult>(); // Added for Mega results
        }

        // Model representing an item result
        public class ItemResult
        {
            public string ItemName { get; set; }
            public string Quantity { get; set; }
            public string MeasureQuantity { get; set; }
            public string Price { get; set; }
            public string Store { get; set; }
            public string Searched { get; set; }

            // Default constructor
            public ItemResult() { }

            // Constructor to initialize from a Product object
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

        private readonly ApplicationDbContext db; // Database context
        private readonly UserManager<ApplicationUser> _userManager; // User manager for handling user-related operations
        private readonly RoleManager<IdentityRole> _roleManager; // Role manager for handling role-related operations
        private readonly IConfiguration _configuration; // Configuration for accessing settings

        // Constructor to initialize the controller with the necessary dependencies
        public ProductController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            IConfiguration configuration)
        {
            db = context;
            _userManager = userManager;
            _roleManager = roleManager;
            _configuration = configuration;
        }

        // Action to handle the search request for products in all stores
        [HttpPost]
        public async Task<IActionResult> SearchBoth(string query, string quantity, bool exactItemName)
        {
            // Return an empty view if the query is empty
            if (string.IsNullOrEmpty(query))
            {
                return View("Index", new SearchViewModel());
            }

            // Check if the product is already in the database
            var existingProducts = db.Products.Where(p => p.Searched == query).ToList();
            if (existingProducts.Any())
            {
                var view = new SearchViewModel
                {
                    CarrefourResults = existingProducts.Where(p => p.Store == "Carrefour" && (string.IsNullOrEmpty(quantity) || GetEquivalentQuantities(quantity).Contains(p.Quantity))).Select(p => new ItemResult(p)).ToList(),
                    KauflandResults = existingProducts.Where(p => p.Store == "Kaufland" && (string.IsNullOrEmpty(quantity) || GetEquivalentQuantities(quantity).Contains(p.Quantity))).Select(p => new ItemResult(p)).ToList(),
                    AuchanResults = existingProducts.Where(p => p.Store == "Auchan" && (string.IsNullOrEmpty(quantity) || GetEquivalentQuantities(quantity).Contains(p.Quantity))).Select(p => new ItemResult(p)).ToList(),
                    MegaResults = existingProducts.Where(p => p.Store == "Mega" && (string.IsNullOrEmpty(quantity) || GetEquivalentQuantities(quantity).Contains(p.Quantity))).Select(p => new ItemResult(p)).ToList() // Added for Mega results
                };

                return View("Index", view);
            }

            // Execute the search scripts for Carrefour, Kaufland, Auchan, and Mega
            Task<string> carrefourTask;
            Task<string> kauflandTask;
            Task<string> auchanTask;
            Task<string> megaTask; // Added for Mega

            if (exactItemName)
            {
                carrefourTask = GetSearchResult("CarrefourExact.py", query);
                kauflandTask = GetSearchResult("KauflandExact.py", query);
                auchanTask = GetSearchResult("AuchanExact.py", query);
                megaTask = GetSearchResult("MegaExact.py", query); // Added for Mega
            }
            else
            {
                carrefourTask = GetSearchResult("Carrefour.py", query);
                kauflandTask = GetSearchResult("Kaufland.py", query);
                auchanTask = GetSearchResult("Auchan.py", query);
                megaTask = GetSearchResult("Mega.py", query); // Added for Mega
            }

            await Task.WhenAll(carrefourTask, kauflandTask, auchanTask, megaTask); // Updated to include Mega task

            var carrefourResults = ParseResults(carrefourTask.Result);
            var kauflandResults = ParseKauflandResults(kauflandTask.Result);
            var auchanResults = ParseAuchanResults(auchanTask.Result);
            var megaResults = ParseMegaResults(megaTask.Result); // Added for Mega results

            if (!string.IsNullOrEmpty(quantity))
            {
                var equivalentQuantities = GetEquivalentQuantities(quantity);
                carrefourResults = carrefourResults.Where(p => equivalentQuantities.Contains(p.Quantity)).ToList();
                kauflandResults = kauflandResults.Where(p => equivalentQuantities.Contains(p.Quantity)).ToList();
                auchanResults = auchanResults.Where(p => equivalentQuantities.Contains(p.Quantity)).ToList();
                megaResults = megaResults.Where(p => equivalentQuantities.Contains(p.Quantity)).ToList(); // Added for Mega results
            }

            var viewModel = new SearchViewModel
            {
                CarrefourResults = carrefourResults,
                KauflandResults = kauflandResults,
                AuchanResults = auchanResults,
                MegaResults = megaResults // Added for Mega results
            };

            // Save the results to the database
            foreach (var item in viewModel.CarrefourResults.Concat(viewModel.KauflandResults).Concat(viewModel.AuchanResults).Concat(viewModel.MegaResults)) // Updated to include Mega results
            {
                var product = new Product
                {
                    ItemName = item.ItemName,
                    Quantity = item.Quantity,
                    MeasureQuantity = item.MeasureQuantity,
                    Price = item.Price,
                    Store = item.Store,
                    Searched = query
                };

                if (!db.Products.Any(p => p.ItemName == product.ItemName && p.Quantity == product.Quantity))
                {
                    db.Products.Add(product);
                }
            }

            await db.SaveChangesAsync();

            return View("Index", viewModel);
        }

        private async Task<string> GetSearchResult(string scriptPath, string query)
        {
            string pythonExePath = _configuration["PathVariables:PythonExePath"];
            string scriptFolderPath = _configuration["PathVariables:ScriptFolderPath"];

            ProcessStartInfo start = new ProcessStartInfo
            {
                FileName = pythonExePath,
                Arguments = $"{scriptFolderPath}{scriptPath} {query}",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                StandardOutputEncoding = Encoding.UTF8
            };

            using (Process process = Process.Start(start))
            {
                using (StreamReader reader = process.StandardOutput)
                {
                    return await reader.ReadToEndAsync();
                }
            }
        }

        private List<ItemResult> ParseResults(string results)
        {
            string pattern = @"Product: (.+?) (\d*[\.,]?\d+)\s*(\w+), Price: (\d+[\.,]?\d*) Lei";
            MatchCollection matches = Regex.Matches(results, pattern);
            return matches.Cast<Match>().Select(m => new ItemResult
            {
                ItemName = m.Groups[1].Value.Trim(),
                Quantity = m.Groups[2].Value.Trim(),
                MeasureQuantity = m.Groups[3].Value.Trim(),
                Price = m.Groups[4].Value.Trim(),
                Store = "Carrefour"
            }).ToList();
        }

        private List<ItemResult> ParseKauflandResults(string results)
        {
            List<ItemResult> kauflandResults = new List<ItemResult>();
            var lines = results.Split(new string[] { "--------------------------------" }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var line in lines)
            {
                string pattern = @"Product Name: (.+?)\r\nProduct Subtitle: (.+?)\r\nProduct Price: (\d+[\.,]?\d*)\r\nProduct Quantity: (.+)";
                Match match = Regex.Match(line, pattern);

                if (match.Success)
                {
                    string itemName = match.Groups[1].Value.Trim() + " " + match.Groups[2].Value.Trim();
                    string price = match.Groups[3].Value.Trim();
                    string quantity = match.Groups[4].Value.Trim();

                    var quantitySplit = quantity.Split(new char[] { ' ' }, 2);
                    if (quantitySplit.Length == 2)
                    {
                        kauflandResults.Add(new ItemResult
                        {
                            ItemName = itemName,
                            Quantity = quantitySplit[0],
                            MeasureQuantity = quantitySplit[1],
                            Price = price + " Lei",
                            Store = "Kaufland"
                        });
                    }
                }
            }

            return kauflandResults;
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
                    string quantityPattern = @"(\d+\.?\d*)\s*(l|kg|g|ml)"; // Pattern to extract quantity and measure

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
                    string quantityPattern = @"(\d+\.?\d*)\s*(l|kg|g|ml)"; // Pattern to extract quantity and measure

                    Match quantityMatch = Regex.Match(itemName, quantityPattern);
                    string quantity = quantityMatch.Success ? quantityMatch.Groups[1].Value.Trim() : "";
                    string measureQuantity = quantityMatch.Success ? quantityMatch.Groups[2].Value.Trim() : "";

                    megaResults.Add(new ItemResult
                    {
                        ItemName = itemName,
                        Quantity = quantity,
                        MeasureQuantity = measureQuantity,
                        Price = "Unknown", // Update this if you have price information
                        Store = "Mega"
                    });
                }
            }

            return megaResults;
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

        // Action to display the initial search view
        public IActionResult Index()
        {
            var viewModel = new SearchViewModel();
            return View(viewModel);
        }

        // Action to display the privacy view
        public IActionResult Privacy()
        {
            return View();
        }

        // Action to handle errors
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}