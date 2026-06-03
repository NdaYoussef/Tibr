using System;
using System.Collections.Generic;
using System.Text;

namespace Tibr.Application.Dtos.ProductDto
{
    public class ProductSummaryDto
    {
        public long Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string MetalType { get; set; } = string.Empty;
        public decimal Weight { get; set; }
        public decimal BuyPrice { get; set; }
        public decimal SellPrice { get; set; }
        public string Status { get; set; } = string.Empty;
        public long Stock { get; set; }
        public string ImageUrl { get; set; } = string.Empty;
        public int PopularityScore { get; set; }                  
        public DateTime CreatedAt { get; set; }
    }
}
