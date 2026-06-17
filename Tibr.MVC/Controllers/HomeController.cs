using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using Tibr.MVC.Models;

namespace Tibr.MVC.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            ViewData["HeaderTitle"] = "Dashboard";
            ViewData["ShowBack"] = false;
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
