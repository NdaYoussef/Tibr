using Microsoft.Extensions.Logging;
using Tibr.Application.Dtos;
using Tibr.Domain.Entities;
using Tibr.Domain.Enums;
using Tibr.Domain.IRepositories;
using Tibr.Domain.ResultPattern;
using Tibr.Application.Services.PaymentServices;
using Tibr.Application.Services.WalletServices;

namespace Tibr.Application.Services.DepositServices
{
    public class DepositService : IDepositService
    {
        private readonly IGenericRepository<Deposit, long> _depositRepo;
        private readonly IGenericRepository<Wallet, long> _walletRepo;
        private readonly IPaymobService _paymobService;
        private readonly IWalletService _walletService;
        private readonly ILogger<DepositService> _logger;

        public DepositService(
            IGenericRepository<Deposit, long> depositRepo,
            IGenericRepository<Wallet, long> walletRepo,
            IPaymobService paymobService,
            IWalletService walletService,
            ILogger<DepositService> logger)
        {
            _depositRepo = depositRepo;
            _walletRepo = walletRepo;
            _paymobService = paymobService;
            _walletService = walletService;
            _logger = logger;
        }

        public async Task<Result<string>> InitiateAsync(long userId, InitiateDepositDto dto)
        {
            if (dto.Amount <= 0)
                return Result<string>.Failure("Amount must be greater than zero.");

            var transactionRef = $"deposit-{userId}-{Guid.NewGuid():N}";

            var deposit = new Deposit
            {
                UserId = userId,
                Amount = dto.Amount,
                Status = DepositStatus.Pending,
                PaymentMethod = dto.PaymentMethod,
                TransactionRef = transactionRef
            };

            await _depositRepo.AddAsync(deposit);
            await _depositRepo.SaveChangesAsync();

            var amountCents = (int)(dto.Amount * 100);

            try
            {
                var checkoutUrl = await _paymobService.CreateDepositIntentionAsync(
                    transactionRef, amountCents, "EGP",
                    "Deposit", "User", "", "0000000000");

                return Result<string>.Success(checkoutUrl);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create Paymob intention for deposit {TransactionRef}", transactionRef);

                deposit.Status = DepositStatus.Failed;
                await _depositRepo.UpdateAsync(deposit);
                await _depositRepo.SaveChangesAsync();

                return Result<string>.Failure("Failed to initiate payment. Please try again.");
            }
        }

        public async Task<Result> HandleCallbackAsync(string transactionRef, bool success)
        {
            var deposit = _depositRepo.GetAll(d => d.TransactionRef == transactionRef).FirstOrDefault();
            if (deposit is null)
                return Result.Failure($"Deposit with reference {transactionRef} not found.");

            if (deposit.Status == DepositStatus.Completed)
            {
                _logger.LogInformation("Deposit {TransactionRef} already completed — skipping.", transactionRef);
                return Result.Success();
            }

            if (success)
            {
                deposit.Status = DepositStatus.Completed;
                await _depositRepo.UpdateAsync(deposit);

                var cashWallet = _walletRepo.GetAll(w => w.UserId == deposit.UserId && w.WalletType == WalletType.Cash).FirstOrDefault();
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
                "Deposit {TransactionRef} processed: Success={Success}, Amount={Amount}",
                transactionRef, success, deposit.Amount);

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
    }
}
