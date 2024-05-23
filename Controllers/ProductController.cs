using MDS_PROJECT.Data;
using MDS_PROJECT.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;

namespace MDS_PROJECT.Controllers
{
    public class ProductController : Controller
    {
        public class SearchViewModel
        {
            public List<ItemResult> CarrefourResults { get; set; } = new List<ItemResult>();
            public List<ItemResult> KauflandResults { get; set; } = new List<ItemResult>();
            public List<ItemResult> PennyResults { get; set; } = new List<ItemResult>();
            public List<ItemResult> AuchanResults { get; set; } = new List<ItemResult>();
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

        private readonly ApplicationDbContext db;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IConfiguration _configuration;

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

        [HttpPost]
        [HttpPost]
        [HttpPost]
        [HttpPost]
        public async Task<IActionResult> SearchBoth(string query, int quantity, bool exactItemName)
        {
            if (string.IsNullOrEmpty(query))
            {
                return View("Index", new SearchViewModel());
            }

            var existingProducts = db.Products.Where(p => p.Searched == query).ToList();
            if (existingProducts.Any())
            {
                var view = new SearchViewModel
                {
                    CarrefourResults = existingProducts.Where(p => p.Store == "Carrefour" && (quantity == 0 || p.Quantity == quantity.ToString())).Select(p => new ItemResult(p)).ToList(),
                    KauflandResults = existingProducts.Where(p => p.Store == "Kaufland" && (quantity == 0 || p.Quantity == quantity.ToString())).Select(p => new ItemResult(p)).ToList(),
                    PennyResults = existingProducts.Where(p => p.Store == "Penny" && (quantity == 0 || p.Quantity == quantity.ToString())).Select(p => new ItemResult(p)).ToList(),
                    AuchanResults = existingProducts.Where(p => p.Store == "Auchan" && (quantity == 0 || p.Quantity == quantity.ToString())).Select(p => new ItemResult(p)).ToList()
                };

                return View("Index", view);
            }

            Task<string> carrefourTask;
            Task<string> kauflandTask;
            Task<string> pennyTask;
            Task<string> auchanTask;

            if (exactItemName)
            {
                carrefourTask = GetSearchResult("CarrefourExact.py", query);
                kauflandTask = GetSearchResult("KauflandExact.py", query);
                pennyTask = GetSearchResult("PennyExact.py", query);
                auchanTask = GetSearchResult("AuchanExact.py", query);
            }
            else
            {
                carrefourTask = GetSearchResult("Carrefour.py", query);
                kauflandTask = GetSearchResult("Kaufland.py", query);
                pennyTask = GetSearchResult("Penny.py", query);
                auchanTask = GetSearchResult("Auchan.py", query);
            }

            await Task.WhenAll(carrefourTask, kauflandTask, pennyTask, auchanTask);

            var carrefourResults = ParseResults(carrefourTask.Result);
            var kauflandResults = ParseKauflandResults(kauflandTask.Result);
            var pennyResults = ParsePennyResults(pennyTask.Result);
            var auchanResults = ParseAuchanResults(auchanTask.Result);

            if (quantity > 0)
            {
                carrefourResults = carrefourResults.Where(p => p.Quantity == quantity.ToString()).ToList();
                kauflandResults = kauflandResults.Where(p => p.Quantity == quantity.ToString()).ToList();
                pennyResults = pennyResults.Where(p => p.Quantity == quantity.ToString()).ToList();
                auchanResults = auchanResults.Where(p => p.Quantity == quantity.ToString()).ToList();
            }

            var viewModel = new SearchViewModel
            {
                CarrefourResults = carrefourResults,
                KauflandResults = kauflandResults,
                PennyResults = pennyResults,
                AuchanResults = auchanResults
            };

            foreach (var item in viewModel.CarrefourResults.Concat(viewModel.KauflandResults).Concat(viewModel.PennyResults).Concat(viewModel.AuchanResults))
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
        private List<ItemResult> ParseAuchanResults(string results)
        {
            var auchanResults = new List<ItemResult>();
            var productPattern = @"Product \d+ Text:(.*?)(?=Product \d+ Text:|$)";
            var matches = Regex.Matches(results, productPattern, RegexOptions.Singleline);

            foreach (Match match in matches)
            {
                var productText = match.Groups[1].Value.Trim();
                var lines = productText.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                                       .Where(line => !line.Contains("---")) // Remove lines containing only dashes
                                       .ToArray();

                if (lines.Length >= 3)
                {
                    string itemName = "";
                    int index = 0;

                    // Collect item name until the line with "In stoc"
                    while (index < lines.Length && !lines[index].StartsWith("In stoc"))
                    {
                        itemName += (itemName == "" ? "" : " ") + lines[index].Trim();
                        index++;
                    }

                    // Skip "In stoc" line
                    if (index < lines.Length && lines[index].StartsWith("In stoc"))
                    {
                        index++;
                    }

                    string price = "";
                    string quantity = "";

                    if (index < lines.Length)
                    {
                        // Check if the next line is a price
                        var priceLine = lines[index].Trim();
                        if (priceLine.Contains("lei"))
                        {
                            price = priceLine.Split(' ')[0].Trim();
                            index++;
                        }

                        // Handle optional extra lines like "+0,5leigarantie" or percentage discounts
                        while (index < lines.Length && (lines[index].Contains("lei") || lines[index].Contains("%")))
                        {
                            index++;
                        }

                        // If we are still within the bounds, the next line could be the quantity
                        if (index < lines.Length)
                        {
                            quantity = lines[index].Trim();
                        }
                    }

                    // If we found valid item name, price, and quantity, add to results
                    if (!string.IsNullOrWhiteSpace(itemName) && !string.IsNullOrWhiteSpace(price))
                    {
                        string measureQuantity = quantity.Contains(" ") ? quantity.Split(' ')[1].Trim() : "";

                        auchanResults.Add(new ItemResult
                        {
                            ItemName = itemName,
                            Quantity = quantity.Split(' ')[0].Trim(),
                            MeasureQuantity = measureQuantity,
                            Price = price + " Lei",
                            Store = "Auchan"
                        });
                    }
                }
            }

            return auchanResults;
        }




        private List<ItemResult> ParsePennyResults(string results)
        {
            var pennyResults = new List<ItemResult>();
            var productPattern = @"Product \d+ Text:(.*?)(?=Product \d+ Text:|$)";
            var matches = Regex.Matches(results, productPattern, RegexOptions.Singleline);

            foreach (Match match in matches)
            {
                var productText = match.Groups[1].Value.Trim();
                var lines = productText.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

                if (lines.Length >= 6)
                {
                    string itemName = lines[0].Trim() + " " + lines[1].Trim();
                    string quantity = lines[2].Trim();
                    string priceLine = lines[^2].Trim();
                    string price = priceLine.Split(' ')[0].Trim();

                    string measureQuantity = quantity.Contains(" ") ? quantity.Split(' ')[1].Trim() : "";

                    pennyResults.Add(new ItemResult
                    {
                        ItemName = itemName,
                        Quantity = quantity.Split(' ')[0].Trim(),
                        MeasureQuantity = measureQuantity,
                        Price = price + " Lei",
                        Store = "Penny"
                    });
                }
            }

            return pennyResults;
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
                    var output = await reader.ReadToEndAsync();
                    // Log the output for debugging
                    Console.WriteLine("Python Script Output:");
                    Console.WriteLine(output);
                    return output;
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
            Console.WriteLine("SUNT IN KAUFLAND");
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

        public IActionResult Index()
        {
            var viewModel = new SearchViewModel();
            return View(viewModel);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
