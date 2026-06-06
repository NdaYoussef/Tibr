
using Mapster;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Tibr.Application.Constants;
using Tibr.Application.Dtos.DashboardDtos;
using Tibr.Domain.Entities;
using Tibr.Domain.Enums;
using Tibr.Domain.IRepositories;

namespace Tibr.Application.Services.AdminServices
{
    public class AdminService : IAdminService
    {
       
        private readonly IGenericRepository<Order, long> _orderRepo;
        private readonly IGenericRepository<OrderItem, long> _orderItemRepo;
        private readonly IGenericRepository<User, long> _userRepo;
        private readonly IProductRepository _productRepo;  

        private readonly IMemoryCache _cache;
        private readonly ILogger<AdminService> _logger;

        private static readonly TimeSpan _slidingExpiry = TimeSpan.FromMinutes(5);
        private static readonly TimeSpan _absoluteExpiry = TimeSpan.FromMinutes(10);

        public TimeSpan DashboardCacheDuration => _absoluteExpiry;

        public AdminService(
            IGenericRepository<Order, long> orderRepo,
            IGenericRepository<OrderItem, long> orderItemRepo,
            IGenericRepository<User, long> userRepo,
            IProductRepository productRepo,
            IMemoryCache cache,
            ILogger<AdminService> logger)
        {
            _orderRepo = orderRepo;
            _orderItemRepo = orderItemRepo;
            _userRepo = userRepo;
            _productRepo = productRepo;
            _cache = cache;
            _logger = logger;
        }

        public async Task<DashboardDto> GetDashboardDataAsync()
        {
            _logger.LogInformation("Building dashboard data");

            var stats = await GetOrCreateAsync(DashboardCacheKeys.Stats, BuildStatsAsync);
            var charts = await GetOrCreateAsync(DashboardCacheKeys.Charts, BuildChartsAsync);
            var bestSellers = await GetOrCreateAsync(DashboardCacheKeys.BestSellers, BuildBestSellersAsync);
            var recentOrders = await GetOrCreateAsync(DashboardCacheKeys.RecentOrders, BuildRecentOrdersAsync);


            return new DashboardDto
            {
                TotalRevenue = stats.TotalRevenue,
                TotalOrders = stats.TotalOrders,
                TotalCustomers = stats.TotalCustomers,
                TotalProducts = stats.TotalProducts,
                ActiveProducts = stats.ActiveProducts,
                LowStockProducts = stats.LowStockProducts,
                OutOfStockProducts = stats.OutOfStockProducts,
                RevenueChangePercent = stats.RevenueChangePercent,
                OrdersChangePercent = stats.OrdersChangePercent,
                OrdersDelivered = stats.OrdersDelivered,
                OrdersPending = stats.OrdersPending,
                OrdersCancelled = stats.OrdersCancelled,
                OrdersProcessing = stats.OrdersProcessing,
                MonthlySales = charts.MonthlySales,
                DailySales = charts.DailySales,
                CustomerGrowth = charts.CustomerGrowth,
                BestSellers = bestSellers,
                RecentOrders = recentOrders
            };
        }

        // ─────────────────────────────────────────────────────────────
        // PUBLIC — InvalidateDashboardCache
        // ─────────────────────────────────────────────────────────────
        public void InvalidateDashboardCache()
        {
            _cache.Remove(DashboardCacheKeys.Stats);
            _cache.Remove(DashboardCacheKeys.Charts);
            _cache.Remove(DashboardCacheKeys.BestSellers);
            _cache.Remove(DashboardCacheKeys.RecentOrders);
            _logger.LogInformation("Dashboard cache invalidated");
        }

