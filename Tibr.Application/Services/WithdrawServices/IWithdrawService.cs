using Tibr.Application.Dtos;
using Tibr.Domain.ResultPattern;

namespace Tibr.Application.Services.WithdrawServices;
public interface IWithdrawService
{
    Task<Result> CreateAsync(CreateWithdrawDto dto, long userId);
}
