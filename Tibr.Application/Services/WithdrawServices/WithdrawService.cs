using Tibr.Application.Dtos;
using Tibr.Domain.Entities;
using Tibr.Domain.IRepositories;
using Tibr.Domain.ResultPattern;

namespace Tibr.Application.Services.WithdrawServices;
public class WithdrawService : IWithdrawService
{
    private readonly IGenericRepository<Withdraw, long> _withdrawRepo;

    public WithdrawService(IGenericRepository<Withdraw, long> withdrawRepo)
    {
        _withdrawRepo = withdrawRepo;
    }

    public async Task<Result> CreateAsync(CreateWithdrawDto dto, long userId)
    {
        if (dto.Amount < 100 || dto.Amount > 50000)
            return Result.Failure("Amount must be between 100 and 50,000.");

        if (string.IsNullOrWhiteSpace(dto.Name))
            return Result.Failure("Recipient name is required.");

        if (string.IsNullOrWhiteSpace(dto.Number))
            return Result.Failure("Account/phone number is required.");

        var withdraw = new Withdraw
        {
            Amount = dto.Amount,
            Type = dto.Type,
            Name = dto.Name,
            Number = dto.Number,
            UserId = userId,
            CreatedAt = DateTime.UtcNow,
        };

        await _withdrawRepo.AddAsync(withdraw);
        await _withdrawRepo.SaveChangesAsync();

        return Result.Success();
    }
}
