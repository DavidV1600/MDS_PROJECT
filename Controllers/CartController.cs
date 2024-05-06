using Microsoft.AspNetCore.Mvc;

namespace MDS_PROJECT.Controllers
{
    public class CartController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
