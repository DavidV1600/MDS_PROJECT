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
    }
}
