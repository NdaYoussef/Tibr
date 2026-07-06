using Microsoft.Extensions.Logging;
using Tibr.Application.Interfaces;
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
        private readonly IGenericRepository<User, long> _userRepo;
        private readonly IAssetPriceService _assetPriceService;
        private readonly IEmailService _emailService;
        private readonly ILogger<ResolutionService> _logger;

        public ResolutionService(
            IGenericRepository<OrdersInvestment, long> orderRepo,
            IGenericRepository<OrderCondition, long> conditionRepo,
            IGenericRepository<Trade, long> tradeRepo,
            IGenericRepository<Transaction, long> transactionRepo,
            IGenericRepository<Wallet, long> walletRepo,
            IGenericRepository<WalletTransaction, long> walletTransactionRepo,
            IGenericRepository<Reservation, long> reservationRepo,
            IGenericRepository<User, long> userRepo,
            IAssetPriceService assetPriceService,
            IEmailService emailService,
            ILogger<ResolutionService> logger)
        {
            _orderRepo = orderRepo;
            _conditionRepo = conditionRepo;
            _tradeRepo = tradeRepo;
            _transactionRepo = transactionRepo;
            _walletRepo = walletRepo;
            _walletTransactionRepo = walletTransactionRepo;
            _reservationRepo = reservationRepo;
            _userRepo = userRepo;
            _assetPriceService = assetPriceService;
            _emailService = emailService;
            _logger = logger;
        }

        public async Task EvaluateAsync()
        {
            var now = DateTime.UtcNow;

            var pendingOrders = _orderRepo.GetAll(o =>
                o.Status == OrderStatus.Pending
                && !o.IsDeleted
                && o.ExecutionMode == ExecutionMode.Strategy)
                .ToList();

            // Expire stale orders first
            foreach (var order in pendingOrders.Where(o => o.ExpiryDate.HasValue && o.ExpiryDate.Value <= now))
            {
                if (TryTransitionStatus(order, OrderStatus.Expired))
                {
                    await _orderRepo.UpdateAsync(order);
                    await ReleaseReservationAsync(order);
                    await _orderRepo.SaveChangesAsync();
                    await NotifyExpiredAsync(order);
                }
            }

            // Re-load to get fresh state after expiry updates
            var activeOrders = _orderRepo.GetAll(o =>
                o.Status == OrderStatus.Pending
                && !o.IsDeleted
                && o.ExecutionMode == ExecutionMode.Strategy
                && (o.ExpiryDate == null || o.ExpiryDate > now))
                .ToList();

            foreach (var order in activeOrders)
            {
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
                            await TryAutoExecuteAsync(order, currentPrice);
                            break;
                        case ExecutionType.AlertOnly:
                            await TryFireAlertAsync(order, currentPrice);
                            break;
                        case ExecutionType.AlertAndExecute:
                            await TryFireAlertAsync(order, currentPrice);
                            await TryAutoExecuteAsync(order, currentPrice);
                            break;
                    }
                }
            }
        }

        private bool TryTransitionStatus(OrdersInvestment order, OrderStatus newStatus)
        {
            var allowed = newStatus switch
            {
                OrderStatus.Triggered => order.Status == OrderStatus.Pending,
                OrderStatus.Executed => order.Status == OrderStatus.Pending || order.Status == OrderStatus.Triggered,
                OrderStatus.Expired => order.Status == OrderStatus.Pending,
                OrderStatus.Cancelled => order.Status == OrderStatus.Pending,
                _ => false
            };

            if (!allowed) return false;

            order.Status = newStatus;
            return true;
        }

        private async Task TryFireAlertAsync(OrdersInvestment order, decimal currentPrice)
        {
            if (!TryTransitionStatus(order, OrderStatus.Triggered))
            {
                _logger.LogWarning("Strategy {OrderId} already triggered, skipping alert", order.Id);
                return;
            }

            order.CurrentPrice = currentPrice;
            await _orderRepo.UpdateAsync(order);
            await _orderRepo.SaveChangesAsync();

            await NotifyTriggeredAsync(order, currentPrice);
        }

        private async Task TryAutoExecuteAsync(OrdersInvestment order, decimal currentPrice)
        {
            if (!TryTransitionStatus(order, OrderStatus.Executed))
            {
                _logger.LogWarning("Strategy {OrderId} already executed, skipping", order.Id);
                return;
            }

            var totalAmount = order.OrderType == OrderType.Buy
                ? (order.MaxBudgetEgp ?? order.Quantity * currentPrice)
                : order.Quantity * currentPrice;

            if (totalAmount > StrategyDefaults.MaxAutoAmountEgp)
            {
                order.Status = OrderStatus.Cancelled;
                await _orderRepo.UpdateAsync(order);
                await _orderRepo.SaveChangesAsync();
                await NotifyLimitExceededAsync(order, totalAmount);
                return;
            }

            order.CurrentPrice = currentPrice;
            await _orderRepo.UpdateAsync(order);

            var tradeQuantity = order.OrderType == OrderType.Buy && order.MaxBudgetEgp.HasValue
                ? order.MaxBudgetEgp.Value / currentPrice
                : order.Quantity;

            var isBuy = order.OrderType == OrderType.Buy;
            var trade = new Trade
            {
                OrderId = order.Id,
                UserId = order.UserId,
                AssetType = order.AssetType,
                Side = isBuy ? TradeSide.Buy : TradeSide.Sell,
                Quantity = tradeQuantity,
                RemainingQuantity = isBuy ? tradeQuantity : 0,
                ExecutedPrice = currentPrice,
                TotalAmount = totalAmount,
                ExecutedAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
            };
            await _tradeRepo.AddAsync(trade);
            await _tradeRepo.SaveChangesAsync();

            if (!isBuy)
            {
                var remainingToDeduct = tradeQuantity;
                var buyTrades = _tradeRepo
                    .GetAll(t => t.UserId == order.UserId
                              && t.AssetType == order.AssetType
                              && t.Side == TradeSide.Buy
                              && t.RemainingQuantity > 0)
                    .OrderBy(t => t.ExecutedPrice)
                    .ToList();

                foreach (var buyTrade in buyTrades)
                {
                    if (remainingToDeduct <= 0) break;
                    var deduction = Math.Min(buyTrade.RemainingQuantity, remainingToDeduct);
                    buyTrade.RemainingQuantity -= deduction;
                    remainingToDeduct -= deduction;
                    await _tradeRepo.UpdateAsync(buyTrade);
                }

                await _tradeRepo.SaveChangesAsync();
            }

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
                            metalWallet.Balance += tradeQuantity;
                            await _walletRepo.UpdateAsync(metalWallet);

                            await _walletTransactionRepo.AddAsync(new WalletTransaction
                            {
                                WalletId = metalWallet.Id,
                                Type = WalletTransactionType.Credit,
                                Amount = tradeQuantity,
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

            await NotifyExecutedAsync(order, totalAmount);
        }

        private async Task ReleaseReservationAsync(OrdersInvestment order)
        {
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

        private async Task NotifyTriggeredAsync(OrdersInvestment order, decimal currentPrice)
        {
            try
            {
                var user = await _userRepo.GetByIdAsync(order.UserId);
                if (user is null) return;

                var assetLabel = order.AssetType == AssetType.Gold ? "Gold" : "Silver";
                var sideLabel = order.OrderType == OrderType.Buy ? "buy" : "sell";
                var subject = $"Tibr Strategy Alert — {assetLabel} target price reached";
                var body = $@"
                    <h2>Your strategy has been triggered!</h2>
                    <p>{GetTriggerMessage(order, sideLabel, assetLabel, currentPrice)}</p>
                    <p>Log in to Tibr to review and execute your strategy manually.</p>
                    <p><a href='{GetBaseUrl()}'>Go to Tibr</a></p>
                ";
                await _emailService.SendEmailAsync(user.Email, subject, body);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send trigger notification for order {OrderId}", order.Id);
            }
        }

        private async Task NotifyExecutedAsync(OrdersInvestment order, decimal totalAmount)
        {
            try
            {
                var user = await _userRepo.GetByIdAsync(order.UserId);
                if (user is null) return;

                var assetLabel = order.AssetType == AssetType.Gold ? "Gold" : "Silver";
                var sideLabel = order.OrderType == OrderType.Buy ? "bought" : "sold";
                var subject = $"Tibr Execution — {sideLabel} {order.Quantity:F4}g of {assetLabel}";
                var body = $@"
                    <h2>Strategy executed successfully</h2>
                    <p>{GetExecutedMessage(order, sideLabel, assetLabel, totalAmount)}</p>
                    <p>Total amount: <strong>{totalAmount:N2} EGP</strong></p>
                    <p><a href='{GetBaseUrl()}'>View in Tibr</a></p>
                ";
                await _emailService.SendEmailAsync(user.Email, subject, body);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send execution notification for order {OrderId}", order.Id);
            }
        }

        private async Task NotifyExpiredAsync(OrdersInvestment order)
        {
            try
            {
                var user = await _userRepo.GetByIdAsync(order.UserId);
                if (user is null) return;

                var assetLabel = order.AssetType == AssetType.Gold ? "Gold" : "Silver";
                var subject = $"Tibr — Strategy for {assetLabel} has expired";
                var body = $@"
                    <h2>Your strategy has expired</h2>
                    <p>Your strategy for {assetLabel} expired without reaching its target price.</p>
                    <p>You can create a new strategy from your Tibr dashboard.</p>
                    <p><a href='{GetBaseUrl()}'>Go to Tibr</a></p>
                ";
                await _emailService.SendEmailAsync(user.Email, subject, body);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send expiry notification for order {OrderId}", order.Id);
            }
        }

        private async Task NotifyLimitExceededAsync(OrdersInvestment order, decimal totalAmount)
        {
            try
            {
                var user = await _userRepo.GetByIdAsync(order.UserId);
                if (user is null) return;

                var assetLabel = order.AssetType == AssetType.Gold ? "Gold" : "Silver";
                var subject = $"Tibr — Strategy cancelled, limit exceeded";
                var body = $@"
                    <h2>Strategy cancelled</h2>
                    <p>Your strategy for {assetLabel} was cancelled because the amount ({totalAmount:N2} EGP) exceeds the auto-execute limit of {StrategyDefaults.MaxAutoAmountEgp:N2} EGP.</p>
                    <p>Please review and execute manually from your Tibr dashboard.</p>
                    <p><a href='{GetBaseUrl()}'>Go to Tibr</a></p>
                ";
                await _emailService.SendEmailAsync(user.Email, subject, body);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send limit notification for order {OrderId}", order.Id);
            }
        }

        private static string GetTriggerMessage(OrdersInvestment order, string sideLabel, string assetLabel, decimal currentPrice)
        {
            if (order.OrderType == OrderType.Buy && order.MaxBudgetEgp.HasValue)
                return $"Your buy strategy to spend up to {order.MaxBudgetEgp:N2} EGP on {assetLabel} was triggered at {currentPrice:N2} EGP/g.";
            return $"Your {sideLabel} strategy for {order.Quantity:F4}g of {assetLabel} was triggered at {currentPrice:N2} EGP/g.";
        }

        private static string GetExecutedMessage(OrdersInvestment order, string sideLabel, string assetLabel, decimal totalAmount)
        {
            if (order.OrderType == OrderType.Buy && order.MaxBudgetEgp.HasValue)
                return $"We've spent <strong>{order.MaxBudgetEgp:N2} EGP</strong> on {assetLabel} on your behalf.";
            return $"We've {sideLabel} <strong>{order.Quantity:F4}g</strong> of {assetLabel} on your behalf.";
        }

        private static string GetBaseUrl() => "http://localhost:5151";

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
