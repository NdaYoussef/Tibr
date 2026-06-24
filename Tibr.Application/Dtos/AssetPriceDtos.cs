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
}
