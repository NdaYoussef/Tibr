using Microsoft.EntityFrameworkCore;
using Tibr.Application.Dtos;
using Tibr.Application.Services.AssetPriceServices;
using Tibr.Domain.Entities;
using Tibr.Domain.Enums;
using Tibr.Domain.IRepositories;
using Tibr.Domain.ResultPattern;

namespace Tibr.Application.Services.TradeServices
{
    public class TradeService : ITradeService
    {
        private readonly DbContext _context;

        private readonly IGenericRepository<Wallet, long> _walletRepo;
        private readonly IGenericRepository<WalletTransaction, long> _walletTransactionRepo;
        private readonly IAssetPriceService _assetPriceService;
        private readonly IGenericRepository<OrdersInvestment, long> _investmentOrderRepo;
        private readonly IGenericRepository<Trade, long> _tradeRepo;
        private readonly IGenericRepository<Transaction, long> _transactionRepo;

        public TradeService(
            DbContext context,
            IGenericRepository<Wallet, long> walletRepo,
            IGenericRepository<WalletTransaction, long> walletTransactionRepo,
            IAssetPriceService assetPriceService,
            IGenericRepository<OrdersInvestment, long> investmentOrderRepo,
            IGenericRepository<Trade, long> tradeRepo,
            IGenericRepository<Transaction, long> transactionRepo)
        {
            _context = context;
            _walletRepo = walletRepo;
            _walletTransactionRepo = walletTransactionRepo;
            _assetPriceService = assetPriceService;
            _investmentOrderRepo = investmentOrderRepo;
            _tradeRepo = tradeRepo;
            _transactionRepo = transactionRepo;
        }

        // =========================
        // BUY
        // =========================
        public async Task<Result<InvestmentOrderDto>> ExecuteDirectBuyAsync(long userId, DirectBuyDto dto)
        {
            await using var tx = await _context.Database.BeginTransactionAsync();

            try
            {
                var priceResult = await _assetPriceService.GetCurrentPriceAsync(dto.AssetType);
                if (priceResult.IsFailure)
                    return Result<InvestmentOrderDto>.Failure(priceResult.ErrorMessage!);

                var price = priceResult.Data!;
                var totalCost = dto.Quantity * price.SellPrice;

                var cashWallet = _walletRepo.GetAll(w => w.UserId == userId && w.WalletType == WalletType.Cash).FirstOrDefault();
                if (cashWallet is null)
                    return Result<InvestmentOrderDto>.Failure("Cash wallet not found.");

                if (cashWallet.Balance - cashWallet.ReservedBalance < totalCost)
                    return Result<InvestmentOrderDto>.Failure("Insufficient balance.");

                var metalType = dto.AssetType == AssetType.Gold ? WalletType.Gold : WalletType.Silver;
                var metalWallet = _walletRepo.GetAll(w => w.UserId == userId && w.WalletType == metalType).FirstOrDefault();
                if (metalWallet is null)
                    return Result<InvestmentOrderDto>.Failure("Metal wallet not found.");

                // 1) Order
                var order = new OrdersInvestment
                {
                    UserId = userId,
                    AssetType = dto.AssetType,
                    OrderType = OrderType.Buy,
                    ExecutionMode = ExecutionMode.Direct,
                    ExecutionType = ExecutionType.AutoExecute,
                    Quantity = dto.Quantity,
                    RequestedPrice = dto.ExpectedPrice,
                    CurrentPrice = price.SellPrice,
                    Status = OrderStatus.Executed,
                    CreatedAt = DateTime.UtcNow
                };

                await _investmentOrderRepo.AddAsync(order);
                await _investmentOrderRepo.SaveChangesAsync();

                // 2) Trade
                var trade = new Trade
                {
                    OrderId = order.Id,
                    UserId = userId,
                    AssetType = dto.AssetType,
                    Side = TradeSide.Buy,
                    Quantity = dto.Quantity,
                    ExecutedPrice = price.SellPrice,
                    TotalAmount = totalCost,
                    ExecutedAt = DateTime.UtcNow,
                    CreatedAt = DateTime.UtcNow
                };

                await _tradeRepo.AddAsync(trade);
                await _tradeRepo.SaveChangesAsync();

                // 3) Transaction
                var transactionEntity = new Transaction
                {
                    UserId = userId,
                    TradeId = trade.Id,
                    TransactionType = TransactionType.Buy,
                    Amount = totalCost,
                    Status = TransactionStatusEnum.Success,
                    CreatedAt = DateTime.UtcNow
                };

                await _transactionRepo.AddAsync(transactionEntity);

                // 4) Wallet update
                cashWallet.Balance -= totalCost;
                metalWallet.Balance += dto.Quantity;

                await _walletRepo.UpdateAsync(cashWallet);
                await _walletRepo.UpdateAsync(metalWallet);

                await _walletTransactionRepo.AddAsync(new WalletTransaction
                {
                    WalletId = cashWallet.Id,
                    Type = WalletTransactionType.Debit,
                    Amount = totalCost,
                    ReferenceType = ReferenceType.Trade,
                    ReferenceId = trade.Id,
                    CreatedAt = DateTime.UtcNow
                });

                await _walletTransactionRepo.AddAsync(new WalletTransaction
                {
                    WalletId = metalWallet.Id,
                    Type = WalletTransactionType.Credit,
                    Amount = dto.Quantity,
                    ReferenceType = ReferenceType.Trade,
                    ReferenceId = trade.Id,
                    CreatedAt = DateTime.UtcNow
                });

                await _investmentOrderRepo.SaveChangesAsync();

                await tx.CommitAsync();

                return Result<InvestmentOrderDto>.Success(new InvestmentOrderDto
                {
                    Id = order.Id,
                    UserId = userId,
                    AssetType = order.AssetType,
                    OrderType = order.OrderType,
                    ExecutionMode = order.ExecutionMode,
                    ExecutionType = order.ExecutionType,
                    Quantity = order.Quantity,
                    RequestedPrice = order.RequestedPrice,
                    CurrentPrice = order.CurrentPrice,
                    Status = order.Status,
                    Trades =
                    [
                        new TradeDto
                        {
                            Id = trade.Id,
                            Side = trade.Side,
                            Quantity = trade.Quantity,
                            ExecutedPrice = trade.ExecutedPrice,
                            TotalAmount = trade.TotalAmount,
                            ExecutedAt = trade.ExecutedAt
                        }
                    ]
                });
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                return Result<InvestmentOrderDto>.Failure(ex.Message);
            }
        }

