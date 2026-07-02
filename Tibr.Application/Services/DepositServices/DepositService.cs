using Microsoft.Extensions.Logging;
using Tibr.Application.Dtos;
using Tibr.Application.Dtos.Payment;
using Tibr.Domain.Entities;
using Tibr.Domain.Enums;
using Tibr.Domain.IRepositories;
using Tibr.Domain.ResultPattern;
using Tibr.Application.Services.PaymentServices;
using Tibr.Application.Services.WalletServices;

namespace Tibr.Application.Services.DepositServices;

public class DepositService : IDepositService
{
    private readonly IGenericRepository<Deposit, long> _depositRepo;
    private readonly IGenericRepository<Wallet, long> _walletRepo;
    private readonly IGenericRepository<User, long> _userRepo;
    private readonly IPaymentGateway _paymentGateway;
    private readonly IWalletService _walletService;
    private readonly ILogger<DepositService> _logger;

    public DepositService(
        IGenericRepository<Deposit, long> depositRepo,
        IGenericRepository<Wallet, long> walletRepo,
        IGenericRepository<User, long> userRepo,
        IPaymentGateway paymentGateway,
        IWalletService walletService,
        ILogger<DepositService> logger)
    {
        _depositRepo = depositRepo;
        _walletRepo = walletRepo;
        _userRepo = userRepo;
        _paymentGateway = paymentGateway;
        _walletService = walletService;
        _logger = logger;
    }

    public async Task<Result<string>> InitiateAsync(long userId, InitiateDepositDto dto)
    {
        if (dto.Amount <= 0)
            return Result<string>.Failure("Amount must be greater than zero.");

        var deposit = new Deposit
        {
            UserId = userId,
            Amount = dto.Amount,
            Status = DepositStatus.Pending,
            PaymentMethod = dto.PaymentMethod,
            CreatedAt = DateTime.UtcNow,
        };

        await _depositRepo.AddAsync(deposit);
        await _depositRepo.SaveChangesAsync();

        var user = await _userRepo.GetByIdAsync(userId);
        var firstName = user?.FirstName ?? "Deposit";
        var lastName = user?.LastName ?? "User";
        var email = user?.Email ?? "";
        var phone = user?.Phone ?? "0000000000";

        var timestamp = new DateTimeOffset(deposit.CreatedAt).ToUnixTimeSeconds();
        var specialReference = $"deposit:{deposit.Id}:{timestamp}";

        deposit.TransactionRef = specialReference;
        await _depositRepo.UpdateAsync(deposit);
        await _depositRepo.SaveChangesAsync();

        var amountCents = (int)(dto.Amount * 100);

        try
        {
            var intentionRequest = new Dtos.Payment.PaymentIntentionRequest
            {
                AmountCents = amountCents,
                Currency = "EGP",
                SpecialReference = specialReference,
                FirstName = firstName,
                LastName = lastName,
                Email = email,
                Phone = phone,
            };

            var result = await _paymentGateway.CreateIntentionAsync(intentionRequest);

            if (!result.IsSuccess)
            {
                deposit.Status = DepositStatus.Failed;
                await _depositRepo.UpdateAsync(deposit);
                await _depositRepo.SaveChangesAsync();
                return Result<string>.Failure(result.ErrorMessage ?? "Payment initiation failed.");
            }

            return Result<string>.Success(result.CheckoutUrl);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create Paymob intention for deposit {DepositId}", deposit.Id);

            deposit.Status = DepositStatus.Failed;
            await _depositRepo.UpdateAsync(deposit);
            await _depositRepo.SaveChangesAsync();

            return Result<string>.Failure("Failed to initiate payment. Please try again.");
        }
    }

