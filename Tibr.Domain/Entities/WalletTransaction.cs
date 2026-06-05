using Tibr.Domain.Common.Classes;
using Tibr.Domain.Enums;

namespace Tibr.Domain.Entities
{
    public class WalletTransaction : BaseEntity<long>
    {
        public long WalletId { get; set; }

        public Wallet Wallet { get; set; } = default!;

        public WalletTransactionType Type { get; set; }

        public decimal Amount { get; set; }

        public ReferenceType ReferenceType { get; set; }

        public long ReferenceId { get; set; }
    }
}
