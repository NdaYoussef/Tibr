using Tibr.Domain.Common.Classes;
using Tibr.Domain.Enums;

namespace Tibr.Domain.Entities
{
    public class Trade : BaseEntity<long>
    {
        public Guid OrderId { get; set; }

        public OrdersInvestment Order { get; set; } = default!;

        public Guid UserId { get; set; }

        public AlertType AssetType { get; set; }

        public TradeSide Side { get; set; }

        public decimal Quantity { get; set; }

        public decimal ExecutedPrice { get; set; }

        public decimal TotalAmount { get; set; }

        public DateTime ExecutedAt { get; set; }
    }
}
