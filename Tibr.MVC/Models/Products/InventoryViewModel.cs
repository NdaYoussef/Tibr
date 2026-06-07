namespace Tibr.MVC.Models.Products
{

    // INVENTORY MANAGEMENT PAGE

    public class InventoryViewModel
    {
        public IEnumerable<InventoryRowViewModel> Products { get; set; } = [];
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public int TotalCount { get; set; }
        public int TotalPages { get; set; }
        public bool HasPrevious { get; set; }
        public bool HasNext { get; set; }

        // Filter state
        public string? SearchKeyword { get; set; }
        public string? StockFilter { get; set; }  // "low", "out", "ok"
        public string? MetalTypeFilter { get; set; }

        // Summary cards
        public long TotalStockUnits { get; set; }
        public decimal TotalStockValue { get; set; }
        public int LowStockCount { get; set; }
        public int OutOfStockCount { get; set; }
    }

    public class InventoryRowViewModel
    {
        public long Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string MetalType { get; set; } = string.Empty;
        public decimal Weight { get; set; }
        public long Stock { get; set; }
        public decimal SellPrice { get; set; }
        public decimal BuyPrice { get; set; }
        public string? ImageUrl { get; set; }
        public string Status { get; set; } = string.Empty;

        public string StockStatusClass => Stock == 0 ? "out-stock"
                                        : Stock <= 5 ? "low-stock"
                                        : "in-stock";

        public string StockStatusLabel => Stock == 0 ? "Out of Stock"
                                        : Stock <= 5 ? "Low Stock"
                                        : "In Stock";

        public decimal StockValue => Stock * SellPrice;
    }

    // Form for quick stock update (inline in inventory table)
    public class UpdateStockViewModel
    {
        public long Id { get; set; }
        public long NewStock { get; set; }
    }
}