        // =========================
        // SELL
        // =========================
        public async Task<Result<InvestmentOrderDto>> ExecuteDirectSellAsync(long userId, DirectSellDto dto)
        {
            await using var tx = await _context.Database.BeginTransactionAsync();

            try
            {
                var priceResult = await _assetPriceService.GetCurrentPriceAsync(dto.AssetType);
                if (priceResult.IsFailure)
                    return Result<InvestmentOrderDto>.Failure(priceResult.ErrorMessage!);

                var price = priceResult.Data!;
                var totalProceeds = dto.Quantity * price.BuyPrice;

                var metalType = dto.AssetType == AssetType.Gold ? WalletType.Gold : WalletType.Silver;
                var metalWallet = _walletRepo.GetAll(w => w.UserId == userId && w.WalletType == metalType).FirstOrDefault();

                if (metalWallet is null)
                    return Result<InvestmentOrderDto>.Failure("Metal wallet not found.");

                if (metalWallet.Balance - metalWallet.ReservedBalance < dto.Quantity)
                    return Result<InvestmentOrderDto>.Failure("Insufficient metal balance.");

                var cashWallet = _walletRepo.GetAll(w => w.UserId == userId && w.WalletType == WalletType.Cash).FirstOrDefault();

                if (cashWallet is null)
                    return Result<InvestmentOrderDto>.Failure("Cash wallet not found.");

                // 1) Order
                var order = new OrdersInvestment
                {
                    UserId = userId,
                    AssetType = dto.AssetType,
                    OrderType = OrderType.Sell,
                    ExecutionMode = ExecutionMode.Direct,
                    ExecutionType = ExecutionType.AutoExecute,
                    Quantity = dto.Quantity,
                    RequestedPrice = dto.ExpectedPrice,
                    CurrentPrice = price.BuyPrice,
                    Status = OrderStatus.Executed,
                    CreatedAt = DateTime.UtcNow
                };

                await _investmentOrderRepo.AddAsync(order);
                await _investmentOrderRepo.SaveChangesAsync();

                // 2) Trade
                var trade = new Trade
                {
                    OrderId = order.Id,
                    UserId = userId,
                    AssetType = dto.AssetType,
                    Side = TradeSide.Sell,
                    Quantity = dto.Quantity,
                    ExecutedPrice = price.BuyPrice,
                    TotalAmount = totalProceeds,
                    ExecutedAt = DateTime.UtcNow,
                    CreatedAt = DateTime.UtcNow
                };

                await _tradeRepo.AddAsync(trade);
                await _tradeRepo.SaveChangesAsync();

                // 3) Transaction
                var transactionEntity = new Transaction
                {
                    UserId = userId,
                    TradeId = trade.Id,
                    TransactionType = TransactionType.Sell,
                    Amount = totalProceeds,
                    Status = TransactionStatusEnum.Success,
                    CreatedAt = DateTime.UtcNow
                };

                await _transactionRepo.AddAsync(transactionEntity);

                // 4) Wallet update
                metalWallet.Balance -= dto.Quantity;
                cashWallet.Balance += totalProceeds;

                await _walletRepo.UpdateAsync(metalWallet);
                await _walletRepo.UpdateAsync(cashWallet);

                await _walletTransactionRepo.AddAsync(new WalletTransaction
                {
                    WalletId = metalWallet.Id,
                    Type = WalletTransactionType.Debit,
                    Amount = dto.Quantity,
                    ReferenceType = ReferenceType.Trade,
                    ReferenceId = trade.Id,
                    CreatedAt = DateTime.UtcNow
                });

                await _walletTransactionRepo.AddAsync(new WalletTransaction
                {
                    WalletId = cashWallet.Id,
                    Type = WalletTransactionType.Credit,
                    Amount = totalProceeds,
                    ReferenceType = ReferenceType.Trade,
                    ReferenceId = trade.Id,
                    CreatedAt = DateTime.UtcNow
                });

                await _investmentOrderRepo.SaveChangesAsync();

                await tx.CommitAsync();

                return Result<InvestmentOrderDto>.Success(new InvestmentOrderDto
                {
                    Id = order.Id,
                    UserId = userId,
                    AssetType = order.AssetType,
                    OrderType = order.OrderType,
                    ExecutionMode = order.ExecutionMode,
                    ExecutionType = order.ExecutionType,
                    Quantity = order.Quantity,
                    RequestedPrice = order.RequestedPrice,
                    CurrentPrice = order.CurrentPrice,
                    Status = order.Status,
                    Trades =
                    [
                        new TradeDto
                        {
                            Id = trade.Id,
                            Side = trade.Side,
                            Quantity = trade.Quantity,
                            ExecutedPrice = trade.ExecutedPrice,
                            TotalAmount = trade.TotalAmount,
                            ExecutedAt = trade.ExecutedAt
                        }
                    ]
                });
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                return Result<InvestmentOrderDto>.Failure(ex.Message);
            }
        }
    }
}