    public async Task<Result> HandleCallbackAsync(long depositId, bool success)
    {
        var deposit = await _depositRepo.GetByIdAsync(depositId);
        if (deposit is null)
            return Result.Failure($"Deposit with ID {depositId} not found.");

        if (deposit.Status == DepositStatus.Completed)
        {
            _logger.LogInformation("Deposit {DepositId} already completed — skipping.", depositId);
            return Result.Success();
        }

        if (success)
        {
            deposit.Status = DepositStatus.Completed;
            await _depositRepo.UpdateAsync(deposit);

            var cashWallet = _walletRepo
                .GetAll(w => w.UserId == deposit.UserId && w.WalletType == WalletType.Cash)
                .FirstOrDefault();

            if (cashWallet is null)
                return Result.Failure("Cash wallet not found.");

            var creditResult = await _walletService.CreditAsync(
                cashWallet.Id, deposit.Amount, ReferenceType.Deposit, deposit.Id);

            if (creditResult.IsFailure)
                return creditResult;
        }
        else
        {
            deposit.Status = DepositStatus.Failed;
            await _depositRepo.UpdateAsync(deposit);
            await _depositRepo.SaveChangesAsync();
        }

        _logger.LogInformation(
            "Deposit {DepositId} processed: Success={Success}, Amount={Amount}",
            depositId, success, deposit.Amount);

        return Result.Success();
    }

    public async Task<Result<List<DepositDto>>> GetUserDepositsAsync(long userId)
    {
        var deposits = _depositRepo.GetAll(d => d.UserId == userId).ToList();

        var dtos = deposits.Select(d => new DepositDto
        {
            Id = d.Id,
            Amount = d.Amount,
            Status = d.Status,
            PaymentMethod = d.PaymentMethod,
            CreatedAt = d.CreatedAt
        }).ToList();

        return Result<List<DepositDto>>.Success(dtos);
    }

    public async Task<Result<VerifyStatusResponse>> VerifyDepositAsync(long depositId)
    {
        var deposit = await _depositRepo.GetByIdAsync(depositId);
        if (deposit is null)
            return Result<VerifyStatusResponse>.Failure("Deposit not found.");

        if (deposit.Status == DepositStatus.Completed)
        {
            return Result<VerifyStatusResponse>.Success(new VerifyStatusResponse
            {
                EntityId = deposit.Id,
                EntityType = "deposit",
                Status = "Completed",
                IsCompleted = true,
                InquiredPaymob = false,
                Message = "Deposit is already completed.",
            });
        }

        if (deposit.Status != DepositStatus.Pending)
        {
            return Result<VerifyStatusResponse>.Success(new VerifyStatusResponse
            {
                EntityId = deposit.Id,
                EntityType = "deposit",
                Status = deposit.Status.ToString(),
                IsCompleted = false,
                InquiredPaymob = false,
                Message = $"Deposit is {deposit.Status}.",
            });
        }

        if (string.IsNullOrEmpty(deposit.TransactionRef))
            return Result<VerifyStatusResponse>.Failure("Deposit has no transaction reference to inquire.");

        var inquiry = await _paymentGateway.InquireByMerchantOrderAsync(deposit.TransactionRef);
        if (!inquiry.IsSuccess)
            return Result<VerifyStatusResponse>.Failure(inquiry.ErrorMessage!);

        if (inquiry.IsPaid)
        {
            deposit.Status = DepositStatus.Completed;
            await _depositRepo.UpdateAsync(deposit);

            var cashWallet = _walletRepo
                .GetAll(w => w.UserId == deposit.UserId && w.WalletType == WalletType.Cash)
                .FirstOrDefault();

            if (cashWallet is null)
                return Result<VerifyStatusResponse>.Failure("Cash wallet not found.");

            var creditResult = await _walletService.CreditAsync(
                cashWallet.Id, deposit.Amount, ReferenceType.Deposit, deposit.Id);

            if (creditResult.IsFailure)
                return Result<VerifyStatusResponse>.Failure(creditResult.ErrorMessage!);

            await _depositRepo.SaveChangesAsync();

            return Result<VerifyStatusResponse>.Success(new VerifyStatusResponse
            {
                EntityId = deposit.Id,
                EntityType = "deposit",
                Status = "Completed",
                IsCompleted = true,
                InquiredPaymob = true,
                Message = "Deposit confirmed via Paymob inquiry.",
            });
        }

        return Result<VerifyStatusResponse>.Success(new VerifyStatusResponse
        {
            EntityId = deposit.Id,
            EntityType = "deposit",
            Status = "Pending",
            IsCompleted = false,
            InquiredPaymob = true,
            Message = "Deposit is still pending on Paymob's side.",
        });
    }
}
