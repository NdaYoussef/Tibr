using Tibr.Application.Dtos;
using Tibr.Application.Dtos.Payment;
using Tibr.Domain.ResultPattern;

namespace Tibr.Application.Services.DepositServices
{
    public interface IDepositService
    {
        Task<Result<string>> InitiateAsync(long userId, InitiateDepositDto dto);
        Task<Result> HandleCallbackAsync(long depositId, bool success);
        Task<Result<List<DepositDto>>> GetUserDepositsAsync(long userId);
        Task<Result<VerifyStatusResponse>> VerifyDepositAsync(long depositId);
    }
}
