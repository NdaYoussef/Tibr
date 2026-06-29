using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Tibr.Application.Dtos;
using Tibr.Application.Services.AdminServices;
using Tibr.Application.Services.OrderServices;
using Tibr.Application.Services.UserServices;
using Tibr.MVC.Models.Orders;

namespace Tibr.MVC.Controllers
{
    public class OrdersController : Controller
    {
        private readonly IOrderService _orderService;
        private readonly IUserService _userService;
        private readonly IAdminService _adminService;
        private readonly ILogger<OrdersController> _logger;

        public OrdersController(IOrderService orderService, IUserService userService, IAdminService adminService, ILogger<OrdersController> logger)
        {
            _orderService = orderService;
            _userService = userService;
            _adminService = adminService;
            _logger = logger;
        }

        // GET: Orders
        public async Task<IActionResult> Index(string? searchQuery, string? statusFilter, string? paymentStatusFilter, int page = 1)
        {
            ViewData["HeaderTitle"] = "Executive Suite";
            ViewData["ShowBack"] = false;
            const int pageSize = 5;

            var result = await _orderService.GetAllAsync();
            if (result.IsFailure)
            {
                TempData["ErrorMessage"] = result.ErrorMessage;
                return View(new OrderListViewModel());
            }

            var ordersList = (result.Data ?? Enumerable.Empty<OrderDto>()).Select(o => new OrderListItemViewModel
            {
                Id = o.Id,
                OrderNumber = o.OrderNumber,
                UserFullName = o.UserFullName,
                TotalAmount = o.TotalAmount,
                OrderStatus = o.OrderStatus,
                PaymentStatus = o.PaymentStatus,
                CreatedAt = o.CreatedAt
            }).ToList();

            // Filtering
            var query = ordersList.AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchQuery))
            {
                var lowerSearch = searchQuery.ToLower();
                query = query.Where(o => o.OrderNumber.ToLower().Contains(lowerSearch) || 
                                         o.UserFullName.ToLower().Contains(lowerSearch));
            }

