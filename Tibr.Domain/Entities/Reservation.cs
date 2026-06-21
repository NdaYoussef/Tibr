using Tibr.Domain.Common.Classes;
using Tibr.Domain.Enums;

namespace Tibr.Domain.Entities
{
    public class Reservation : BaseEntity<long>
    {
        public long UserId { get; set; }

        public long WalletId { get; set; }

        public Wallet Wallet { get; set; } = default!;

        public long OrderId { get; set; }

        public OrdersInvestment Order { get; set; } = default!;

        public decimal Amount { get; set; }

        public ReservationStatus Status { get; set; }
    }
}
