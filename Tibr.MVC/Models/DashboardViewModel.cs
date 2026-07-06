namespace Tibr.MVC.Models
{
    public class DashboardViewModel
    {
        public decimal TotalRevenue { get; set; }
        public int TotalOrders { get; set; }
        public int TotalCustomers { get; set; }
        public int TotalProducts { get; set; }
        public int ActiveProducts { get; set; }
        public int LowStockProducts { get; set; }
        public int OutOfStockProducts { get; set; }

        public double RevenueChangePercent { get; set; }
        public double OrdersChangePercent { get; set; }
        public double CustomersChangePercent { get; set; }

        public int OrdersDelivered { get; set; }
        public int OrdersPending { get; set; }
        public int OrdersCancelled { get; set; }
        public int OrdersProcessing { get; set; }

        public List<RecentOrderRow> RecentOrders { get; set; } = [];
        public List<BestSellerRow> BestSellers { get; set; } = [];
        public List<MonthlySalesPoint> MonthlySales { get; set; } = [];
        public List<DailySalesPoint> DailySales { get; set; } = [];
        public List<CustomerGrowthPoint> CustomerGrowth { get; set; } = [];
    }

    public class RecentOrderRow
    {
        public long Id { get; set; }
        public string OrderNumber { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public string PaymentStatus { get; set; } = string.Empty;
        public string OrderStatus { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public int ItemCount { get; set; }
    }

    public class BestSellerRow
    {
        public long ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string MetalType { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
        public decimal SellPrice { get; set; }
        public int TotalSold { get; set; }
        public decimal TotalRevenue { get; set; }
    }

    public class MonthlySalesPoint
    {
        public string Month { get; set; } = string.Empty;
        public decimal Revenue { get; set; }
        public int Orders { get; set; }
    }

    public class DailySalesPoint
    {
        public string Day { get; set; } = string.Empty;
        public decimal Revenue { get; set; }
    }

    public class CustomerGrowthPoint
    {
        public string Month { get; set; } = string.Empty;
        public int NewCustomers { get; set; }
    }

    // ═══════════════════════════════════════════════════════════════
    // REPORTS PAGE — fed by IAnalyticsService DTOs via Mapster
    // ═══════════════════════════════════════════════════════════════

    public class ReportsViewModel
    {
        public string ReportType { get; set; } = "sales";
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }

        public ReportsSummaryViewModel Summary { get; set; } = new();

        public List<SalesReportRow> SalesRows { get; set; } = [];
        public List<RevenueReportRow> RevenueRows { get; set; } = [];
        public List<ProductPerformanceRow> ProductRows { get; set; } = [];
        public List<InventoryRow> InventoryRows { get; set; } = [];
        public List<CustomerReportRow> CustomerRows { get; set; } = [];

        // Header chart
        public List<MonthlySalesPoint> MonthlySales { get; set; } = [];
    }

    public class ReportsSummaryViewModel
    {
        public decimal TotalRevenue { get; set; }
        public int TotalOrders { get; set; }
        public int TotalCustomers { get; set; }
        public int TotalProducts { get; set; }
    }

    public class SalesReportRow
    {
        public string OrderNumber { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public decimal Amount { get; set; }
        public string OrderStatus { get; set; } = string.Empty;
        public string PaymentStatus { get; set; } = string.Empty;
    }

    public class RevenueReportRow
    {
        public string ProductName { get; set; } = string.Empty;
        public string MetalType { get; set; } = string.Empty;
        public int UnitsSold { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal TotalCost { get; set; }
        public decimal NetMargin => TotalRevenue - TotalCost;
        public decimal MarginPercent => TotalCost == 0 ? 0
            : Math.Round(NetMargin / TotalCost * 100, 1);
    }

    public class ProductPerformanceRow
    {
        public string ProductName { get; set; } = string.Empty;
        public string MetalType { get; set; } = string.Empty;
        public int TotalSold { get; set; }
        public decimal Revenue { get; set; }
        public long Stock { get; set; }
        public string Status { get; set; } = string.Empty;
    }

    public class InventoryRow
    {
        public string ProductName { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string MetalType { get; set; } = string.Empty;
        public long Stock { get; set; }
        public string StockStatus { get; set; } = string.Empty;
        public decimal BuyPrice { get; set; }
        public decimal SellPrice { get; set; }
    }

    public class CustomerReportRow
    {
        public string CustomerName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public int TotalOrders { get; set; }
        public decimal TotalSpent { get; set; }
        public DateTime JoinedAt { get; set; }
        public string KycStatus { get; set; } = string.Empty;
    }
}