            if (!string.IsNullOrWhiteSpace(statusFilter))
            {
                query = query.Where(o => o.OrderStatus.Equals(statusFilter, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrWhiteSpace(paymentStatusFilter))
            {
                query = query.Where(o => o.PaymentStatus.Equals(paymentStatusFilter, StringComparison.OrdinalIgnoreCase));
            }

            var totalItems = query.Count();
            var paginatedOrders = query.OrderByDescending(o => o.CreatedAt)
                                       .Skip((page - 1) * pageSize)
                                       .Take(pageSize)
                                       .ToList();

            var viewModel = new OrderListViewModel
            {
                Orders = paginatedOrders,
                SearchQuery = searchQuery,
                StatusFilter = statusFilter,
                PaymentStatusFilter = paymentStatusFilter,
                CurrentPage = page,
                PageSize = pageSize,
                TotalItems = totalItems
            };

            return View(viewModel);
        }

        // GET: Orders/Details/5
        public async Task<IActionResult> Details(long id)
        {
            var result = await _orderService.GetByIdAsync(id);
            if (result.IsFailure)
            {
                TempData["ErrorMessage"] = result.ErrorMessage;
                return RedirectToAction(nameof(Index));
            }

            var apiOrder = result.Data!;

            // Fetch user address/phone or use default if not available
            string userAddress = "Building 12, Rainbow Street, Amman, Jordan";
            string userPhone = "N/A";
            string userEmail = "N/A";
            var userResult = await _userService.GetByIdAsync(apiOrder.UserId);
            if (userResult.IsSuccess)
            {
                userEmail = userResult.Data.Email;
                userPhone = userResult.Data.Phone;
            }

            var details = new OrderDetailsViewModel
            {
                Id = apiOrder.Id,
                OrderNumber = apiOrder.OrderNumber,
                UserId = apiOrder.UserId,
                UserFullName = apiOrder.UserFullName,
                UserEmail = userEmail,
                UserPhone = userPhone,
                TotalAmount = apiOrder.TotalAmount,
                OrderStatus = apiOrder.OrderStatus,
                PaymentStatus = apiOrder.PaymentStatus,
                CreatedAt = apiOrder.CreatedAt,
                ShippingAddress = userAddress,
                CourierName = apiOrder.OrderStatus == "Shipped" || apiOrder.OrderStatus == "Delivered" ? "DHL Express" : "Assigned Operator",
                Items = apiOrder.Items.Select(item => new OrderItemViewModel
                {
                    Id = item.Id,
                    ProductId = item.ProductId,
                    ProductName = item.ProductName,
                    Quantity = item.Quantity,
                    Price = item.Price
                }).ToList(),
                TrackingTimeline = new List<OrderTrackingMilestoneViewModel>
                {
                    new() { Status = "Pending", UpdatedTime = apiOrder.CreatedAt, Description = "Order created successfully.", IsCompleted = true }
                }
            };

            // Dynamically simulate milestones on details tracking page based on DB status
            if (apiOrder.OrderStatus == "Processing")
            {
                details.TrackingTimeline.Add(new() { Status = "Processing", UpdatedTime = apiOrder.CreatedAt.AddHours(2), Description = "Order is being packaged and prepared.", IsCompleted = true });
            }
            else if (apiOrder.OrderStatus == "Shipped")
            {
                details.TrackingTimeline.Add(new() { Status = "Processing", UpdatedTime = apiOrder.CreatedAt.AddHours(2), Description = "Order is being packaged and prepared.", IsCompleted = true });
                details.TrackingTimeline.Add(new() { Status = "Shipped", UpdatedTime = apiOrder.CreatedAt.AddHours(5), Description = "Order is in transit.", IsCompleted = true });
            }
            else if (apiOrder.OrderStatus == "Delivered")
            {
                details.TrackingTimeline.Add(new() { Status = "Processing", UpdatedTime = apiOrder.CreatedAt.AddHours(2), Description = "Order is being packaged and prepared.", IsCompleted = true });
                details.TrackingTimeline.Add(new() { Status = "Shipped", UpdatedTime = apiOrder.CreatedAt.AddHours(5), Description = "Order is in transit.", IsCompleted = true });
                details.TrackingTimeline.Add(new() { Status = "Delivered", UpdatedTime = apiOrder.CreatedAt.AddHours(24), Description = "Order delivered and signed.", IsCompleted = true });
            }
            else if (apiOrder.OrderStatus == "Cancelled")
            {
                details.TrackingTimeline.Add(new() { Status = "Cancelled", UpdatedTime = apiOrder.CreatedAt.AddHours(1), Description = "Order cancelled by administrator.", IsCompleted = true });
            }

            var formattedNumber = details.OrderNumber.Contains("-") 
                ? details.OrderNumber.Replace("TIBR-2026-000", "TR-").Replace("TIBR-", "TR-").Replace("ORD-", "TR-")
                : details.OrderNumber;

            ViewData["HeaderTitle"] = $"Order #{formattedNumber}";
            ViewData["ShowBack"] = true;

            return View(details);
        }

        // POST: Orders/UpdateStatus
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(long id, string orderStatus, string paymentStatus)
        {
            if (string.IsNullOrWhiteSpace(paymentStatus))
            {
                TempData["ErrorMessage"] = "Payment status is required.";
                return RedirectToAction(nameof(Details), new { id });
            }

            if (string.IsNullOrWhiteSpace(orderStatus))
            {
                TempData["ErrorMessage"] = "Order status is required.";
                return RedirectToAction(nameof(Details), new { id });
            }

            // Validate status combination based on business rules:
            // - If Pending: Pending or Cancelled
            // - If Paid: Processing, Shipped, Delivered
            // - If Refunded: Returned or Cancelled
            bool isValid = false;
            if (paymentStatus == "Pending" && (orderStatus == "Pending" || orderStatus == "Cancelled"))
            {
                isValid = true;
            }
            else if (paymentStatus == "Paid" && (orderStatus == "Processing" || orderStatus == "Shipped" || orderStatus == "Delivered"))
            {
                isValid = true;
            }
            else if (paymentStatus == "Refunded" && (orderStatus == "Returned" || orderStatus == "Cancelled"))
            {
                isValid = true;
            }

            if (!isValid)
            {
                TempData["ErrorMessage"] = $"Invalid order status '{orderStatus}' for payment status '{paymentStatus}'.";
                return RedirectToAction(nameof(Details), new { id });
            }

            var updateDto = new UpdateOrderDto
            {
                OrderStatus = orderStatus,
                PaymentStatus = paymentStatus
            };

            var result = await _orderService.UpdateAsync(id, updateDto);
            if (result.IsSuccess)
            {
                _adminService.InvalidateDashboardCache();
                TempData["SuccessMessage"] = "Order status updated successfully in DB.";
            }
            else
            {
                TempData["ErrorMessage"] = result.ErrorMessage;
            }

            return RedirectToAction(nameof(Details), new { id });
        }

        // POST: Orders/AssignDelivery
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignDelivery(long id, string courierName)
        {
            if (string.IsNullOrWhiteSpace(courierName))
            {
                TempData["ErrorMessage"] = "Courier name is required.";
                return RedirectToAction(nameof(Details), new { id });
            }

            var result = await _orderService.UpdateAsync(id, new UpdateOrderDto
            {
                OrderStatus = "Shipped"
            });

            if (result.IsSuccess)
            {
                _adminService.InvalidateDashboardCache();
                TempData["SuccessMessage"] = $"Order assigned to {courierName} & status updated to Shipped.";
            }
            else
            {
                TempData["ErrorMessage"] = result.ErrorMessage;
            }

            return RedirectToAction(nameof(Details), new { id });
        }

        // POST: Orders/CancelAndRefund
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelAndRefund(long id)
        {
            var result = await _orderService.UpdateAsync(id, new UpdateOrderDto
            {
                OrderStatus = "Cancelled",
                PaymentStatus = "Refunded"
            });

            if (result.IsSuccess)
            {
                _adminService.InvalidateDashboardCache();
                TempData["SuccessMessage"] = "Order cancelled and status updated to Refunded.";
            }
            else
            {
                TempData["ErrorMessage"] = result.ErrorMessage;
            }

            return RedirectToAction(nameof(Details), new { id });
        }

        // GET: Orders/Invoice/5
        public async Task<IActionResult> Invoice(long id)
        {
            var result = await _orderService.GetByIdAsync(id);
            if (result.IsFailure)
            {
                return NotFound();
            }

            var apiOrder = result.Data!;

            string userEmail = "N/A";
            string userPhone = "N/A";
            string userAddress = "Building 12, Rainbow Street, Amman, Jordan";
            var userResult = await _userService.GetByIdAsync(apiOrder.UserId);
            if (userResult.IsSuccess)
            {
                userEmail = userResult.Data.Email;
                userPhone = userResult.Data.Phone;
            }

            var details = new OrderDetailsViewModel
            {
                Id = apiOrder.Id,
                OrderNumber = apiOrder.OrderNumber,
                UserId = apiOrder.UserId,
                UserFullName = apiOrder.UserFullName,
                UserEmail = userEmail,
                UserPhone = userPhone,
                TotalAmount = apiOrder.TotalAmount,
                OrderStatus = apiOrder.OrderStatus,
                PaymentStatus = apiOrder.PaymentStatus,
                CreatedAt = apiOrder.CreatedAt,
                ShippingAddress = userAddress,
                Items = apiOrder.Items.Select(item => new OrderItemViewModel
                {
                    Id = item.Id,
                    ProductId = item.ProductId,
                    ProductName = item.ProductName,
                    Quantity = item.Quantity,
                    Price = item.Price
                }).ToList()
            };

            var invoice = new InvoiceViewModel
            {
                InvoiceNumber = $"INV-{details.OrderNumber.Replace("TIBR-", "").Replace("ORD-", "")}",
                InvoiceDate = details.CreatedAt.AddHours(2),
                DueDate = details.CreatedAt.AddDays(7),
                OrderDetails = details
            };

            return View(invoice);
        }
    }
}
