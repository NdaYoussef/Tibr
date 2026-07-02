using Tibr.Domain.Common.Classes;
using Tibr.Domain.Enums;

namespace Tibr.Domain.Entities
{
    public class PriceSnapshot : BaseEntity<long>
    {
        public AssetType AssetType { get; set; }
        public decimal Price { get; set; }
        public DateTime SnapshotDate { get; set; }
    }
}
