using System;
using System.Collections.Generic;
using System.Text;

namespace Tibr.Application.Constants
{
    
    public static class OrderStatusConstants
    {
        public const string Pending = "Pending";
        public const string Processing = "Processing";
        public const string Delivered = "Delivered";
        public const string Cancelled = "Cancelled";
    }

    public static class PaymentStatusConstants
    {
        public const string Paid = "Paid";
        public const string Unpaid = "Unpaid";
        public const string Pending = "Pending";
    }

    public static class KycStatusConstants
    {
        public const string Approved = "Approved";
        public const string Pending = "Pending";
        public const string Rejected = "Rejected";
    }

    public static class StockThresholds
    {
        public const int Low = 5;
    }

    public static class DashboardCacheKeys
    {
        public const string Stats = "admin:dashboard:stats";
        public const string Charts = "admin:dashboard:charts";
        public const string BestSellers = "admin:dashboard:bestsellers";
        public const string RecentOrders = "admin:dashboard:recentorders";
    }
}
