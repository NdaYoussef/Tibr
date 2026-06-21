using Tibr.Application.Dtos;
using Tibr.Domain.Enums;
using Tibr.Domain.ResultPattern;

namespace Tibr.Application.Services.WalletServices
{
    public interface IWalletService
    {
        Task<Result<List<WalletBalanceDto>>> GetBalancesAsync(long userId);
        Task<Result<decimal>> GetAvailableBalanceAsync(long userId, WalletType walletType);
        Task<Result> CreditAsync(long walletId, decimal amount, ReferenceType referenceType, long referenceId);
        Task<Result> DebitAsync(long walletId, decimal amount, ReferenceType referenceType, long referenceId);
        Task<Result> ReserveAsync(long walletId, long investmentOrderId, decimal amount);
        Task<Result> ReleaseReservationAsync(long reservationId);
        Task<Result> ConsumeReservationAsync(long reservationId);
        Task<Result<List<WalletTransactionDto>>> GetTransactionHistoryAsync(long userId);
    }
}
