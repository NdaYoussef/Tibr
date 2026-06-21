using Microsoft.EntityFrameworkCore;
using Tibr.Application.Dtos;
using Tibr.Domain.Entities;
using Tibr.Domain.Enums;
using Tibr.Domain.IRepositories;
using Tibr.Domain.ResultPattern;

namespace Tibr.Application.Services.WithdrawServices;
public class WithdrawService : IWithdrawService
{
    private readonly IGenericRepository<Withdraw, long> _withdrawRepo;
    private readonly DbContext _context;

    public WithdrawService(IGenericRepository<Withdraw, long> withdrawRepo, DbContext context   )
    {
        _withdrawRepo = withdrawRepo;
        _context = context;
    }

    public async Task<Result> CreateAsync(CreateWithdrawDto dto, long userId)
    {
        if (dto.Amount < 100 || dto.Amount > 50000)
            return Result.Failure("Amount must be between 100 and 50,000.");

        if (string.IsNullOrWhiteSpace(dto.Name))
            return Result.Failure("Recipient name is required.");

        if (string.IsNullOrWhiteSpace(dto.Number))
            return Result.Failure("Account/phone number is required.");

        var user = await _context.Set<Wallet>()
            .FirstOrDefaultAsync(x => x.Id == userId && x.WalletType == WalletType.Cash);

        if (user == null)
            return Result.Failure("User not found.");

        if (user.Balance < dto.Amount)
            return Result.Failure("Insufficient balance.");

        user.ReservedBalance += dto.Amount;

        var withdraw = new Withdraw
        {
            Amount = dto.Amount,
            Type = dto.Type,
            Name = dto.Name,
            Number = dto.Number,
            UserId = userId,
            Status = (int)WithdrawStatus.Pending, 
            CreatedAt = DateTime.UtcNow
        };

        await _withdrawRepo.AddAsync(withdraw);

        _context.Set<Wallet>().Update(user);

        await _withdrawRepo.SaveChangesAsync();

        return Result.Success();
    }
}
