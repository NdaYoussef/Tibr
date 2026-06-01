using Tibr.Domain.Common.Classes;
using Tibr.Domain.Enums;

namespace Tibr.Domain.Entities
{
    public class Reservation : BaseEntity<long>
    {
        public Guid UserId { get; set; }

        public Guid WalletId { get; set; }

        public Wallet Wallet { get; set; } = default!;

        public Guid OrderId { get; set; }

        public OrdersInvestment Order { get; set; } = default!;

        public decimal Amount { get; set; }

        public ReservationStatus Status { get; set; }
    }
}
