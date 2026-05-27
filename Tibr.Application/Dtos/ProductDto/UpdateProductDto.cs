using System;
using System.Collections.Generic;
using System.Text;
using Tibr.Domain.Enums;

namespace Tibr.Application.Dtos.ProductDto
{
    public class UpdateProductDto
    {
        public long CategoryId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string MetalType { get; set; } = string.Empty;
        public decimal Purity { get; set; }
        public decimal Weight { get; set; }
        public decimal BuyPrice { get; set; }
        public decimal SellPrice { get; set; }
        public ProductStatus Status { get; set; } 
        public decimal Stock { get; set; }
        public string? ImageUrl { get; set; }
    }
}
