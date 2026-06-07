using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;
using Tibr.Domain.Enums;

namespace Tibr.MVC.Models.Products
{
    public class ProductListViewModel
    {
        // Paginated product data
        public IEnumerable<ProductRowViewModel> Products { get; set; } = [];
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public int TotalCount { get; set; }
        public int TotalPages { get; set; }
        public bool HasPrevious { get; set; }
        public bool HasNext { get; set; }

        // Active filters (echoed back for form state)
        public string? SearchKeyword { get; set; }
        public string? MetalTypeFilter { get; set; }
        public string? StatusFilter { get; set; }
        public long? CategoryIdFilter { get; set; }
        public string SortBy { get; set; } = "newest";

        // Dropdowns
        public IEnumerable<SelectListItem> CategoryOptions { get; set; } = [];

        // Quick stats (shown in header cards)
        public int TotalActive { get; set; }
        public int TotalLowStock { get; set; }
        public int TotalOutOfStock { get; set; }
    }

    public class ProductRowViewModel
    {
        public long Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string MetalType { get; set; } = string.Empty;
        public decimal Purity { get; set; }
        public decimal Weight { get; set; }
        public decimal BuyPrice { get; set; }
        public decimal SellPrice { get; set; }
        public string Status { get; set; } = string.Empty;
        public long Stock { get; set; }
        public string? ImageUrl { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public int PopularityScore { get; set; }
        public DateTime CreatedAt { get; set; }

        // Computed helpers for the view
        public string StockStatusClass => Stock == 0 ? "out-stock"
                                        : Stock <= 5 ? "low-stock"
                                        : "in-stock";

        public string StockStatusLabel => Stock == 0 ? "Out of Stock"
                                        : Stock <= 5 ? "Low Stock"
                                        : "In Stock";

        public decimal Margin => SellPrice > 0 && BuyPrice > 0
            ? Math.Round((SellPrice - BuyPrice) / BuyPrice * 100, 1)
            : 0;
    }

}
