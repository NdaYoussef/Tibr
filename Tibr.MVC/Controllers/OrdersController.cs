using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Tibr.Application.Dtos; // Maps directly to clean architecture DTOs
using Tibr.MVC.Models.Orders;

namespace Tibr.MVC.Controllers
{
    public class OrdersController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<OrdersController> _logger;

        // In-memory mock database for fallback testing
        private static readonly List<OrderListItemViewModel> MockOrdersList = new()
        {
            new() { Id = 1, OrderNumber = "TIBR-2026-0001", UserFullName = "Ahmad Al-Saeed", TotalAmount = 340.50m, OrderStatus = "Pending", PaymentStatus = "Paid", CreatedAt = DateTime.Now.AddDays(-1) },
            new() { Id = 2, OrderNumber = "TIBR-2026-0002", UserFullName = "Sarah Smith", TotalAmount = 120.00m, OrderStatus = "Processing", PaymentStatus = "Paid", CreatedAt = DateTime.Now.AddDays(-3) },
            new() { Id = 3, OrderNumber = "TIBR-2026-0003", UserFullName = "John Doe", TotalAmount = 95.99m, OrderStatus = "Shipped", PaymentStatus = "Paid", CreatedAt = DateTime.Now.AddDays(-5) },
            new() { Id = 4, OrderNumber = "TIBR-2026-0004", UserFullName = "Fatima Mansour", TotalAmount = 450.00m, OrderStatus = "Delivered", PaymentStatus = "Paid", CreatedAt = DateTime.Now.AddDays(-8) },
            new() { Id = 5, OrderNumber = "TIBR-2026-0005", UserFullName = "Liam Johnson", TotalAmount = 15.25m, OrderStatus = "Cancelled", PaymentStatus = "Refunded", CreatedAt = DateTime.Now.AddDays(-10) }
        };

