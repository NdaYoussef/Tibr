using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Tibr.Application.Constants;
using Tibr.Application.Dtos.DashboardDtos;
using Tibr.Domain.Entities;
using Tibr.Domain.IRepositories;

namespace Tibr.Application.Services.AdminServices
{
    public class AnalyticsService : IAnalyticsService
    {
        private readonly IGenericRepository<Order, long> _orderRepo;
        private readonly IGenericRepository<OrderItem, long> _orderItemRepo;
        private readonly IGenericRepository<User, long> _userRepo;
        private readonly IGenericRepository<Product, long> _productRepo;
        private readonly ILogger<AnalyticsService> _logger;

        public AnalyticsService(
            IGenericRepository<Order, long> orderRepo,
            IGenericRepository<OrderItem, long> orderItemRepo,
            IGenericRepository<User, long> userRepo,
            IGenericRepository<Product, long> productRepo,
            ILogger<AnalyticsService> logger)
        {
            _orderRepo = orderRepo;
            _orderItemRepo = orderItemRepo;
            _userRepo = userRepo;
            _productRepo = productRepo;
            _logger = logger;
        }

        //  SUMMARY 
        public async Task<ReportsSummaryDto> GetReportsSummaryAsync(
            DateTime fromDate,
            DateTime toDate)
        {
            _logger.LogDebug("GetReportsSummaryAsync {From} – {To}", fromDate, toDate);

            var from = fromDate.Date.ToUniversalTime();
            var to = toDate.Date.AddDays(1).ToUniversalTime();


            var revenue = await _orderRepo
                .GetAll(o => !o.IsDeleted
                          && o.PaymentStatus == PaymentStatusConstants.Paid
                          && o.CreatedAt >= from
                          && o.CreatedAt < to)
                .SumAsync(o => (decimal?)o.TotalAmount);

            var orders = await _orderRepo
                .GetAll(o => !o.IsDeleted
                          && o.CreatedAt >= from
                          && o.CreatedAt < to)
                .CountAsync();

            var customers = await _userRepo
                .GetAll(u => !u.IsDeleted)
                .CountAsync();

            var products = await _productRepo
                .GetAll(p => !p.IsDeleted)
                .CountAsync();

            return new ReportsSummaryDto
            {
                TotalRevenue = revenue ?? 0m,
                TotalOrders = orders,
                TotalCustomers = customers,
                TotalProducts = products
            };
        }

        //  SALES REPORT 
        public async Task<List<SalesReportDto>> GetSalesReportAsync(
            DateTime fromDate,
            DateTime toDate)
        {
            _logger.LogDebug("GetSalesReportAsync {From} – {To}", fromDate, toDate);

            var from = fromDate.Date.ToUniversalTime();
            var to = toDate.Date.AddDays(1).ToUniversalTime();

            return await _orderRepo
                .GetAll(o => !o.IsDeleted
                          && o.CreatedAt >= from
                          && o.CreatedAt < to)
                .OrderByDescending(o => o.CreatedAt)
                .Select(o => new SalesReportDto
                {
                    OrderNumber = o.OrderNumber,
                    CustomerName = o.User != null
                        ? (o.User.FirstName + " " + o.User.LastName).Trim()
                        : "—",
                    Date = o.CreatedAt,
                    Amount = o.TotalAmount,
                    OrderStatus = o.OrderStatus,
                    PaymentStatus = o.PaymentStatus
                })
                .ToListAsync();
        }

        //  REVENUE REPORT (Paid orders, grouped by product)
        //   - only Paid orders are counted
        //   - grouped by Product (not by Order)
        //   - calculates TotalCost (BuyPrice * Qty) and NetMargin per product
        public async Task<List<RevenueReportDto>> GetRevenueReportAsync(
           DateTime fromDate,
           DateTime toDate)
        {
            _logger.LogDebug("GetRevenueReportAsync {From} – {To}", fromDate, toDate);

            var from = fromDate.Date.ToUniversalTime();
            var to = toDate.Date.AddDays(1).ToUniversalTime();

            var raw = await _orderItemRepo
                .GetAll(oi =>
                    !oi.Order.IsDeleted
                    && oi.Order.PaymentStatus == PaymentStatusConstants.Paid
                    && oi.Order.CreatedAt >= from
                    && oi.Order.CreatedAt < to)
                .GroupBy(oi => new
                {
                    oi.ProductId,
                    oi.Product.Name,
                    oi.Product.MetalType,
                    oi.Product.BuyPrice
                })
                .Select(g => new
                {
                    g.Key.Name,
                    g.Key.MetalType,
                    g.Key.BuyPrice,
                    UnitsSold = g.Sum(x => x.Quantity),
                    TotalRevenue = g.Sum(x => x.Price * x.Quantity)
                })
                .ToListAsync();

            return raw
                .Select(r => new RevenueReportDto
                {
                    ProductName = r.Name,
                    MetalType = r.MetalType.ToString(),
                    UnitsSold = r.UnitsSold,
                    TotalRevenue = r.TotalRevenue,
                    TotalCost = r.BuyPrice * r.UnitsSold
                })
                .OrderByDescending(r => r.NetMargin)
                .ToList();
        }

