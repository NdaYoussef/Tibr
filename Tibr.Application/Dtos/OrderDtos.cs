namespace Tibr.Application.Dtos
{
    public class OrderDto
    {
        public long Id { get; set; }
        public long UserId { get; set; }
        public string UserFullName { get; set; } = string.Empty;
        public string OrderNumber { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public string PaymentStatus { get; set; } = string.Empty;
        public string OrderStatus { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public List<OrderItemDto> Items { get; set; } = [];
    }

    public class CreateOrderDto
    {
        public long UserId { get; set; }
        public List<CreateOrderItemDto> Items { get; set; } = [];
    }

    public class UpdateOrderDto
    {
        public string? PaymentStatus { get; set; }
        public string? OrderStatus { get; set; }
    }

    public class OrderItemDto
    {
        public long Id { get; set; }
        public long ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal Price { get; set; }
    }

    public class CreateOrderItemDto
    {
        public long ProductId { get; set; }
        public int Quantity { get; set; }
    }
}
