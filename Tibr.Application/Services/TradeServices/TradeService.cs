using Tibr.Application.Dtos;
using Tibr.Domain.Entities;
using Tibr.Domain.Enums;
using Tibr.Domain.IRepositories;
using Tibr.Domain.ResultPattern;
using Tibr.Application.Services.AssetPriceServices;

namespace Tibr.Application.Services.TradeServices
{
    public class TradeService : ITradeService
    {
        private readonly IGenericRepository<Wallet, long> _walletRepo;
        private readonly IGenericRepository<WalletTransaction, long> _walletTransactionRepo;
        private readonly IAssetPriceService _assetPriceService;
        private readonly IGenericRepository<OrdersInvestment, long> _investmentOrderRepo;
        private readonly IGenericRepository<Trade, long> _tradeRepo;
        private readonly IGenericRepository<Transaction, long> _transactionRepo;

        public TradeService(
            IGenericRepository<Wallet, long> walletRepo,
            IGenericRepository<WalletTransaction, long> walletTransactionRepo,
            IAssetPriceService assetPriceService,
            IGenericRepository<OrdersInvestment, long> investmentOrderRepo,
            IGenericRepository<Trade, long> tradeRepo,
            IGenericRepository<Transaction, long> transactionRepo)
        {
            _walletRepo = walletRepo;
            _walletTransactionRepo = walletTransactionRepo;
            _assetPriceService = assetPriceService;
            _investmentOrderRepo = investmentOrderRepo;
            _tradeRepo = tradeRepo;
            _transactionRepo = transactionRepo;
        }

        public async Task<Result<InvestmentOrderDto>> ExecuteDirectBuyAsync(long userId, DirectBuyDto dto)
        {
            var priceResult = await _assetPriceService.GetCurrentPriceAsync(dto.AssetType);
            if (priceResult.IsFailure)
                return Result<InvestmentOrderDto>.Failure(priceResult.ErrorMessage!);
            if (priceResult.Data is null)
                return Result<InvestmentOrderDto>.Failure("No current price available for this asset.");

            var price = priceResult.Data;

            if (price.SellPrice != dto.ExpectedPrice)
                return Result<InvestmentOrderDto>.Failure(
                    "Price has changed since you viewed it. Please refresh and try again.");

            var cashWallet = _walletRepo.GetAll(w => w.UserId == userId && w.WalletType == WalletType.Cash).FirstOrDefault();
            if (cashWallet is null)
                return Result<InvestmentOrderDto>.Failure("Cash wallet not found.");

            var totalCost = dto.Quantity * price.SellPrice;
            var cashAvailable = cashWallet.Balance - cashWallet.ReservedBalance;
            if (cashAvailable < totalCost)
                return Result<InvestmentOrderDto>.Failure(
                    $"Insufficient cash balance. Available: {cashAvailable:F2}, Required: {totalCost:F2}.");

            var metalType = dto.AssetType == AssetType.Gold ? WalletType.Gold : WalletType.Silver;
            var metalWallet = _walletRepo.GetAll(w => w.UserId == userId && w.WalletType == metalType).FirstOrDefault();
            if (metalWallet is null)
                return Result<InvestmentOrderDto>.Failure($"{metalType} wallet not found.");

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
                Status = OrderStatus.Executed
            };
            await _investmentOrderRepo.AddAsync(order);

            var trade = new Trade
            {
                OrderId = order.Id,
                UserId = userId,
                AssetType = dto.AssetType,
                Side = TradeSide.Buy,
                Quantity = dto.Quantity,
                ExecutedPrice = price.SellPrice,
                TotalAmount = totalCost,
                ExecutedAt = DateTime.UtcNow
            };
            await _tradeRepo.AddAsync(trade);

            var transaction = new Transaction
            {
                UserId = userId,
                TradeId = trade.Id,
                TransactionType = TransactionType.Buy,
                Amount = totalCost,
                Status = TransactionStatusEnum.Success
            };
            await _transactionRepo.AddAsync(transaction);

            cashWallet.Balance -= totalCost;
            await _walletRepo.UpdateAsync(cashWallet);

