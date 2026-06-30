using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tibr.Application.Dtos;
using Tibr.Application.Services.WithdrawServices;

namespace Tibr.MVC.Controllers
{
    [Authorize(Roles = "Admin")]
    public class WithdrawController(IWithdrawService withdrawService) : Controller
    {
        public async Task<IActionResult> Index()
        {
            var result = await withdrawService.GetAllAsync();

            if (result.IsFailure)
                return View("Error");

            return View(result.Data);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateStatus(UpdateWithdrawStatusDto dto)
        {
            var result = await withdrawService.UpdateStatusAsync(dto);

            if (result.IsFailure)
                TempData["Error"] = result.ErrorMessage;
            else
                TempData["Success"] = "تم تحديث الحالة بنجاح";

            return RedirectToAction(nameof(Index));
        }
    }
}