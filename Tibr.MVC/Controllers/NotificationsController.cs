using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Tibr.Application.Services.NotificationServices;
using Tibr.Domain.Entities;

[Authorize(Roles = "Admin,SuperAdmin")]
public class NotificationsController : Controller
{
    private readonly INotificationService _notificationService;

    public NotificationsController(INotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    [HttpGet]
    public async Task<IActionResult> GetRecent()
    {
        var adminId = long.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await _notificationService.GetRecentForAdminAsync(adminId);
        return Json(result.Data);
    }

    [HttpPost]
    public async Task<IActionResult> MarkAsRead(long id)
    {
        var result = await _notificationService.MarkAsReadAsync(id);
        return Json(new { success = result.IsSuccess });
    }

    [HttpPost]
    public async Task<IActionResult> MarkAllAsRead()
    {
        var adminId = long.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await _notificationService.MarkAllAsReadAsync(adminId);
        return Json(new { success = result.IsSuccess });
    }
}