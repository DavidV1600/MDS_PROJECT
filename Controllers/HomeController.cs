﻿using MDS_PROJECT.Models;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace MDS_PROJECT.Controllers
{
    public class HomeController : Controller
    {
        // Definițiile modelelor de date pot fi mutate în fișiere separate pentru claritate, dar funcționează și aici.
        public class SearchViewModel
        {
            public List<ItemResult> CarrefourResults { get; set; } = new List<ItemResult>();
            public List<ItemResult> KauflandResults { get; set; } = new List<ItemResult>();
        }

        public class ItemResult
        {
            public string ItemName { get; set; }
            public string Quantity { get; set; }
            public string MeasureQuantity { get; set; }
            public string Price { get; set; }
        }

        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        // Eliminați unul dintre atributele [HttpPost] redundante
        [HttpPost]
        public async Task<IActionResult> SearchBoth(string query)
        {
            if (string.IsNullOrEmpty(query))
            {
                return View("Index", new SearchViewModel()); // Returnează un model gol dacă interogarea este nulă sau goală
            }

            // Aici, vom efectua ambele căutări simultan
            var carrefourTask = GetSearchResult("Carrefour.py", query);
            var kauflandTask = GetSearchResult("Kaufland.py", query);
            await Task.WhenAll(carrefourTask, kauflandTask);

            var viewModel = new SearchViewModel
            {
                CarrefourResults = ParseResults(carrefourTask.Result), // Asigură-te că ParseResults poate gestiona rezultatele goale/nule
                KauflandResults = ParseKauflandResults(kauflandTask.Result) // Presupunând că și Kaufland va avea o listă de ItemResult
            };

            return View("Index", viewModel);
        }


        private async Task<string> GetSearchResult(string scriptPath, string query)
        {
            ProcessStartInfo start = new ProcessStartInfo
            {
                FileName = "C:\\Users\\Alex\\AppData\\Local\\Programs\\Python\\Launcher\\py.exe",
                Arguments = $"C:\\Users\\Alex\\Desktop\\Proiect\\{scriptPath} {query}",
                UseShellExecute = false,
                RedirectStandardOutput = true
            };

            using (Process process = Process.Start(start))
            {
                using (StreamReader reader = process.StandardOutput)
                {
                    return await reader.ReadToEndAsync();
                }
            }
        }

        //CARREFOUR
        private List<ItemResult> ParseResults(string results)
        {
            //Console.WriteLine(results);
            string pattern = @"Product: (.+?) (\d*[\.,]?\d+)\s*(\w+), Price: (\d+[\.,]?\d*) Lei";
            MatchCollection matches = Regex.Matches(results, pattern);
            return matches.Cast<Match>().Select(m => new ItemResult
            {
                ItemName = m.Groups[1].Value.Trim(),
                Quantity = m.Groups[2].Value.Trim(),
                MeasureQuantity = m.Groups[3].Value.Trim(),
                Price = m.Groups[4].Value.Trim()
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

                    // Assuming quantity is always in the format "number unit"
                    var quantitySplit = quantity.Split(new char[] { ' ' }, 2);
                    if (quantitySplit.Length == 2)
                    {
                        kauflandResults.Add(new ItemResult
                        {
                            ItemName = itemName,
                            Quantity = quantitySplit[0],
                            MeasureQuantity = quantitySplit[1],
                            Price = price + " Lei" // assuming currency is always Lei as per Carrefour results
                        });
                    }
                }
            }

            return kauflandResults;
        }



        public IActionResult Index()
        {
            // Creează o instanță a ViewModel-ului chiar dacă nu există date pentru a preveni referințele nule
            var viewModel = new SearchViewModel();
            return View(viewModel); // Pasează modelul gol către view
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