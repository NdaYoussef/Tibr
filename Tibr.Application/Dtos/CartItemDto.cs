using System;
using System.Collections.Generic;
using System.Text;
using Tibr.Application.Dtos.ProductDto;

namespace Tibr.Application.Dtos
{
    public class CartItemDto
    {
        public long Id { get; set; }
        public long CartId { get; set; }
        public long ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalPrice => Quantity * UnitPrice;
        public ProductDetailsDto Product { get; set; } = null!;
    }
}
