using Microsoft.AspNetCore.Mvc;

namespace Tibr.MVC.Controllers
{
    public class AnalyticsController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
