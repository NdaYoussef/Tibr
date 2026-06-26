using Tibr.Application.Dtos;
using Tibr.Domain.ResultPattern;

namespace Tibr.Application.Services.WithdrawServices;
public interface IWithdrawService
{
    Task<Result> CreateAsync(CreateWithdrawDto dto, long userId);

    Task<Result<IEnumerable<WithdrawDto>>> GetAllAsync();
    Task<Result> UpdateStatusAsync(UpdateWithdrawStatusDto dto);
}