        private static readonly Dictionary<long, OrderDetailsViewModel> MockOrderDetails = new()
        {
            {
                1, new OrderDetailsViewModel
                {
                    Id = 1,
                    OrderNumber = "TIBR-2026-0001",
                    UserId = 1,
                    UserFullName = "Ahmad Al-Saeed",
                    UserEmail = "ahmad.saeed@tibr.com",
                    UserPhone = "+962791234567",
                    TotalAmount = 340.50m,
                    OrderStatus = "Pending",
                    PaymentStatus = "Paid",
                    CreatedAt = DateTime.Now.AddDays(-1),
                    ShippingAddress = "Building 12, Rainbow Street, Amman, Jordan",
                    CourierName = "DHL Global Express",
                    Items = new List<OrderItemViewModel>
                    {
                        new() { Id = 201, ProductId = 10, ProductName = "Wireless Mechanical Keyboard", Quantity = 1, Price = 180.00m },
                        new() { Id = 202, ProductId = 11, ProductName = "Ergonomic Office Mouse", Quantity = 2, Price = 80.25m }
                    },
                    TrackingTimeline = new List<OrderTrackingMilestoneViewModel>
                    {
                        new() { Status = "Pending", UpdatedTime = DateTime.Now.AddDays(-1), Description = "Order received and payment confirmed.", IsCompleted = true },
                        new() { Status = "Processing", UpdatedTime = DateTime.Now.AddHours(-12), Description = "Order is being packaged and prepared.", IsCompleted = false },
                        new() { Status = "Shipped", UpdatedTime = DateTime.MinValue, Description = "Waiting for shipment processing.", IsCompleted = false },
                        new() { Status = "Delivered", UpdatedTime = DateTime.MinValue, Description = "Waiting for delivery.", IsCompleted = false }
                    }
                }
            },
            {
                2, new OrderDetailsViewModel
                {
                    Id = 2,
                    OrderNumber = "TIBR-2026-0002",
                    UserId = 2,
                    UserFullName = "Sarah Smith",
                    UserEmail = "sarah.smith@tibr.com",
                    UserPhone = "+12025550143",
                    TotalAmount = 120.00m,
                    OrderStatus = "Processing",
                    PaymentStatus = "Paid",
                    CreatedAt = DateTime.Now.AddDays(-3),
                    ShippingAddress = "742 Evergreen Terrace, Springfield, USA",
                    CourierName = "FedEx Economy",
                    Items = new List<OrderItemViewModel>
                    {
                        new() { Id = 203, ProductId = 15, ProductName = "USB-C Hub Multiport Adapter", Quantity = 3, Price = 40.00m }
                    },
                    TrackingTimeline = new List<OrderTrackingMilestoneViewModel>
                    {
                        new() { Status = "Pending", UpdatedTime = DateTime.Now.AddDays(-3), Description = "Payment processed successfully.", IsCompleted = true },
                        new() { Status = "Processing", UpdatedTime = DateTime.Now.AddDays(-2), Description = "Items picked and packing completed.", IsCompleted = true },
                        new() { Status = "Shipped", UpdatedTime = DateTime.MinValue, Description = "Waiting for transit dispatch.", IsCompleted = false },
                        new() { Status = "Delivered", UpdatedTime = DateTime.MinValue, Description = "Waiting for courier drop-off.", IsCompleted = false }
                    }
                }
            },
            {
                3, new OrderDetailsViewModel
                {
                    Id = 3,
                    OrderNumber = "TIBR-2026-0003",
                    UserId = 3,
                    UserFullName = "John Doe",
                    UserEmail = "john.doe@tibr.com",
                    UserPhone = "+14155552671",
                    TotalAmount = 95.99m,
                    OrderStatus = "Shipped",
                    PaymentStatus = "Paid",
                    CreatedAt = DateTime.Now.AddDays(-5),
                    ShippingAddress = "555 Market Street, San Francisco, CA, USA",
                    CourierName = "UPS Ground",
                    Items = new List<OrderItemViewModel>
                    {
                        new() { Id = 204, ProductId = 22, ProductName = "Waterproof Hiking Backpack", Quantity = 1, Price = 95.99m }
                    },
                    TrackingTimeline = new List<OrderTrackingMilestoneViewModel>
                    {
                        new() { Status = "Pending", UpdatedTime = DateTime.Now.AddDays(-5), Description = "Order logged.", IsCompleted = true },
                        new() { Status = "Processing", UpdatedTime = DateTime.Now.AddDays(-4), Description = "Order quality checks passed.", IsCompleted = true },
                        new() { Status = "Shipped", UpdatedTime = DateTime.Now.AddDays(-3), Description = "Shipped via UPS (Tracking #1Z9999W99999999999).", IsCompleted = true },
                        new() { Status = "Delivered", UpdatedTime = DateTime.MinValue, Description = "Out for delivery in destination city.", IsCompleted = false }
                    }
                }
            },
            {
                4, new OrderDetailsViewModel
                {
                    Id = 4,
                    OrderNumber = "TIBR-2026-0004",
                    UserId = 4,
                    UserFullName = "Fatima Mansour",
                    UserEmail = "fatima.m@tibr.com",
                    UserPhone = "+966501234567",
                    TotalAmount = 450.00m,
                    OrderStatus = "Delivered",
                    PaymentStatus = "Paid",
                    CreatedAt = DateTime.Now.AddDays(-8),
                    ShippingAddress = "Olaya District, King Fahd Rd, Riyadh, Saudi Arabia",
                    CourierName = "Aramex Logistics",
                    Items = new List<OrderItemViewModel>
                    {
                        new() { Id = 205, ProductId = 30, ProductName = "Active Noise Cancelling Headphones", Quantity = 1, Price = 300.00m },
                        new() { Id = 206, ProductId = 31, ProductName = "Bluetooth Smart Watch Lite", Quantity = 1, Price = 150.00m }
                    },
                    TrackingTimeline = new List<OrderTrackingMilestoneViewModel>
                    {
                        new() { Status = "Pending", UpdatedTime = DateTime.Now.AddDays(-8), Description = "Payment confirmed.", IsCompleted = true },
                        new() { Status = "Processing", UpdatedTime = DateTime.Now.AddDays(-7), Description = "Order packed and scanned.", IsCompleted = true },
                        new() { Status = "Shipped", UpdatedTime = DateTime.Now.AddDays(-6), Description = "In transit through logistics hub.", IsCompleted = true },
                        new() { Status = "Delivered", UpdatedTime = DateTime.Now.AddDays(-4), Description = "Package delivered and signed by customer.", IsCompleted = true }
                    }
                }
            },
            {
                5, new OrderDetailsViewModel
                {
                    Id = 5,
                    OrderNumber = "TIBR-2026-0005",
                    UserId = 5,
                    UserFullName = "Liam Johnson",
                    UserEmail = "liam.j@tibr.com",
                    UserPhone = "+442079460192",
                    TotalAmount = 15.25m,
                    OrderStatus = "Cancelled",
                    PaymentStatus = "Refunded",
                    CreatedAt = DateTime.Now.AddDays(-10),
                    ShippingAddress = "221B Baker Street, London, UK",
                    CourierName = null,
                    Items = new List<OrderItemViewModel>
                    {
                        new() { Id = 207, ProductId = 40, ProductName = "Antiglare Screen Cleaner", Quantity = 1, Price = 15.25m }
                    },
                    TrackingTimeline = new List<OrderTrackingMilestoneViewModel>
                    {
                        new() { Status = "Pending", UpdatedTime = DateTime.Now.AddDays(-10), Description = "Order initialized.", IsCompleted = true },
                        new() { Status = "Cancelled", UpdatedTime = DateTime.Now.AddDays(-9), Description = "Customer requested cancellation. Payment refunded to Visa card.", IsCompleted = true }
                    }
                }
            }
        };