        //  PRODUCT PERFORMANCE 
        public async Task<List<ProductPerformanceDto>> GetProductPerformanceReportAsync(
            DateTime fromDate,
            DateTime toDate)
        {
            _logger.LogDebug(
                "GetProductPerformanceReportAsync {From} – {To}", fromDate, toDate);

            var from = fromDate.Date.ToUniversalTime();
            var to = toDate.Date.AddDays(1).ToUniversalTime();


            var soldByProduct = await _orderItemRepo
                .GetAll(oi => !oi.Order.IsDeleted
                           && oi.Order.CreatedAt >= from
                           && oi.Order.CreatedAt < to)
                .GroupBy(oi => oi.ProductId)
                .Select(g => new
                {
                    ProductId = g.Key,
                    TotalSold = g.Sum(x => x.Quantity),
                    TotalRevenue = g.Sum(x => x.Price * x.Quantity)
                })
                .ToListAsync();

            var products = await _productRepo
                .GetAll(p => !p.IsDeleted)
                .Select(p => new
                {
                    p.Id,
                    p.Name,
                    MetalType = p.MetalType.ToString(),
                    p.Stock,
                    Status = p.Status.ToString()
                })
                .ToListAsync();

            var soldLookup = soldByProduct.ToDictionary(x => x.ProductId);

            return products
                .Select(p =>
                {
                    soldLookup.TryGetValue(p.Id, out var sold);
                    return new ProductPerformanceDto
                    {
                        ProductName = p.Name,
                        MetalType = p.MetalType,
                        TotalSold = sold?.TotalSold ?? 0,
                        Revenue = sold?.TotalRevenue ?? 0m,
                        Stock = p.Stock,
                        Status = p.Status
                    };
                })
                .OrderByDescending(r => r.TotalSold)
                .ToList();
        }

        //  INVENTORY REPORT 
        public async Task<List<InventoryDto>> GetInventoryReportAsync()
        {
            _logger.LogDebug("GetInventoryReportAsync");


            return await _productRepo
                .GetAll(p => !p.IsDeleted)
                .OrderBy(p => p.Stock)
                .Select(p => new InventoryDto
                {
                    ProductName = p.Name,
                    Category = p.Category != null ? p.Category.Name : "—",
                    MetalType = p.MetalType.ToString(),
                    Stock = p.Stock,
                    StockStatus = p.Stock == 0 ? "Out"
                                : p.Stock <= StockThresholds.Low ? "Low"
                                                                    : "OK",
                    BuyPrice = p.BuyPrice,
                    SellPrice = p.SellPrice
                })
                .ToListAsync();
        }

        // ── CUSTOMER REPORT ──────────────────────────────────────────────────
        public async Task<List<CustomerReportDto>> GetCustomerReportAsync()
        {
            _logger.LogDebug("GetCustomerReportAsync");

            return await _userRepo
                .GetAll(u => !u.IsDeleted)
                .Select(u => new CustomerReportDto
                {
                    CustomerName = (u.FirstName + " " + u.LastName).Trim(),
                    Email = u.Email,
                    TotalOrders = u.Orders.Count(o => !o.IsDeleted),
                    TotalSpent = u.Orders
                        .Where(o => !o.IsDeleted
                                 && o.PaymentStatus == PaymentStatusConstants.Paid)
                        .Sum(o => (decimal?)o.TotalAmount) ?? 0m,
                    JoinedAt = u.CreatedAt,
                    KycStatus = u.KycStatus
                })
                .OrderByDescending(c => c.TotalSpent)
                .ToListAsync();
        }

        //  MONTHLY SALES CHART 
        public async Task<List<MonthlySalesDto>> GetMonthlySalesChartAsync()
        {
            _logger.LogDebug("GetMonthlySalesChartAsync");

            var now = DateTime.UtcNow;
            var twelveMonthsAgo = now.AddMonths(-11);

            var rawRevenue = await _orderRepo
                .GetAll(o => !o.IsDeleted
                          && o.PaymentStatus == PaymentStatusConstants.Paid
                          && o.CreatedAt >= twelveMonthsAgo)
                .GroupBy(o => new { o.CreatedAt.Year, o.CreatedAt.Month })
                .Select(g => new
                {
                    g.Key.Year,
                    g.Key.Month,
                    Revenue = g.Sum(o => o.TotalAmount)
                })
                .ToListAsync();

            var rawOrders = await _orderRepo
                .GetAll(o => !o.IsDeleted
                          && o.CreatedAt >= twelveMonthsAgo)
                .GroupBy(o => new { o.CreatedAt.Year, o.CreatedAt.Month })
                .Select(g => new
                {
                    g.Key.Year,
                    g.Key.Month,
                    Orders = g.Count()
                })
                .ToListAsync();

            return Enumerable.Range(0, 12)
                .Select(i =>
                {
                    var d = now.AddMonths(-11 + i);
                    var rev = rawRevenue.FirstOrDefault(
                        x => x.Year == d.Year && x.Month == d.Month);
                    var ord = rawOrders.FirstOrDefault(
                        x => x.Year == d.Year && x.Month == d.Month);

                    return new MonthlySalesDto
                    {
                        Month = d.ToString("MMM"),
                        Revenue = rev?.Revenue ?? 0m,
                        Orders = ord?.Orders ?? 0
                    };
                })
                .ToList();
        }
    }
}