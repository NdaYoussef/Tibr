using Tibr.Domain.Common.Classes;
using Tibr.Domain.Enums;

namespace Tibr.Domain.Entities
{
    public class AssetPrice : BaseEntity<long>
    {
        public AssetType AssetType { get; set; }

        public decimal BuyPrice { get; set; }

        public decimal SellPrice { get; set; }

        public string Source { get; set; } = string.Empty;
    }
}
