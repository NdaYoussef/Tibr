using Microsoft.AspNetCore.Mvc;

namespace Tibr.MVC.Controllers
{
    public class CategoryController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
