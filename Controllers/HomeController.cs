using MDS_PROJECT.Models;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace MDS_PROJECT.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }
        [HttpPost]
        public IActionResult SearchCarrefour(string query)
        {
            Console.WriteLine("The word entered by the user is: " + query);

            ProcessStartInfo start = new ProcessStartInfo();
            start.FileName = "C:\\Users\\Alex\\AppData\\Local\\Programs\\Python\\Launcher\\py.exe";
            start.Arguments = $"C:\\Users\\Alex\\Desktop\\Proiect\\Carrefour.py {query}";
            start.UseShellExecute = false;
            start.RedirectStandardOutput = true;

            using (Process process = Process.Start(start))
            {
                using (StreamReader reader = process.StandardOutput)
                {
                    string result = reader.ReadToEnd();
                    //ViewData["Result"] = result;
                    string pattern = @"Product: (.+?) (\d*[\.,]?\d+)\s*(\w+), Price: (\d+[\.,]?\d*) Lei";

                    // Match the pattern in the input text
                    MatchCollection matches = Regex.Matches(result, pattern);

                    // Iterate over the matches and print the extracted information
                    foreach (Match match in matches)
                    {
                        string itemName = match.Groups[1].Value.Trim();
                        string quantity = match.Groups[2].Value.Trim();
                        string measureQuantity = match.Groups[3].Value.Trim();
                        string price = match.Groups[4].Value.Trim();

                        Console.WriteLine($"Item: {itemName}, Quantity: {quantity} {measureQuantity}, Price: {price} Lei");
                    }


                }
            }


            return RedirectToAction("Index"); // Redirect to a different action if needed
        }
        [HttpPost]
        public IActionResult SearchKaufland(string query)
        {
            Console.WriteLine("The word entered by the user is: " + query);

            ProcessStartInfo start = new ProcessStartInfo();
            start.FileName = "C:\\Users\\Alex\\AppData\\Local\\Programs\\Python\\Launcher\\py.exe";
            start.Arguments = $"C:\\Users\\Alex\\Desktop\\Proiect\\Kaufland.py {query}";
            start.UseShellExecute = false;
            start.RedirectStandardOutput = true;

            using (Process process = Process.Start(start))
            {
                using (StreamReader reader = process.StandardOutput)
                {
                    string result = reader.ReadToEnd();
                    //ViewData["Result"] = result;
                    Console.WriteLine(result);
                }
            }
            return RedirectToAction("Index"); // Redirect to a different action if needed
        }

        public IActionResult Index()
        {
            return View();
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