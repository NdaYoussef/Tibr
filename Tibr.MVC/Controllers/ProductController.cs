using Microsoft.AspNetCore.Mvc;

namespace Tibr.MVC.Controllers
{
    public class ProductController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
