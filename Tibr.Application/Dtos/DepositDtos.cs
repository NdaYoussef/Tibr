using Tibr.Domain.Enums;

namespace Tibr.Application.Dtos
{
    public class InitiateDepositDto
    {
        public decimal Amount { get; set; }
        public PaymentMethod PaymentMethod { get; set; }
    }

    public class DepositDto
    {
        public long Id { get; set; }
        public decimal Amount { get; set; }
        public DepositStatus Status { get; set; }
        public PaymentMethod PaymentMethod { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
