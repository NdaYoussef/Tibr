using Microsoft.AspNetCore.Mvc;

namespace Tibr.MVC.Controllers
{
    public class SupportController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
