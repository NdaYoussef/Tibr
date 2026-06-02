using Tibr.Domain.Common.Classes;
using Tibr.Domain.Enums;

namespace Tibr.Domain.Entities
{
    public class Wallet : BaseEntity<long>
    {
        public long UserId { get; set; }

        public WalletType WalletType { get; set; }

        public decimal Balance { get; set; }

        public decimal ReservedBalance { get; set; }

        public ICollection<WalletTransaction> Transactions { get; set; } = [];
    }
}
