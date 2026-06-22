using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using MediatR;
using Tibr.Application.Dtos;
using Tibr.Application.Services.UserServices;
using Tibr.Application.Services.Auth;
using Tibr.MVC.Models.Users;

namespace Tibr.MVC.Controllers
{
    public class UsersController : Controller
    {
        private readonly IUserService _userService;
        private readonly IMediator _mediator;
        private readonly ILogger<UsersController> _logger;

        public UsersController(IUserService userService, IMediator mediator, ILogger<UsersController> logger)
        {
            _userService = userService;
            _mediator = mediator;
            _logger = logger;
        }

        // GET: Users
        public async Task<IActionResult> Index(string? searchQuery, string? statusFilter, string? kycStatusFilter, int page = 1)
        {
            ViewData["HeaderTitle"] = "User Directory";
            ViewData["ShowBack"] = false;
            const int pageSize = 5;

            var result = await _userService.GetUsersAsync(searchQuery, statusFilter, kycStatusFilter);
            if (result.IsFailure)
            {
                TempData["ErrorMessage"] = result.ErrorMessage;
                return View(new UserListViewModel());
            }

            var usersList = (result.Data ?? Enumerable.Empty<UserListItemDto>()).Select(u => new UserListItemViewModel
            {
                Id = u.Id,
                FirstName = u.FirstName,
                LastName = u.LastName,
                Email = u.Email,
                Phone = u.Phone,
                Status = u.Status,
                OtpVerified = u.OtpVerified,
                KycStatus = u.KycStatus,
                CreatedAt = u.CreatedAt
            }).ToList();

            var totalItems = usersList.Count;
            var paginatedUsers = usersList.OrderByDescending(u => u.CreatedAt)
                                      .Skip((page - 1) * pageSize)
                                      .Take(pageSize)
                                      .ToList();

            var viewModel = new UserListViewModel
            {
                Users = paginatedUsers,
                SearchQuery = searchQuery,
                StatusFilter = statusFilter,
                KycStatusFilter = kycStatusFilter,
                CurrentPage = page,
                PageSize = pageSize,
                TotalItems = totalItems
            };

            return View(viewModel);
        }

        // GET: Users/Details/5
        public async Task<IActionResult> Details(long id)
        {
            ViewData["HeaderTitle"] = "User Details";
            ViewData["ShowBack"] = true;

            var result = await _userService.GetByIdAsync(id);
            if (result.IsFailure)
            {
                TempData["ErrorMessage"] = result.ErrorMessage;
                return RedirectToAction(nameof(Index));
            }

            var u = result.Data!;
            var userDetails = new UserDetailsViewModel
            {
                Id = u.Id,
                FirstName = u.FirstName,
                LastName = u.LastName,
                Email = u.Email,
                Phone = u.Phone,
                Status = u.Status,
                OtpVerified = u.OtpVerified,
                KycStatus = u.KycStatus,
                CreatedAt = u.CreatedAt,
                UpdatedAt = u.UpdatedAt,
                Orders = u.Orders.Select(o => new UserOrderHistoryItemViewModel
                {
                    OrderId = o.OrderId,
                    OrderNumber = o.OrderNumber,
                    TotalAmount = o.TotalAmount,
                    OrderStatus = o.OrderStatus,
                    PaymentStatus = o.PaymentStatus,
                    CreatedAt = o.CreatedAt
                }).ToList()
            };

            return View(userDetails);
        }

        // GET: Users/Create
        public IActionResult Create()
        {
            ViewData["HeaderTitle"] = "Add New User";
            ViewData["ShowBack"] = true;
            ViewData["HeaderBadge"] = "Executive Suite";
            return View(new UserCreateViewModel());
        }

        // POST: Users/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(UserCreateViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var requestData = new RegisterRequestData(
                model.FirstName,
                model.LastName,
                model.Email,
                model.Phone,
                model.Password,
                model.ConfirmPassword
            );

            try
            {
                var result = await _mediator.Send(new RegisterCommand(requestData));
                if (result.IsSuccess)
                {
                    TempData["SuccessMessage"] = "User created successfully. OTP code sent to user email.";
                    return RedirectToAction(nameof(Index));
                }
                else
                {
                    ModelState.AddModelError("", result.MessageEN ?? "Registration failed.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create user in DB.");
                ModelState.AddModelError("", $"Failed to create user: {ex.Message}");
            }

            return View(model);
        }

