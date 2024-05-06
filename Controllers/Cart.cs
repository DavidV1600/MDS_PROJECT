using Microsoft.AspNetCore.Mvc;

namespace MDS_PROJECT.Controllers
{
    public class Cart : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
