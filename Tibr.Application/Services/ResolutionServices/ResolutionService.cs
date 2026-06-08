using Tibr.Domain.Entities;
using Tibr.Domain.Enums;
using Tibr.Domain.IRepositories;
using Tibr.Application.Services.AssetPriceServices;
using Tibr.Application.Services.WalletServices;

namespace Tibr.Application.Services.ResolutionServices
{
    public class ResolutionService : IResolutionService
    {
        private readonly IGenericRepository<OrdersInvestment, long> _orderRepo;
        private readonly IGenericRepository<OrderCondition, long> _conditionRepo;
        private readonly IGenericRepository<Trade, long> _tradeRepo;
        private readonly IGenericRepository<Transaction, long> _transactionRepo;
        private readonly IGenericRepository<Wallet, long> _walletRepo;
        private readonly IGenericRepository<WalletTransaction, long> _walletTransactionRepo;
        private readonly IGenericRepository<Reservation, long> _reservationRepo;
        private readonly IAssetPriceService _assetPriceService;

        public ResolutionService(
            IGenericRepository<OrdersInvestment, long> orderRepo,
            IGenericRepository<OrderCondition, long> conditionRepo,
            IGenericRepository<Trade, long> tradeRepo,
            IGenericRepository<Transaction, long> transactionRepo,
            IGenericRepository<Wallet, long> walletRepo,
            IGenericRepository<WalletTransaction, long> walletTransactionRepo,
            IGenericRepository<Reservation, long> reservationRepo,
            IAssetPriceService assetPriceService)
        {
            _orderRepo = orderRepo;
            _conditionRepo = conditionRepo;
            _tradeRepo = tradeRepo;
            _transactionRepo = transactionRepo;
            _walletRepo = walletRepo;
            _walletTransactionRepo = walletTransactionRepo;
            _reservationRepo = reservationRepo;
            _assetPriceService = assetPriceService;
        }

        public async Task EvaluateAsync()
        {
            var now = DateTime.UtcNow;

            var orders = _orderRepo.GetAll(o =>
                (o.Status == OrderStatus.Pending || o.Status == OrderStatus.Triggered)
                && !o.IsDeleted
                && (o.ExpiryDate == null || o.ExpiryDate > now))
                .ToList();

            foreach (var order in orders)
            {
                if (order.ExpiryDate.HasValue && order.ExpiryDate.Value <= now)
                {
                    await ExpireOrderAsync(order);
                    continue;
                }

                var priceResult = await _assetPriceService.GetCurrentPriceAsync(order.AssetType);
                if (priceResult.IsFailure || priceResult.Data is null)
                    continue;

                var currentPrice = order.OrderType == OrderType.Buy
                    ? priceResult.Data.SellPrice
                    : priceResult.Data.BuyPrice;

                var conditions = _conditionRepo.GetAll(c => c.OrderId == order.Id).ToList();

                if (!conditions.Any() || ConditionsMet(conditions, currentPrice))
                {
                    switch (order.ExecutionType)
                    {
                        case ExecutionType.AutoExecute:
                            await ExecuteOrderAsync(order, currentPrice);
                            break;
                        case ExecutionType.AlertOnly:
                            await TriggerOrderAsync(order, currentPrice);
                            break;
                        case ExecutionType.AlertAndExecute:
                            await ExecuteOrderAsync(order, currentPrice);
                            break;
                    }
                }
            }
        }

