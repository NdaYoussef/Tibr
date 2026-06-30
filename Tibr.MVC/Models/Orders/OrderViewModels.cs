using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Tibr.MVC.Models.Orders
{
    public class OrderListItemViewModel
    {
        public long Id { get; set; }
        public string OrderNumber { get; set; } = string.Empty;
        public string UserFullName { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public string PaymentStatus { get; set; } = string.Empty;
        public string OrderStatus { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    public class OrderListViewModel
    {
        public List<OrderListItemViewModel> Orders { get; set; } = [];

        // Search & Filters
        public string? SearchQuery { get; set; }
        public string? StatusFilter { get; set; }
        public string? PaymentStatusFilter { get; set; }

        // Pagination
        public int CurrentPage { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public int TotalItems { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalItems / PageSize);
        public bool HasPreviousPage => CurrentPage > 1;
        public bool HasNextPage => CurrentPage < TotalPages;
    }

    public class OrderItemViewModel
    {
        public long Id { get; set; }
        public long ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal Price { get; set; }
        public decimal Total => Quantity * Price;
    }

    public class OrderTrackingMilestoneViewModel
    {
        public string Status { get; set; } = string.Empty;
        public DateTime UpdatedTime { get; set; }
        public string Description { get; set; } = string.Empty;
        public bool IsCompleted { get; set; }
    }

    public class OrderDetailsViewModel
    {
        public long Id { get; set; }
        public string OrderNumber { get; set; } = string.Empty;
        public long UserId { get; set; }
        public string UserFullName { get; set; } = string.Empty;
        public string UserEmail { get; set; } = string.Empty;
        public string UserPhone { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public string PaymentStatus { get; set; } = string.Empty;
        public string OrderStatus { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public string ShippingAddress { get; set; } = string.Empty;
        public string? CourierName { get; set; }

        public List<OrderItemViewModel> Items { get; set; } = [];
        public List<OrderTrackingMilestoneViewModel> TrackingTimeline { get; set; } = [];
    }

    public class InvoiceViewModel
    {
        public string InvoiceNumber { get; set; } = string.Empty;
        public DateTime InvoiceDate { get; set; }
        public DateTime DueDate { get; set; }

        public string CompanyName { get; set; } = "Tibr Enterprise Ltd";
        public string CompanyAddress { get; set; } = "123 Business Parkway, Suite 500, Tech City";
        public string CompanyEmail { get; set; } = "billing@tibr.com";
        public string CompanyPhone { get; set; } = "+1 (555) 123-4567";

        public OrderDetailsViewModel OrderDetails { get; set; } = null!;
    }
}