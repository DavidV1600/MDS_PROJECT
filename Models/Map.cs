using Microsoft.AspNetCore.Mvc;

namespace MDS_PROJECT.Models
{
    public class Map : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