        public OrdersController(IHttpClientFactory httpClientFactory, ILogger<OrdersController> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        private HttpClient GetApiClient()
        {
            var client = _httpClientFactory.CreateClient("TibrApi");
            var token = Request.Cookies["JwtToken"];
            if (!string.IsNullOrEmpty(token))
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }
            return client;
        }

        // GET: Orders
        public async Task<IActionResult> Index(string? searchQuery, string? statusFilter, string? paymentStatusFilter, int page = 1)
        {
            ViewData["HeaderTitle"] = "Executive Suite";
            ViewData["ShowBack"] = false;
            const int pageSize = 5;
            var client = GetApiClient();
            List<OrderListItemViewModel> ordersList = [];

            try
            {
                // Call GET /api/orders
                var response = await client.GetAsync("orders");
                if (response.IsSuccessStatusCode)
                {
                    var apiOrders = await response.Content.ReadFromJsonAsync<List<OrderDto>>() ?? [];
                    ordersList = apiOrders.Select(o => new OrderListItemViewModel
                    {
                        Id = o.Id,
                        OrderNumber = o.OrderNumber,
                        UserFullName = o.UserFullName,
                        TotalAmount = o.TotalAmount,
                        OrderStatus = o.OrderStatus,
                        PaymentStatus = o.PaymentStatus,
                        CreatedAt = o.CreatedAt
                    }).ToList();
                }
                else
                {
                    _logger.LogWarning($"API returned error code {response.StatusCode}. Using mock database fallback.");
                    ordersList = [.. MockOrdersList];
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "API Orders fetch failed. Falling back to local simulated DB.");
                ordersList = [.. MockOrdersList];
            }

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
            var client = GetApiClient();
            OrderDetailsViewModel? details = null;

            try
            {
                var response = await client.GetAsync($"orders/{id}");
                if (response.IsSuccessStatusCode)
                {
                    var apiOrder = await response.Content.ReadFromJsonAsync<OrderDto>();
                    if (apiOrder != null)
                    {
                        details = new OrderDetailsViewModel
                        {
                            Id = apiOrder.Id,
                            OrderNumber = apiOrder.OrderNumber,
                            UserId = apiOrder.UserId,
                            UserFullName = apiOrder.UserFullName,
                            TotalAmount = apiOrder.TotalAmount,
                            OrderStatus = apiOrder.OrderStatus,
                            PaymentStatus = apiOrder.PaymentStatus,
                            CreatedAt = apiOrder.CreatedAt,
                            ShippingAddress = "N/A - Fetched from backend user profile",
                            CourierName = "Assigned Operator",
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
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to fetch details for order {id}. Using mock database.");
            }

            if (details == null)
            {
                if (MockOrderDetails.TryGetValue(id, out var mockDetails))
                {
                    details = mockDetails;
                }
                else
                {
                    return NotFound($"Order with ID {id} not found.");
                }
            }

            // Format order number like TR-9821
            var formattedNumber = details.OrderNumber.Contains("2026-") 
                ? details.OrderNumber.Replace("TIBR-2026-000", "TR-") 
                : details.OrderNumber.Replace("TIBR-", "TR-");
            
            ViewData["HeaderTitle"] = $"Order #{formattedNumber}";
            ViewData["ShowBack"] = true;

            return View(details);
        }

        // POST: Orders/UpdateStatus (GET/POST pattern)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(long id, string orderStatus, string paymentStatus)
        {
            var client = GetApiClient();
            var updateDto = new UpdateOrderDto
            {
                OrderStatus = orderStatus,
                PaymentStatus = paymentStatus
            };

            try
            {
                var response = await client.PutAsJsonAsync($"orders/{id}", updateDto);
                if (response.IsSuccessStatusCode)
                {
                    TempData["SuccessMessage"] = "Order status updated successfully via API.";
                    return RedirectToAction(nameof(Details), new { id });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to update status for order {id}. Updating simulated database.");
            }

            // Fallback mock update
            if (MockOrderDetails.TryGetValue(id, out var mockDetails))
            {
                mockDetails.OrderStatus = orderStatus;
                mockDetails.PaymentStatus = paymentStatus;

                // Sync with general index list
                var listOrder = MockOrdersList.FirstOrDefault(o => o.Id == id);
                if (listOrder != null)
                {
                    listOrder.OrderStatus = orderStatus;
                    listOrder.PaymentStatus = paymentStatus;
                }

                // Add to tracking timeline
                var isCompleted = orderStatus == "Delivered";
                mockDetails.TrackingTimeline.Add(new OrderTrackingMilestoneViewModel
                {
                    Status = orderStatus,
                    UpdatedTime = DateTime.Now,
                    Description = $"Order status advanced to {orderStatus}. Payment status is {paymentStatus}.",
                    IsCompleted = isCompleted
                });

                TempData["SuccessMessage"] = "Order status updated successfully (Simulated).";
                return RedirectToAction(nameof(Details), new { id });
            }

            return NotFound();
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

            var client = GetApiClient();
            try
            {
                // In clean architecture, we can extend the update API or call a specific sub-route
                var response = await client.PostAsJsonAsync($"orders/{id}/assign-delivery", new { courierName });
                if (response.IsSuccessStatusCode)
                {
                    TempData["SuccessMessage"] = $"Courier assigned successfully via API.";
                    return RedirectToAction(nameof(Details), new { id });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"API delivery assignment failed for order {id}. Updating mock database.");
            }

            // Fallback mock update
            if (MockOrderDetails.TryGetValue(id, out var mockDetails))
            {
                mockDetails.CourierName = courierName;
                
                // Add timeline entry
                mockDetails.TrackingTimeline.Add(new OrderTrackingMilestoneViewModel
                {
                    Status = "Shipped",
                    UpdatedTime = DateTime.Now,
                    Description = $"Delivery assigned to: {courierName}.",
                    IsCompleted = true
                });

                // Advance status to Shipped
                mockDetails.OrderStatus = "Shipped";
                var listOrder = MockOrdersList.FirstOrDefault(o => o.Id == id);
                if (listOrder != null)
                {
                    listOrder.OrderStatus = "Shipped";
                }

                TempData["SuccessMessage"] = $"Order assigned to {courierName} & dispatched (Simulated).";
                return RedirectToAction(nameof(Details), new { id });
            }

            return NotFound();
        }

        // POST: Orders/CancelAndRefund
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelAndRefund(long id)
        {
            var client = GetApiClient();
            try
            {
                // Call DELETE or custom cancellation route
                var response = await client.PostAsync($"orders/{id}/cancel", null);
                if (response.IsSuccessStatusCode)
                {
                    TempData["SuccessMessage"] = "Order cancelled and refund requested via API.";
                    return RedirectToAction(nameof(Details), new { id });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to cancel order {id} via API. Applying mock cancellation.");
            }

            // Fallback mock update
            if (MockOrderDetails.TryGetValue(id, out var mockDetails))
            {
                mockDetails.OrderStatus = "Cancelled";
                mockDetails.PaymentStatus = "Refunded";

                var listOrder = MockOrdersList.FirstOrDefault(o => o.Id == id);
                if (listOrder != null)
                {
                    listOrder.OrderStatus = "Cancelled";
                    listOrder.PaymentStatus = "Refunded";
                }

                mockDetails.TrackingTimeline.Add(new OrderTrackingMilestoneViewModel
                {
                    Status = "Cancelled",
                    UpdatedTime = DateTime.Now,
                    Description = "Order has been cancelled by Admin. Payment status changed to Refunded.",
                    IsCompleted = true
                });

                TempData["SuccessMessage"] = "Order cancelled and refund finalized (Simulated).";
                return RedirectToAction(nameof(Details), new { id });
            }

            return NotFound();
        }

        // GET: Orders/Invoice/5
        public async Task<IActionResult> Invoice(long id)
        {
            var client = GetApiClient();
            OrderDetailsViewModel? details = null;

            try
            {
                var response = await client.GetAsync($"orders/{id}");
                if (response.IsSuccessStatusCode)
                {
                    var apiOrder = await response.Content.ReadFromJsonAsync<OrderDto>();
                    if (apiOrder != null)
                    {
                        details = new OrderDetailsViewModel
                        {
                            Id = apiOrder.Id,
                            OrderNumber = apiOrder.OrderNumber,
                            UserId = apiOrder.UserId,
                            UserFullName = apiOrder.UserFullName,
                            TotalAmount = apiOrder.TotalAmount,
                            OrderStatus = apiOrder.OrderStatus,
                            PaymentStatus = apiOrder.PaymentStatus,
                            CreatedAt = apiOrder.CreatedAt,
                            ShippingAddress = "123 Main Street, City",
                            Items = apiOrder.Items.Select(item => new OrderItemViewModel
                            {
                                Id = item.Id,
                                ProductId = item.ProductId,
                                ProductName = item.ProductName,
                                Quantity = item.Quantity,
                                Price = item.Price
                            }).ToList()
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Invoice load failed via API. Falling back to mock details for ID {id}.");
            }

            if (details == null)
            {
                if (MockOrderDetails.TryGetValue(id, out var mockDetails))
                {
                    details = mockDetails;
                }
                else
                {
                    return NotFound();
                }
            }

            var invoice = new InvoiceViewModel
            {
                InvoiceNumber = $"INV-{details.OrderNumber.Replace("TIBR-", "")}",
                InvoiceDate = details.CreatedAt.AddHours(2),
                DueDate = details.CreatedAt.AddDays(7),
                OrderDetails = details
            };

            return View(invoice);
        }
    }
}