        private async Task ExecuteOrderAsync(OrdersInvestment order, decimal executedPrice)
        {
            order.Status = OrderStatus.Executed;
            order.CurrentPrice = executedPrice;
            await _orderRepo.UpdateAsync(order);

            var totalAmount = order.OrderType == OrderType.Buy
                ? order.Quantity * executedPrice
                : order.Quantity * executedPrice;

            var trade = new Trade
            {
                OrderId = order.Id,
                UserId = order.UserId,
                AssetType = order.AssetType,
                Side = order.OrderType == OrderType.Buy ? TradeSide.Buy : TradeSide.Sell,
                Quantity = order.Quantity,
                ExecutedPrice = executedPrice,
                TotalAmount = totalAmount,
                ExecutedAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
            };
            await _tradeRepo.AddAsync(trade);

            var transaction = new Transaction
            {
                UserId = order.UserId,
                TradeId = trade.Id,
                TransactionType = order.OrderType == OrderType.Buy ? TransactionType.Buy : TransactionType.Sell,
                Amount = totalAmount,
                Status = TransactionStatusEnum.Success,
                CreatedAt = DateTime.UtcNow,
            };
            await _transactionRepo.AddAsync(transaction);

            var reservation = _reservationRepo.GetAll(r => r.OrderId == order.Id && r.Status == ReservationStatus.Active).FirstOrDefault();
            if (reservation is not null)
            {
                var wallet = await _walletRepo.GetByIdAsync(reservation.WalletId);
                if (wallet is not null)
                {
                    wallet.ReservedBalance -= reservation.Amount;

                    if (order.OrderType == OrderType.Buy)
                    {
                        wallet.Balance -= reservation.Amount;

                        var metalType = order.AssetType == AssetType.Gold ? WalletType.Gold : WalletType.Silver;
                        var metalWallet = _walletRepo.GetAll(w => w.UserId == order.UserId && w.WalletType == metalType).FirstOrDefault();
                        if (metalWallet is not null)
                        {
                            metalWallet.Balance += order.Quantity;
                            await _walletRepo.UpdateAsync(metalWallet);

                            await _walletTransactionRepo.AddAsync(new WalletTransaction
                            {
                                WalletId = metalWallet.Id,
                                Type = WalletTransactionType.Credit,
                                Amount = order.Quantity,
                                ReferenceType = ReferenceType.Trade,
                                ReferenceId = trade.Id,
                                CreatedAt = DateTime.UtcNow,
                            });
                        }
                    }
                    else
                    {
                        wallet.Balance -= order.Quantity;

                        var cashWallet = _walletRepo.GetAll(w => w.UserId == order.UserId && w.WalletType == WalletType.Cash).FirstOrDefault();
                        if (cashWallet is not null)
                        {
                            cashWallet.Balance += totalAmount;
                            await _walletRepo.UpdateAsync(cashWallet);

                            await _walletTransactionRepo.AddAsync(new WalletTransaction
                            {
                                WalletId = cashWallet.Id,
                                Type = WalletTransactionType.Credit,
                                Amount = totalAmount,
                                ReferenceType = ReferenceType.Trade,
                                ReferenceId = trade.Id,
                                CreatedAt = DateTime.UtcNow,
                            });
                        }
                    }

                    await _walletRepo.UpdateAsync(wallet);

                    await _walletTransactionRepo.AddAsync(new WalletTransaction
                    {
                        WalletId = wallet.Id,
                        Type = WalletTransactionType.Debit,
                        Amount = order.OrderType == OrderType.Buy ? reservation.Amount : order.Quantity,
                        ReferenceType = ReferenceType.Trade,
                        ReferenceId = trade.Id,
                        CreatedAt = DateTime.UtcNow,
                    });
                }

                reservation.Status = ReservationStatus.Consumed;
                await _reservationRepo.UpdateAsync(reservation);
            }

            await _orderRepo.SaveChangesAsync();
        }

        private async Task TriggerOrderAsync(OrdersInvestment order, decimal currentPrice)
        {
            order.Status = OrderStatus.Triggered;
            order.CurrentPrice = currentPrice;
            await _orderRepo.UpdateAsync(order);
            await _orderRepo.SaveChangesAsync();
        }

        private async Task ExpireOrderAsync(OrdersInvestment order)
        {
            order.Status = OrderStatus.Expired;
            await _orderRepo.UpdateAsync(order);

            var reservation = _reservationRepo.GetAll(r => r.OrderId == order.Id && r.Status == ReservationStatus.Active).FirstOrDefault();
            if (reservation is not null)
            {
                var wallet = await _walletRepo.GetByIdAsync(reservation.WalletId);
                if (wallet is not null)
                {
                    wallet.ReservedBalance -= reservation.Amount;
                    await _walletRepo.UpdateAsync(wallet);
                }

                reservation.Status = ReservationStatus.Released;
                await _reservationRepo.UpdateAsync(reservation);
            }

            await _orderRepo.SaveChangesAsync();
        }

        private static bool ConditionsMet(List<OrderCondition> conditions, decimal currentPrice)
        {
            return conditions.All(c => EvaluateCondition(c, currentPrice));
        }

        private static bool EvaluateCondition(OrderCondition condition, decimal currentPrice)
        {
            var value = condition.ConditionType == ConditionType.PriceTarget ? currentPrice : currentPrice;

            return condition.Operator switch
            {
                ConditionOperator.GreaterThan => value > condition.TargetValue,
                ConditionOperator.GreaterThanOrEqual => value >= condition.TargetValue,
                ConditionOperator.LessThan => value < condition.TargetValue,
                ConditionOperator.LessThanOrEqual => value <= condition.TargetValue,
                ConditionOperator.Equal => value == condition.TargetValue,
                _ => false
            };
        }
    }
}