            await _walletTransactionRepo.AddAsync(new WalletTransaction
            {
                WalletId = cashWallet.Id,
                Type = WalletTransactionType.Debit,
                Amount = totalCost,
                ReferenceType = ReferenceType.Trade,
                ReferenceId = trade.Id
            });

            metalWallet.Balance += dto.Quantity;
            await _walletRepo.UpdateAsync(metalWallet);

            await _walletTransactionRepo.AddAsync(new WalletTransaction
            {
                WalletId = metalWallet.Id,
                Type = WalletTransactionType.Credit,
                Amount = dto.Quantity,
                ReferenceType = ReferenceType.Trade,
                ReferenceId = trade.Id
            });

            await _investmentOrderRepo.SaveChangesAsync();

            var dtoResult = new InvestmentOrderDto
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
            };

            return Result<InvestmentOrderDto>.Success(dtoResult);
        }

        public async Task<Result<InvestmentOrderDto>> ExecuteDirectSellAsync(long userId, DirectSellDto dto)
        {
            var priceResult = await _assetPriceService.GetCurrentPriceAsync(dto.AssetType);
            if (priceResult.IsFailure)
                return Result<InvestmentOrderDto>.Failure(priceResult.ErrorMessage!);
            if (priceResult.Data is null)
                return Result<InvestmentOrderDto>.Failure("No current price available for this asset.");

            var price = priceResult.Data;

            if (price.BuyPrice != dto.ExpectedPrice)
                return Result<InvestmentOrderDto>.Failure(
                    "Price has changed since you viewed it. Please refresh and try again.");

            var metalType = dto.AssetType == AssetType.Gold ? WalletType.Gold : WalletType.Silver;
            var metalWallet = _walletRepo.GetAll(w => w.UserId == userId && w.WalletType == metalType).FirstOrDefault();
            if (metalWallet is null)
                return Result<InvestmentOrderDto>.Failure($"{metalType} wallet not found.");

            var metalAvailable = metalWallet.Balance - metalWallet.ReservedBalance;
            if (metalAvailable < dto.Quantity)
                return Result<InvestmentOrderDto>.Failure(
                    $"Insufficient {dto.AssetType} balance. Available: {metalAvailable:F4}, Required: {dto.Quantity:F4}.");

            var cashWallet = _walletRepo.GetAll(w => w.UserId == userId && w.WalletType == WalletType.Cash).FirstOrDefault();
            if (cashWallet is null)
                return Result<InvestmentOrderDto>.Failure("Cash wallet not found.");

            var totalProceeds = dto.Quantity * price.BuyPrice;

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
                Status = OrderStatus.Executed
            };
            await _investmentOrderRepo.AddAsync(order);

            var trade = new Trade
            {
                OrderId = order.Id,
                UserId = userId,
                AssetType = dto.AssetType,
                Side = TradeSide.Sell,
                Quantity = dto.Quantity,
                ExecutedPrice = price.BuyPrice,
                TotalAmount = totalProceeds,
                ExecutedAt = DateTime.UtcNow
            };
            await _tradeRepo.AddAsync(trade);

            var transaction = new Transaction
            {
                UserId = userId,
                TradeId = trade.Id,
                TransactionType = TransactionType.Sell,
                Amount = totalProceeds,
                Status = TransactionStatusEnum.Success
            };
            await _transactionRepo.AddAsync(transaction);

            metalWallet.Balance -= dto.Quantity;
            await _walletRepo.UpdateAsync(metalWallet);

            await _walletTransactionRepo.AddAsync(new WalletTransaction
            {
                WalletId = metalWallet.Id,
                Type = WalletTransactionType.Debit,
                Amount = dto.Quantity,
                ReferenceType = ReferenceType.Trade,
                ReferenceId = trade.Id
            });

            cashWallet.Balance += totalProceeds;
            await _walletRepo.UpdateAsync(cashWallet);

            await _walletTransactionRepo.AddAsync(new WalletTransaction
            {
                WalletId = cashWallet.Id,
                Type = WalletTransactionType.Credit,
                Amount = totalProceeds,
                ReferenceType = ReferenceType.Trade,
                ReferenceId = trade.Id
            });

            await _investmentOrderRepo.SaveChangesAsync();

            var dtoResult = new InvestmentOrderDto
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
            };

            return Result<InvestmentOrderDto>.Success(dtoResult);
        }
    }
}
