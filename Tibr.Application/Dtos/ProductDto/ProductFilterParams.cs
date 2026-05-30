using System;
using System.Collections.Generic;
using System.Text;
using Tibr.Application.Dtos.Common;
using Tibr.Domain.Enums;

namespace Tibr.Application.Dtos.ProductDto
{
    public class ProductFilterParams : PaginationParams
    {
        public string? SearchKeyword { get; set; }
        public long? CategoryId { get; set; }
        public string? CategoryName { get; set; }
        public string? MetalType { get; set; }
        public decimal? MinWeight { get; set; }
        public decimal? MaxWeight { get; set; }
        public decimal? MinPurity { get; set; }
        public decimal? MaxPurity { get; set; }
        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }
        public ProductStatus? Status { get; set; }

        /// Sorting option: "newest", "price_asc", "price_desc", "popularity", "weight_asc", "weight_desc", "purity_asc", "purity_desc"
        public string SortBy { get; set; } = "newest";

        public bool IncludeOutOfStock { get; set; } = false;
    }
}
            