        // GET: Users/Edit/5
        public async Task<IActionResult> Edit(long id)
        {
            ViewData["HeaderTitle"] = "Edit User Profile";
            ViewData["ShowBack"] = true;

            var result = await _userService.GetByIdAsync(id);
            if (result.IsFailure)
            {
                TempData["ErrorMessage"] = result.ErrorMessage;
                return RedirectToAction(nameof(Index));
            }

            var details = result.Data!;
            var editModel = new UserEditViewModel
            {
                Id = details.Id,
                FirstName = details.FirstName,
                LastName = details.LastName,
                Email = details.Email,
                Phone = details.Phone,
                Status = details.Status,
                KycStatus = details.KycStatus
            };

            return View(editModel);
        }

        // POST: Users/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(long id, UserEditViewModel model)
        {
            if (id != model.Id)
            {
                return BadRequest();
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var updateDto = new UpdateUserDto
            {
                Id = model.Id,
                FirstName = model.FirstName,
                LastName = model.LastName,
                Email = model.Email,
                Phone = model.Phone,
                Status = model.Status,
                KycStatus = model.KycStatus
            };

            var result = await _userService.UpdateAsync(id, updateDto);
            if (result.IsSuccess)
            {
                TempData["SuccessMessage"] = "User updated successfully.";
                return RedirectToAction(nameof(Index));
            }

            ModelState.AddModelError("", result.ErrorMessage);
            return View(model);
        }

        // POST: Users/ToggleStatus/5 (AJAX)
        [HttpPost]
        public async Task<IActionResult> ToggleStatus(long id)
        {
            var result = await _userService.ToggleStatusAsync(id);
            if (result.IsSuccess)
            {
                return Json(new { success = true, status = result.Data });
            }
            return Json(new { success = false, message = result.ErrorMessage });
        }

        // POST: Users/UpdateKycStatus (AJAX)
        [HttpPost]
        public async Task<IActionResult> UpdateKycStatus(long id, string kycStatus)
        {
            if (string.IsNullOrWhiteSpace(kycStatus))
            {
                return Json(new { success = false, message = "KYC status is empty." });
            }

            var result = await _userService.UpdateKycStatusAsync(id, kycStatus);
            if (result.IsSuccess)
            {
                return Json(new { success = true, kycStatus = result.Data });
            }
            return Json(new { success = false, message = result.ErrorMessage });
        }

        // POST: Users/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(long id)
        {
            var result = await _userService.DeleteAsync(id);
            if (result.IsSuccess)
            {
                TempData["SuccessMessage"] = "User deleted successfully.";
                return RedirectToAction(nameof(Index));
            }

            TempData["ErrorMessage"] = result.ErrorMessage;
            return RedirectToAction(nameof(Index));
        }

        // GET: Users/Export
        public async Task<IActionResult> Export(string? searchQuery, string? statusFilter, string? kycStatusFilter)
        {
            var result = await _userService.GetUsersAsync(searchQuery, statusFilter, kycStatusFilter);
            List<UserListItemDto> usersToExport = result.IsSuccess && result.Data != null ? result.Data.ToList() : [];

            // Generate CSV
            var csvBuilder = new StringBuilder();
            csvBuilder.AppendLine("User ID,Full Name,Email,Phone,Account Status,Verification Status,Registered Date");

            foreach (var user in usersToExport)
            {
                csvBuilder.AppendLine($"{user.Id},\"{user.FullName}\",\"{user.Email}\",\"{user.Phone}\",\"{user.Status}\",\"{user.KycStatus}\",\"{user.CreatedAt:yyyy-MM-dd HH:mm:ss}\"");
            }

            var csvData = Encoding.UTF8.GetBytes(csvBuilder.ToString());
            var fileResult = new FileContentResult(csvData, "text/csv")
            {
                FileDownloadName = $"Tibr_Users_Export_{DateTime.Now:yyyyMMdd_HHmmss}.csv"
            };

            return fileResult;
        }
    }
}
