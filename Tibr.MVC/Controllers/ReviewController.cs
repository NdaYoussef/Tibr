using Microsoft.AspNetCore.Mvc;

namespace Tibr.MVC.Controllers
{
    public class ReviewController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
