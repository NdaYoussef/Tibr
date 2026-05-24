using System;
using Tibr.Domain.Common.Classes;

namespace Tibr.Domain.Entities
{
    public class Payment : BaseEntity<long>
    {
        public long OrderId { get; set; }
        public long UserId { get; set; }
        public string PaymentMethod { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime PaidAt { get; set; }

        public virtual Order Order { get; set; } = null!;
        public virtual User User { get; set; } = null!;
    }
}
