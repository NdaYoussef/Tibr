using Tibr.Domain.Enums;

namespace Tibr.Application.Dtos
{
    public class WalletBalanceDto
    {
        public WalletType WalletType { get; set; }
        public decimal Balance { get; set; }
        public decimal ReservedBalance { get; set; }
        public decimal AvailableBalance { get; set; }
    }

    public class WalletTransactionDto
    {
        public long Id { get; set; }
        public WalletTransactionType Type { get; set; }
        public decimal Amount { get; set; }
        public ReferenceType ReferenceType { get; set; }
        public long ReferenceId { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
