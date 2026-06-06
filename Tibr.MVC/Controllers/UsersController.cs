using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Tibr.MVC.Models.Users;

namespace Tibr.MVC.Controllers
{
    public class UsersController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<UsersController> _logger;

        // In-memory fallback mock database for demonstration and testing when the backend API is offline
        private static readonly List<UserListItemViewModel> MockUsers = new()
        {
            new() { Id = 1, FirstName = "Ahmad", LastName = "Al-Saeed", Email = "ahmad.saeed@tibr.com", Phone = "+962791234567", Status = "Active", OtpVerified = true, KycStatus = "Approved", CreatedAt = DateTime.Now.AddDays(-60) },
            new() { Id = 2, FirstName = "Sarah", LastName = "Smith", Email = "sarah.smith@tibr.com", Phone = "+12025550143", Status = "Active", OtpVerified = true, KycStatus = "Pending", CreatedAt = DateTime.Now.AddDays(-30) },
            new() { Id = 3, FirstName = "John", LastName = "Doe", Email = "john.doe@tibr.com", Phone = "+14155552671", Status = "Suspended", OtpVerified = true, KycStatus = "Rejected", CreatedAt = DateTime.Now.AddDays(-15) },
            new() { Id = 4, FirstName = "Fatima", LastName = "Mansour", Email = "fatima.m@tibr.com", Phone = "+966501234567", Status = "Active", OtpVerified = false, KycStatus = "Pending", CreatedAt = DateTime.Now.AddDays(-5) },
            new() { Id = 5, FirstName = "Liam", LastName = "Johnson", Email = "liam.j@tibr.com", Phone = "+442079460192", Status = "Active", OtpVerified = true, KycStatus = "Approved", CreatedAt = DateTime.Now.AddDays(-45) }
        };

        private static readonly List<UserOrderHistoryItemViewModel> MockOrders = new()
        {
            new() { OrderId = 101, OrderNumber = "TIBR-9821-01", TotalAmount = 249.99m, OrderStatus = "Delivered", PaymentStatus = "Paid", CreatedAt = DateTime.Now.AddDays(-40) },
            new() { OrderId = 102, OrderNumber = "TIBR-9821-02", TotalAmount = 89.50m, OrderStatus = "Shipped", PaymentStatus = "Paid", CreatedAt = DateTime.Now.AddDays(-10) },
            new() { OrderId = 103, OrderNumber = "TIBR-3310-01", TotalAmount = 1200.00m, OrderStatus = "Processing", PaymentStatus = "Paid", CreatedAt = DateTime.Now.AddDays(-2) },
            new() { OrderId = 104, OrderNumber = "TIBR-1102-05", TotalAmount = 45.00m, OrderStatus = "Cancelled", PaymentStatus = "Refunded", CreatedAt = DateTime.Now.AddDays(-1) }
        };

        public UsersController(IHttpClientFactory httpClientFactory, ILogger<UsersController> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        // Helper to create and authorize HttpClient with Bearer Token
        private HttpClient GetApiClient()
        {
            var client = _httpClientFactory.CreateClient("TibrApi");
            var token = Request.Cookies["JwtToken"]; // Read token from client browser cookie
            if (!string.IsNullOrEmpty(token))
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }
            return client;
        }

        // GET: Users
        public async Task<IActionResult> Index(string? searchQuery, string? statusFilter, string? kycStatusFilter, int page = 1)
        {
            ViewData["HeaderTitle"] = "User Directory";
            ViewData["ShowBack"] = false;
            const int pageSize = 5;
            var client = GetApiClient();
            List<UserListItemViewModel> usersList;

            try
            {
                // Construct URL with query parameters for the API
                var queryParams = $"?searchQuery={searchQuery}&statusFilter={statusFilter}&kycStatusFilter={kycStatusFilter}";
                var response = await client.GetAsync($"users{queryParams}");

                if (response.IsSuccessStatusCode)
                {
                    usersList = await response.Content.ReadFromJsonAsync<List<UserListItemViewModel>>() ?? [];
                }
                else
                {
                    _logger.LogWarning($"API returned error code {response.StatusCode}. Falling back to simulated local database.");
                    usersList = [.. MockUsers];
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to connect to API. Falling back to simulated local database.");
                usersList = [.. MockUsers];
            }

            // Client-side filtering logic (applied on API list or Mock data)
            var query = usersList.AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchQuery))
            {
                var lowerSearch = searchQuery.ToLower();
                query = query.Where(u => u.FirstName.ToLower().Contains(lowerSearch) || 
                                         u.LastName.ToLower().Contains(lowerSearch) || 
                                         u.Email.ToLower().Contains(lowerSearch) || 
                                         u.Phone.Contains(lowerSearch));
            }

