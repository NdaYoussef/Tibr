using System;
using System.Collections.Generic;
using System.Text;

namespace Tibr.Application.Dtos.ProductDto
{
    public class CreateProductDto
    {
        public long CategoryId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string MetalType { get; set; } = string.Empty;
        public decimal Purity { get; set; }
        public decimal Weight { get; set; }
        public decimal BuyPrice { get; set; }
        public decimal SellPrice { get; set; }
        public string? ImageUrl { get; set; }
    }
}
