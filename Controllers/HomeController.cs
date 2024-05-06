using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace MDS_PROJECT.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View("~/Views/Home/Index.cshtml"); // Specify the correct path to the Index view
        }

        //[HttpPost]
        //public IActionResult Search(string query)
        //{
        //    // Call Python script with user's input
        //    ProcessStartInfo start = new ProcessStartInfo();
        //    start.FileName = "C:\\Users\\maria\\AppData\\Local\\Microsoft\\WindowsApps\\PythonSoftwareFoundation.Python.3.11_qbz5n2kfra8p0\\python.exe"; // Replace with the path to your Python executable
        //    start.Arguments = $"C:\\Users\\maria\\PycharmProjects\\Lab1_IA\\Carefour.py {query}";
        //    start.UseShellExecute = false;
        //    start.RedirectStandardOutput = true;
        //    using (Process process = Process.Start(start))
        //    {
        //        using (StreamReader reader = process.StandardOutput)
        //        {
        //            string result = reader.ReadToEnd();
        //            ViewData["Result"] = result;
        //        }
        //    }

        //    return View("~/Views/Product/Index.cshtml");
        //}

    }
}