        private async Task<StatsIntermediate> BuildStatsAsync()
        {
            _logger.LogDebug("Fetching dashboard stats from DB");

            var now = DateTime.UtcNow;
            var thisMonthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
            var lastMonthStart = thisMonthStart.AddMonths(-1);
            var nextMonthStart = thisMonthStart.AddMonths(1);

          
            var ordersQuery = _orderRepo.GetAll(o => !o.IsDeleted);

            var totalRevenue = await ordersQuery
                .Where(o => o.PaymentStatus == PaymentStatusConstants.Paid)
                .SumAsync(o => (decimal?)o.TotalAmount) ?? 0m;

            var totalOrders = await ordersQuery.CountAsync();

            var statusCounts = await ordersQuery
                .GroupBy(o => o.OrderStatus)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToListAsync();

            int StatusCount(string status) =>
                statusCounts.FirstOrDefault(s => s.Status == status)?.Count ?? 0;

            var totalCustomers = await _userRepo
                .GetAll(u => !u.IsDeleted)
                .CountAsync();

            var productStats = await _productRepo.GetAll()
                .GroupBy(p => 1)
                .Select(g => new
                {
                    Total = g.Count(),
                    Active = g.Count(p => p.Status == ProductStatus.Active),
                    LowStock = g.Count(p => p.Stock > 0 && p.Stock <= StockThresholds.Low),
                    OutOfStock = g.Count(p => p.Stock == 0)
                })
                .FirstOrDefaultAsync();

            var thisMonthRevenue = await ordersQuery
                .Where(o => o.PaymentStatus == PaymentStatusConstants.Paid
                         && o.CreatedAt >= thisMonthStart
                         && o.CreatedAt < nextMonthStart)
                .SumAsync(o => (decimal?)o.TotalAmount) ?? 0m;

            var lastMonthRevenue = await ordersQuery
                .Where(o => o.PaymentStatus == PaymentStatusConstants.Paid
                         && o.CreatedAt >= lastMonthStart
                         && o.CreatedAt < thisMonthStart)
                .SumAsync(o => (decimal?)o.TotalAmount) ?? 0m;

            var thisMonthOrders = await ordersQuery
                .CountAsync(o => o.CreatedAt >= thisMonthStart
                              && o.CreatedAt < nextMonthStart);

            var lastMonthOrders = await ordersQuery
                .CountAsync(o => o.CreatedAt >= lastMonthStart
                              && o.CreatedAt < thisMonthStart);

            return new StatsIntermediate
            {
                TotalRevenue = totalRevenue,
                TotalOrders = totalOrders,
                TotalCustomers = totalCustomers,
                TotalProducts = productStats?.Total ?? 0,
                ActiveProducts = productStats?.Active ?? 0,
                LowStockProducts = productStats?.LowStock ?? 0,
                OutOfStockProducts = productStats?.OutOfStock ?? 0,
                OrdersDelivered = StatusCount(OrderStatusConstants.Delivered),
                OrdersPending = StatusCount(OrderStatusConstants.Pending),
                OrdersCancelled = StatusCount(OrderStatusConstants.Cancelled),
                OrdersProcessing = StatusCount(OrderStatusConstants.Processing),
                RevenueChangePercent = lastMonthRevenue == 0 ? 0
                    : Math.Round((double)((thisMonthRevenue - lastMonthRevenue)
                                         / lastMonthRevenue * 100), 1),
                OrdersChangePercent = lastMonthOrders == 0 ? 0
                    : Math.Round((double)(thisMonthOrders - lastMonthOrders)
                                         / lastMonthOrders * 100, 1)
            };
        }

        private async Task<ChartsIntermediate> BuildChartsAsync()
        {
            _logger.LogDebug("Fetching dashboard chart data from DB");

            var now = DateTime.UtcNow;
            var twelveMonthsAgo = now.AddMonths(-11);
            var thirtyDaysAgo = now.AddDays(-29).Date;

            var ordersQuery = _orderRepo.GetAll(o => !o.IsDeleted);

            var rawMonthlyRevenue = await ordersQuery
                .Where(o => o.PaymentStatus == PaymentStatusConstants.Paid
                         && o.CreatedAt >= twelveMonthsAgo)
                .GroupBy(o => new { o.CreatedAt.Year, o.CreatedAt.Month })
                .Select(g => new
                {
                    g.Key.Year,
                    g.Key.Month,
                    Revenue = g.Sum(o => o.TotalAmount)
                })
                .ToListAsync();

            var rawMonthlyOrders = await ordersQuery
                .Where(o => o.CreatedAt >= twelveMonthsAgo)
                .GroupBy(o => new { o.CreatedAt.Year, o.CreatedAt.Month })
                .Select(g => new { g.Key.Year, g.Key.Month, Orders = g.Count() })
                .ToListAsync();

            var monthlySales = Enumerable.Range(0, 12)
                .Select(i =>
                {
                    var d = now.AddMonths(-11 + i);
                    var rev = rawMonthlyRevenue
                        .FirstOrDefault(m => m.Year == d.Year && m.Month == d.Month);
                    var ord = rawMonthlyOrders
                        .FirstOrDefault(m => m.Year == d.Year && m.Month == d.Month);
                    return new MonthlySalesPointDto
                    {
                        Month = d.ToString("MMM"),
                        Revenue = rev?.Revenue ?? 0m,
                        Orders = ord?.Orders ?? 0
                    };
                })
                .ToList();

            var rawDaily = await ordersQuery
                .Where(o => o.PaymentStatus == PaymentStatusConstants.Paid
                         && o.CreatedAt >= thirtyDaysAgo)
                .GroupBy(o => o.CreatedAt.Date)
                .Select(g => new { Date = g.Key, Revenue = g.Sum(o => o.TotalAmount) })
                .ToListAsync();

            var dailySales = Enumerable.Range(0, 30)
                .Select(i =>
                {
                    var d = now.AddDays(-29 + i).Date;
                    var rev = rawDaily.FirstOrDefault(x => x.Date == d);
                    return new DailySalesPointDto
                    {
                        Day = d.ToString("dd"),
                        Revenue = rev?.Revenue ?? 0m
                    };
                })
                .ToList();

            var rawCust = await _userRepo
                .GetAll(u => !u.IsDeleted && u.CreatedAt >= twelveMonthsAgo)
                .GroupBy(u => new { u.CreatedAt.Year, u.CreatedAt.Month })
                .Select(g => new { g.Key.Year, g.Key.Month, Count = g.Count() })
                .ToListAsync();

            var customerGrowth = Enumerable.Range(0, 12)
                .Select(i =>
                {
                    var d = now.AddMonths(-11 + i);
                    var c = rawCust
                        .FirstOrDefault(x => x.Year == d.Year && x.Month == d.Month);
                    return new CustomerGrowthPointDto
                    {
                        Month = d.ToString("MMM"),
                        NewCustomers = c?.Count ?? 0
                    };
                })
                .ToList();

            return new ChartsIntermediate
            {
                MonthlySales = monthlySales,
                DailySales = dailySales,
                CustomerGrowth = customerGrowth
            };
        }

