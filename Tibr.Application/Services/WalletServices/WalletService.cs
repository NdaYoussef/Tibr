using Tibr.Application.Dtos;
using Tibr.Domain.Entities;
using Tibr.Domain.Enums;
using Tibr.Domain.IRepositories;
using Tibr.Domain.ResultPattern;

namespace Tibr.Application.Services.WalletServices
{
    public class WalletService : IWalletService
    {
        private readonly IGenericRepository<Wallet, long> _walletRepo;
        private readonly IGenericRepository<Reservation, long> _reservationRepo;
        private readonly IGenericRepository<WalletTransaction, long> _walletTransactionRepo;

        public WalletService(
            IGenericRepository<Wallet, long> walletRepo,
            IGenericRepository<Reservation, long> reservationRepo,
            IGenericRepository<WalletTransaction, long> walletTransactionRepo)
        {
            _walletRepo = walletRepo;
            _reservationRepo = reservationRepo;
            _walletTransactionRepo = walletTransactionRepo;
        }

        public async Task<Result<List<WalletBalanceDto>>> GetBalancesAsync(long userId)
        {
            var wallets = _walletRepo.GetAll(w => w.UserId == userId).ToList();

            var dtos = wallets.Select(w => new WalletBalanceDto
            {
                WalletType = w.WalletType,
                Balance = w.Balance,
                ReservedBalance = w.ReservedBalance,
                AvailableBalance = w.Balance - w.ReservedBalance
            }).ToList();

            return Result<List<WalletBalanceDto>>.Success(dtos);
        }

        public async Task<Result<decimal>> GetAvailableBalanceAsync(long userId, WalletType walletType)
        {
            var wallet = _walletRepo.GetAll(w => w.UserId == userId && w.WalletType == walletType).FirstOrDefault();
            if (wallet is null)
                return Result<decimal>.Failure($"Wallet not found for user {userId} and type {walletType}.");

            return Result<decimal>.Success(wallet.Balance - wallet.ReservedBalance);
        }

        public async Task<Result> CreditAsync(long walletId, decimal amount, ReferenceType referenceType, long referenceId)
        {
            var wallet = await _walletRepo.GetByIdAsync(walletId);
            if (wallet is null)
                return Result.Failure($"Wallet with ID {walletId} not found.");

            wallet.Balance += amount;

            var transaction = new WalletTransaction
            {
                WalletId = walletId,
                Type = WalletTransactionType.Credit,
                Amount = amount,
                ReferenceType = referenceType,
                ReferenceId = referenceId,
                CreatedAt = DateTime.UtcNow,
            };

            await _walletRepo.UpdateAsync(wallet);
            await _walletTransactionRepo.AddAsync(transaction);
            await _walletRepo.SaveChangesAsync();

            return Result.Success();
        }

        public async Task<Result> DebitAsync(long walletId, decimal amount, ReferenceType referenceType, long referenceId)
        {
            var wallet = await _walletRepo.GetByIdAsync(walletId);
            if (wallet is null)
                return Result.Failure($"Wallet with ID {walletId} not found.");

            var available = wallet.Balance - wallet.ReservedBalance;
            if (available < amount)
                return Result.Failure($"Insufficient available balance. Available: {available}, Requested: {amount}.");

            wallet.Balance -= amount;

            var transaction = new WalletTransaction
            {
                WalletId = walletId,
                Type = WalletTransactionType.Debit,
                Amount = amount,
                ReferenceType = referenceType,
                ReferenceId = referenceId,
                CreatedAt = DateTime.UtcNow,
            };

            await _walletRepo.UpdateAsync(wallet);
            await _walletTransactionRepo.AddAsync(transaction);
            await _walletRepo.SaveChangesAsync();

            return Result.Success();
        }

        public async Task<Result> ReserveAsync(long walletId, long investmentOrderId, decimal amount)
        {
            var wallet = await _walletRepo.GetByIdAsync(walletId);
            if (wallet is null)
                return Result.Failure($"Wallet with ID {walletId} not found.");

            var available = wallet.Balance - wallet.ReservedBalance;
            if (available < amount)
                return Result.Failure($"Insufficient available balance. Available: {available}, Requested: {amount}.");

            wallet.ReservedBalance += amount;

            var reservation = new Reservation
            {
                WalletId = walletId,
                OrderId = investmentOrderId,
                Amount = amount,
                Status = ReservationStatus.Active,
                UserId = wallet.UserId,
                CreatedAt = DateTime.UtcNow,
            };

            var transaction = new WalletTransaction
            {
                WalletId = walletId,
                Type = WalletTransactionType.Reserve,
                Amount = amount,
                ReferenceType = ReferenceType.OrderInvestment,
                ReferenceId = investmentOrderId,
                CreatedAt = DateTime.UtcNow,
            };

            await _walletRepo.UpdateAsync(wallet);
            await _reservationRepo.AddAsync(reservation);
            await _walletTransactionRepo.AddAsync(transaction);
            await _walletRepo.SaveChangesAsync();

            return Result.Success();
        }

        public async Task<Result> ReleaseReservationAsync(long reservationId)
        {
            var reservation = await _reservationRepo.GetByIdAsync(reservationId);
            if (reservation is null)
                return Result.Failure($"Reservation with ID {reservationId} not found.");

            var wallet = await _walletRepo.GetByIdAsync(reservation.WalletId);
            if (wallet is null)
                return Result.Failure($"Wallet with ID {reservation.WalletId} not found.");

            wallet.ReservedBalance -= reservation.Amount;

            var transaction = new WalletTransaction
            {
                WalletId = wallet.Id,
                Type = WalletTransactionType.Release,
                Amount = reservation.Amount,
                ReferenceType = ReferenceType.OrderInvestment,
                ReferenceId = reservation.OrderId,
                CreatedAt = DateTime.UtcNow,
            };

            reservation.Status = ReservationStatus.Released;

            await _walletRepo.UpdateAsync(wallet);
            await _walletTransactionRepo.AddAsync(transaction);
            await _reservationRepo.UpdateAsync(reservation);
            await _walletRepo.SaveChangesAsync();

            return Result.Success();
        }

        public async Task<Result> ConsumeReservationAsync(long reservationId)
        {
            var reservation = await _reservationRepo.GetByIdAsync(reservationId);
            if (reservation is null)
                return Result.Failure($"Reservation with ID {reservationId} not found.");

            reservation.Status = ReservationStatus.Consumed;
            await _reservationRepo.UpdateAsync(reservation);
            await _reservationRepo.SaveChangesAsync();

            return Result.Success();
        }

        public async Task<Result<List<WalletTransactionDto>>> GetTransactionHistoryAsync(long userId)
        {
            var wallets = _walletRepo.GetAll(w => w.UserId == userId).ToList();
            var walletIds = wallets.Select(w => w.Id).ToList();

            var transactions = _walletTransactionRepo
                .GetAll(wt => walletIds.Contains(wt.WalletId))
                .OrderByDescending(wt => wt.CreatedAt)
                .ToList();

            var dtos = transactions.Select(t => new WalletTransactionDto
            {
                Id = t.Id,
                Type = t.Type,
                Amount = t.Amount,
                ReferenceType = t.ReferenceType,
                ReferenceId = t.ReferenceId,
                CreatedAt = t.CreatedAt
            }).ToList();

            return Result<List<WalletTransactionDto>>.Success(dtos);
        }
    }
}
