
using System.Transactions;
using Tibr.Domain.Common.Classes;
using Tibr.Domain.Enums;

namespace Tibr.Domain.Entities
{
    public class Transaction : BaseEntity<long>
    {
        public Guid UserId { get; set; }

        public Guid TradeId { get; set; }

        public Trade Trade { get; set; } = default!;

        public TransactionType TransactionType { get; set; }

        public decimal Amount { get; set; }

        public TransactionStatus Status { get; set; }
    }
}