        private async Task<List<BestSellerDto>> BuildBestSellersAsync()
        {
            _logger.LogDebug("Fetching best sellers from DB");

            // Filter: only items from non-deleted orders
            // Navigation to o.Order.IsDeleted is translated to a SQL JOIN
            return await _orderItemRepo
                .GetAll(oi => !oi.Order.IsDeleted)
                .GroupBy(oi => new
                {
                    oi.ProductId,
                    oi.Product.Name,
                    MetalType = oi.Product.MetalType.ToString(),
                    oi.Product.ImageUrl,
                    oi.Product.SellPrice
                })
                .Select(g => new BestSellerDto
                {
                    ProductId = g.Key.ProductId,
                    ProductName = g.Key.Name,
                    MetalType = g.Key.MetalType,
                    ImageUrl = g.Key.ImageUrl,
                    SellPrice = g.Key.SellPrice,
                    TotalSold = g.Sum(x => x.Quantity),
                    TotalRevenue = g.Sum(x => x.Price * x.Quantity)
                })
                .OrderByDescending(r => r.TotalSold)
                .Take(5)
                .ToListAsync();
        }

        private async Task<List<RecentOrderDto>> BuildRecentOrdersAsync()
        {
            _logger.LogDebug("Fetching recent orders from DB");

            return await _orderRepo
                .GetAll(o => !o.IsDeleted)
                .OrderByDescending(o => o.CreatedAt)
                .Take(10)
                .Select(o => new RecentOrderDto
                {
                    Id = o.Id,
                    OrderNumber = o.OrderNumber,
                    CustomerName = o.User != null
                        ? (o.User.FirstName + " " + o.User.LastName).Trim()
                        : "—",
                    TotalAmount = o.TotalAmount,
                    PaymentStatus = o.PaymentStatus,
                    OrderStatus = o.OrderStatus,
                    CreatedAt = o.CreatedAt,
                    ItemCount = o.OrderItems.Count()   // → SQL COUNT subquery
                })
                .ToListAsync();
        }

   
        private Task<T> GetOrCreateAsync<T>(string key, Func<Task<T>> factory)
        {
            return _cache.GetOrCreateAsync(key, async entry =>
            {
                entry.SlidingExpiration = _slidingExpiry;
                entry.AbsoluteExpiration = DateTimeOffset.UtcNow.Add(_absoluteExpiry);
                return await factory();
            })!;
        }
        private sealed class StatsIntermediate
        {
            public decimal TotalRevenue { get; init; }
            public int TotalOrders { get; init; }
            public int TotalCustomers { get; init; }
            public int TotalProducts { get; init; }
            public int ActiveProducts { get; init; }
            public int LowStockProducts { get; init; }
            public int OutOfStockProducts { get; init; }
            public int OrdersDelivered { get; init; }
            public int OrdersPending { get; init; }
            public int OrdersCancelled { get; init; }
            public int OrdersProcessing { get; init; }
            public double RevenueChangePercent { get; init; }
            public double OrdersChangePercent { get; init; }
        }

        private sealed class ChartsIntermediate
        {
            public List<MonthlySalesPointDto> MonthlySales { get; init; } = [];
            public List<DailySalesPointDto> DailySales { get; init; } = [];
            public List<CustomerGrowthPointDto> CustomerGrowth { get; init; } = [];
        }
    }
}