            if (!string.IsNullOrWhiteSpace(statusFilter))
            {
                query = query.Where(u => u.Status.Equals(statusFilter, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrWhiteSpace(kycStatusFilter))
            {
                query = query.Where(u => u.KycStatus.Equals(kycStatusFilter, StringComparison.OrdinalIgnoreCase));
            }

            var totalItems = query.Count();
            var paginatedUsers = query.OrderByDescending(u => u.CreatedAt)
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
            var client = GetApiClient();
            UserDetailsViewModel? userDetails = null;

            try
            {
                var response = await client.GetAsync($"users/{id}");
                if (response.IsSuccessStatusCode)
                {
                    userDetails = await response.Content.ReadFromJsonAsync<UserDetailsViewModel>();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to retrieve details from API for user {id}. Using simulated database.");
            }

            if (userDetails == null)
            {
                var mockUser = MockUsers.FirstOrDefault(u => u.Id == id);
                if (mockUser == null)
                {
                    return NotFound($"User with ID {id} was not found.");
                }

                // Map mock data
                userDetails = new UserDetailsViewModel
                {
                    Id = mockUser.Id,
                    FirstName = mockUser.FirstName,
                    LastName = mockUser.LastName,
                    Email = mockUser.Email,
                    Phone = mockUser.Phone,
                    Status = mockUser.Status,
                    OtpVerified = mockUser.OtpVerified,
                    KycStatus = mockUser.KycStatus,
                    CreatedAt = mockUser.CreatedAt,
                    UpdatedAt = mockUser.CreatedAt.AddDays(2),
                    // Pull sample orders associated with user ID
                    Orders = id == 1 ? [MockOrders[0], MockOrders[1]] : id == 2 ? [MockOrders[2]] : [MockOrders[3]]
                };
            }

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

            var client = GetApiClient();
            try
            {
                // Trigger register endpoint on API
                var response = await client.PostAsJsonAsync("auth/register", model);
                if (response.IsSuccessStatusCode)
                {
                    TempData["SuccessMessage"] = "User created successfully via API.";
                    return RedirectToAction(nameof(Index));
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    ModelState.AddModelError("", $"API Registration Error: {error}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "API User registration failed. Simulating local insert.");
                
                // Simulated fallback
                var newId = MockUsers.Max(u => u.Id) + 1;
                MockUsers.Add(new UserListItemViewModel
                {
                    Id = newId,
                    FirstName = model.FirstName,
                    LastName = model.LastName,
                    Email = model.Email,
                    Phone = model.Phone,
                    Status = "Active",
                    OtpVerified = false,
                    KycStatus = "Pending",
                    CreatedAt = DateTime.Now
                });

                TempData["SuccessMessage"] = "User created successfully (Simulated Fallback).";
                return RedirectToAction(nameof(Index));
            }

            return View(model);
        }

        // GET: Users/Edit/5
        public async Task<IActionResult> Edit(long id)
        {
            ViewData["HeaderTitle"] = "Edit User Profile";
            ViewData["ShowBack"] = true;
            var client = GetApiClient();
            UserEditViewModel? editModel = null;

            try
            {
                var response = await client.GetAsync($"users/{id}");
                if (response.IsSuccessStatusCode)
                {
                    var details = await response.Content.ReadFromJsonAsync<UserDetailsViewModel>();
                    if (details != null)
                    {
                        editModel = new UserEditViewModel
                        {
                            Id = details.Id,
                            FirstName = details.FirstName,
                            LastName = details.LastName,
                            Email = details.Email,
                            Phone = details.Phone,
                            Status = details.Status,
                            KycStatus = details.KycStatus
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"API edit GET failed for user {id}. Using mock database.");
            }

            if (editModel == null)
            {
                var mockUser = MockUsers.FirstOrDefault(u => u.Id == id);
                if (mockUser == null)
                {
                    return NotFound();
                }

                editModel = new UserEditViewModel
                {
                    Id = mockUser.Id,
                    FirstName = mockUser.FirstName,
                    LastName = mockUser.LastName,
                    Email = mockUser.Email,
                    Phone = mockUser.Phone,
                    Status = mockUser.Status,
                    KycStatus = mockUser.KycStatus
                };
            }

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

            var client = GetApiClient();
            try
            {
                var response = await client.PutAsJsonAsync($"users/{id}", model);
                if (response.IsSuccessStatusCode)
                {
                    TempData["SuccessMessage"] = "User updated successfully via API.";
                    return RedirectToAction(nameof(Index));
                }
                
                ModelState.AddModelError("", "API failed to process updating details.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"API edit PUT failed for user {id}. Simulating update.");
                
                var mockUser = MockUsers.FirstOrDefault(u => u.Id == id);
                if (mockUser != null)
                {
                    mockUser.FirstName = model.FirstName;
                    mockUser.LastName = model.LastName;
                    mockUser.Email = model.Email;
                    mockUser.Phone = model.Phone;
                    mockUser.Status = model.Status;
                    mockUser.KycStatus = model.KycStatus;

                    TempData["SuccessMessage"] = "User updated successfully (Simulated Fallback).";
                    return RedirectToAction(nameof(Index));
                }
                return NotFound();
            }

            return View(model);
        }

        // POST: Users/ToggleStatus/5 (AJAX Preferred)
        [HttpPost]
        public async Task<IActionResult> ToggleStatus(long id)
        {
            var client = GetApiClient();
            try
            {
                var response = await client.PostAsync($"users/{id}/toggle-status", null);
                if (response.IsSuccessStatusCode)
                {
                    var newStatus = await response.Content.ReadAsStringAsync();
                    return Json(new { success = true, status = newStatus });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to toggle user {id} status via API. Applying mock update.");
            }

            // Fallback mock logic
            var user = MockUsers.FirstOrDefault(u => u.Id == id);
            if (user != null)
            {
                user.Status = user.Status == "Active" ? "Suspended" : "Active";
                return Json(new { success = true, status = user.Status, message = "Status toggled (Simulated)." });
            }

            return Json(new { success = false, message = "User not found." });
        }

        // POST: Users/UpdateKycStatus (AJAX Preferred)
        [HttpPost]
        public async Task<IActionResult> UpdateKycStatus(long id, string kycStatus)
        {
            if (string.IsNullOrWhiteSpace(kycStatus))
            {
                return Json(new { success = false, message = "KYC status is empty." });
            }

            var client = GetApiClient();
            try
            {
                var response = await client.PostAsJsonAsync($"users/{id}/kyc-status", new { status = kycStatus });
                if (response.IsSuccessStatusCode)
                {
                    return Json(new { success = true, kycStatus });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to update KYC status for user {id}. Applying mock update.");
            }

            // Fallback mock logic
            var user = MockUsers.FirstOrDefault(u => u.Id == id);
            if (user != null)
            {
                user.KycStatus = kycStatus;
                return Json(new { success = true, kycStatus, message = "KYC status updated (Simulated)." });
            }

            return Json(new { success = false, message = "User not found." });
        }

        // POST: Users/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(long id)
        {
            var client = GetApiClient();
            try
            {
                var response = await client.DeleteAsync($"users/{id}");
                if (response.IsSuccessStatusCode)
                {
                    TempData["SuccessMessage"] = "User deleted successfully via API.";
                    return RedirectToAction(nameof(Index));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to delete user {id} from API. Applying mock delete.");
            }

            var user = MockUsers.FirstOrDefault(u => u.Id == id);
            if (user != null)
            {
                MockUsers.Remove(user);
                TempData["SuccessMessage"] = "User deleted successfully (Simulated).";
                return RedirectToAction(nameof(Index));
            }

            return NotFound();
        }

        // GET: Users/Export
        public async Task<IActionResult> Export(string? searchQuery, string? statusFilter, string? kycStatusFilter)
        {
            var client = GetApiClient();
            List<UserListItemViewModel> usersList;

            try
            {
                var queryParams = $"?searchQuery={searchQuery}&statusFilter={statusFilter}&kycStatusFilter={kycStatusFilter}";
                var response = await client.GetAsync($"users{queryParams}");
                if (response.IsSuccessStatusCode)
                {
                    usersList = await response.Content.ReadFromJsonAsync<List<UserListItemViewModel>>() ?? [];
                }
                else
                {
                    usersList = [.. MockUsers];
                }
            }
            catch
            {
                usersList = [.. MockUsers];
            }

            // Apply filters to export data
            var query = usersList.AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchQuery))
            {
                var lowerSearch = searchQuery.ToLower();
                query = query.Where(u => u.FirstName.ToLower().Contains(lowerSearch) || 
                                         u.LastName.ToLower().Contains(lowerSearch) || 
                                         u.Email.ToLower().Contains(lowerSearch));
            }

            if (!string.IsNullOrWhiteSpace(statusFilter))
            {
                query = query.Where(u => u.Status.Equals(statusFilter, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrWhiteSpace(kycStatusFilter))
            {
                query = query.Where(u => u.KycStatus.Equals(kycStatusFilter, StringComparison.OrdinalIgnoreCase));
            }

            var usersToExport = query.ToList();

            // Generate CSV
            var csvBuilder = new StringBuilder();
            csvBuilder.AppendLine("User ID,Full Name,Email,Phone,Account Status,Verification Status,Registered Date");

            foreach (var user in usersToExport)
            {
                csvBuilder.AppendLine($"{user.Id},\"{user.FullName}\",\"{user.Email}\",\"{user.Phone}\",\"{user.Status}\",\"{user.KycStatus}\",\"{user.CreatedAt:yyyy-MM-dd HH:mm:ss}\"");
            }

            var csvData = Encoding.UTF8.GetBytes(csvBuilder.ToString());
            var result = new FileContentResult(csvData, "text/csv")
            {
                FileDownloadName = $"Tibr_Users_Export_{DateTime.Now:yyyyMMdd_HHmmss}.csv"
            };

            return result;
        }
    }
}
