using System;
using System.Collections.Generic;
using System.Text;

namespace Tibr.Application.Dtos.DashboardDtos
{
    public class DashboardDto
    {
        //  KPI stats 
        public decimal TotalRevenue { get; init; }
        public int TotalOrders { get; init; }
        public int TotalCustomers { get; init; }
        public int TotalProducts { get; init; }
        public int ActiveProducts { get; init; }
        public int LowStockProducts { get; init; }
        public int OutOfStockProducts { get; init; }

        //  Month-over-month % changes 
        public double RevenueChangePercent { get; init; }
        public double OrdersChangePercent { get; init; }
        public double CustomersChangePercent { get; init; }

        //  Order status breakdown 
        public int OrdersDelivered { get; init; }
        public int OrdersPending { get; init; }
        public int OrdersCancelled { get; init; }
        public int OrdersProcessing { get; init; }

        //  Tables 
        public List<RecentOrderDto> RecentOrders { get; init; } = [];
        public List<BestSellerDto> BestSellers { get; init; } = [];

        //  Chart datasets 
        public List<MonthlySalesPointDto> MonthlySales { get; init; } = [];
        public List<DailySalesPointDto> DailySales { get; init; } = [];
        public List<CustomerGrowthPointDto> CustomerGrowth { get; init; } = [];
    }

    public class RecentOrderDto
    {
        public long Id { get; init; }
        public string OrderNumber { get; init; } = string.Empty;
        public string CustomerName { get; init; } = string.Empty;
        public decimal TotalAmount { get; init; }
        public string PaymentStatus { get; init; } = string.Empty;
        public string OrderStatus { get; init; } = string.Empty;
        public DateTime CreatedAt { get; init; }
        public int ItemCount { get; init; }
    }

    public class BestSellerDto
    {
        public long ProductId { get; init; }
        public string ProductName { get; init; } = string.Empty;
        public string MetalType { get; init; } = string.Empty;
        public string? ImageUrl { get; init; }
        public decimal SellPrice { get; init; }
        public int TotalSold { get; init; }
        public decimal TotalRevenue { get; init; }
    }

    public class MonthlySalesPointDto
    {
        public string Month { get; init; } = string.Empty;
        public decimal Revenue { get; init; }
        public int Orders { get; init; }
    }

    public class DailySalesPointDto
    {
        public string Day { get; init; } = string.Empty;
        public decimal Revenue { get; init; }
    }

    public class CustomerGrowthPointDto
    {
        public string Month { get; init; } = string.Empty;
        public int NewCustomers { get; init; }
    }
}