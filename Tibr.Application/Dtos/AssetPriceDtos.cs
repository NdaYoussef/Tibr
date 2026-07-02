using Tibr.Domain.Enums;

namespace Tibr.Application.Dtos
{
    public class AssetPriceDto
    {
        public AssetType AssetType { get; set; }
        public decimal BuyPrice { get; set; }
        public decimal SellPrice { get; set; }
        public string Source { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    public class PriceAnalyticsDto
    {
        public AssetType AssetType { get; set; }
        public decimal CurrentPrice { get; set; }
        public decimal AvgPriceLast30Days { get; set; }
        public decimal? SevenDayAvg { get; set; }
        public decimal MinPriceLast30Days { get; set; }
        public decimal MaxPriceLast30Days { get; set; }
        public int DaysOfData { get; set; }
        public bool IsBelowAverage { get; set; }
        public decimal? PercentBelowAverage { get; set; }
        public bool IsNearMonthlyLow { get; set; }
    }
}
