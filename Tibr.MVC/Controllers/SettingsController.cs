using Microsoft.AspNetCore.Mvc;

namespace Tibr.MVC.Controllers
{
    public class SettingsController : Controller
    {
        // GET: Settings
        public IActionResult Index()
        {
            ViewData["HeaderTitle"] = "Settings";
            ViewData["ShowBack"] = false;
            return View();
        }

        // POST: Settings/Save
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Save(string companyName, string companyEmail, string currency, bool maintenanceMode)
        {
            // Simulating save process
            TempData["SuccessMessage"] = "System settings updated successfully.";
            return RedirectToAction(nameof(Index));
        }
    }
}
