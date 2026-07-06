using System;
using System.Collections.Generic;
using System.Text;

namespace Tibr.Application.Dtos.DashboardDtos
{
    public class ReportsSummaryDto
    {
        public decimal TotalRevenue { get; init; }
        public int TotalOrders { get; init; }
        public int TotalCustomers { get; init; }
        public int TotalProducts { get; init; }
    }

    public class SalesReportDto
    {
        public string OrderNumber { get; init; } = string.Empty;
        public string CustomerName { get; init; } = string.Empty;
        public DateTime Date { get; init; }
        public decimal Amount { get; init; }
        public string OrderStatus { get; init; } = string.Empty;
        public string PaymentStatus { get; init; } = string.Empty;
    }

    public class RevenueReportDto
    {
        public string ProductName { get; init; } = string.Empty;
        public string MetalType { get; init; } = string.Empty;
        public int UnitsSold { get; init; }
        public decimal TotalRevenue { get; init; }   // sum(SellPrice * Qty) paid orders
        public decimal TotalCost { get; init; }      // sum(BuyPrice  * Qty) paid orders
        public decimal NetMargin => TotalRevenue - TotalCost;
        public decimal MarginPercent => TotalCost == 0 ? 0
            : Math.Round(NetMargin / TotalCost * 100, 1);
    }

    public class ProductPerformanceDto
    {
        public string ProductName { get; init; } = string.Empty;
        public string MetalType { get; init; } = string.Empty;
        public int TotalSold { get; init; }
        public decimal Revenue { get; init; }
        public long Stock { get; init; }
        public string Status { get; init; } = string.Empty;
    }

    public class InventoryDto
    {
        public string ProductName { get; init; } = string.Empty;
        public string Category { get; init; } = string.Empty;
        public string MetalType { get; init; } = string.Empty;
        public long Stock { get; init; }
        public string StockStatus { get; init; } = string.Empty;   // "OK" | "Low" | "Out"
        public decimal BuyPrice { get; init; }
        public decimal SellPrice { get; init; }
    }

    public class CustomerReportDto
    {
        public string CustomerName { get; init; } = string.Empty;
        public string Email { get; init; } = string.Empty;
        public int TotalOrders { get; init; }
        public decimal TotalSpent { get; init; }
        public DateTime JoinedAt { get; init; }
        public string KycStatus { get; init; } = string.Empty;
    }

    public class MonthlySalesDto
    {
        public string Month { get; init; } = string.Empty;
        public decimal Revenue { get; init; }
        public int Orders { get; init; }
    }
}