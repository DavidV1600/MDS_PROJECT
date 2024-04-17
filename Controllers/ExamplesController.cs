using Microsoft.AspNetCore.Mvc;

namespace MDS_PROJECT.Controllers
{
    public class ExamplesController : Controller
    {
        public IActionResult Index()
        {
            Console.WriteLine("salut");
            return View();
        }
        void exemplu()
        {
            Console.WriteLine("salut");
        }
    }
}
