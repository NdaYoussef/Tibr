
using Tibr.Domain.Common.Classes;
using Tibr.Domain.Enums;

namespace Tibr.Domain.Entities
{
    public class Transaction : BaseEntity<long>
    {
        public long UserId { get; set; }

        public long TradeId { get; set; }

        public Trade Trade { get; set; } = default!;

        public TransactionType TransactionType { get; set; }

        public decimal Amount { get; set; }

        public TransactionStatusEnum Status { get; set; }
    }
}
