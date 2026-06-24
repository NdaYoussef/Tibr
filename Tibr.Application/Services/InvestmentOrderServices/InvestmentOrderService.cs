using Tibr.Application.Dtos;
using Tibr.Domain.Entities;
using Tibr.Domain.Enums;
using Tibr.Domain.IRepositories;
using Tibr.Domain.ResultPattern;
using Tibr.Application.Services.AssetPriceServices;
using Tibr.Application.Services.WalletServices;

namespace Tibr.Application.Services.InvestmentOrderServices
{
    public class InvestmentOrderService : IInvestmentOrderService
    {
        private readonly IGenericRepository<OrdersInvestment, long> _orderRepository;
        private readonly IGenericRepository<OrderCondition, long> _conditionRepository;
        private readonly IGenericRepository<Wallet, long> _walletRepo;
        private readonly IWalletService _walletService;
        private readonly IAssetPriceService _assetPriceService;

        public InvestmentOrderService(
            IGenericRepository<OrdersInvestment, long> orderRepository,
            IGenericRepository<OrderCondition, long> conditionRepository,
            IGenericRepository<Wallet, long> walletRepo,
            IWalletService walletService,
            IAssetPriceService assetPriceService)
        {
            _orderRepository = orderRepository;
            _conditionRepository = conditionRepository;
            _walletRepo = walletRepo;
            _walletService = walletService;
            _assetPriceService = assetPriceService;
        }

        public async Task<Result<InvestmentOrderDto>> CreateStrategyOrderAsync(long userId, CreateStrategyOrderDto dto)
        {
            var priceResult = await _assetPriceService.GetCurrentPriceAsync(dto.AssetType);
            if (priceResult.IsFailure)
                return Result<InvestmentOrderDto>.Failure(priceResult.ErrorMessage!);

            var currentPrice = priceResult.Data?.SellPrice ?? 0;

            var walletType = dto.OrderType == OrderType.Buy ? WalletType.Cash
                : dto.AssetType == AssetType.Gold ? WalletType.Gold : WalletType.Silver;

            var reserveAmount = dto.OrderType == OrderType.Buy
                ? dto.Quantity * currentPrice
                : dto.Quantity;

            var wallet = _walletRepo.GetAll(w => w.UserId == userId && w.WalletType == walletType).FirstOrDefault();
            if (wallet is null)
                return Result<InvestmentOrderDto>.Failure($"{walletType} wallet not found.");

            var available = wallet.Balance - wallet.ReservedBalance;
            if (available < reserveAmount)
                return Result<InvestmentOrderDto>.Failure(
                    $"Insufficient balance. Available: {available:F4}, Required: {reserveAmount:F4}.");

            var order = new OrdersInvestment
            {
                UserId = userId,
                AssetType = dto.AssetType,
                OrderType = dto.OrderType,
                ExecutionMode = ExecutionMode.Strategy,
                ExecutionType = dto.ExecutionType,
                Quantity = dto.Quantity,
                RequestedPrice = 0,
                CurrentPrice = currentPrice,
                Status = OrderStatus.Pending,
                ExpiryDate = dto.ExpiryDate,
                CreatedAt = DateTime.UtcNow,
            };
            await _orderRepository.AddAsync(order);
            await _orderRepository.SaveChangesAsync();

            foreach (var c in dto.Conditions)
            {
                await _conditionRepository.AddAsync(new OrderCondition
                {
                    OrderId = order.Id,
                    ConditionType = c.ConditionType,
                    Operator = c.Operator,
                    TargetValue = c.TargetValue,
                    CreatedAt = DateTime.UtcNow,
                });
            }
            await _conditionRepository.SaveChangesAsync();

            var reserveResult = await _walletService.ReserveAsync(wallet.Id, order.Id, reserveAmount);
            if (reserveResult.IsFailure)
                return Result<InvestmentOrderDto>.Failure(reserveResult.ErrorMessage!);

            var dtoResult = BuildOrderDto(order, dto.Conditions);

            return Result<InvestmentOrderDto>.Success(dtoResult);
        }

        public async Task<Result> CancelOrderAsync(long userId, long orderId)
        {
            var order = await _orderRepository.GetByIdAsync(orderId);
            if (order is null || order.UserId != userId)
                return Result.Failure("Order not found.");

            if (order.Status != OrderStatus.Pending)
                return Result.Failure("Only pending orders can be cancelled.");

            order.Status = OrderStatus.Cancelled;
            await _orderRepository.UpdateAsync(order);
            await _orderRepository.SaveChangesAsync();

            return Result.Success();
        }

        public async Task<Result<List<InvestmentOrderDto>>> GetUserOrdersAsync(long userId)
        {
            var orders = _orderRepository.GetAll(o => o.UserId == userId).ToList();

            var dtos = orders.Select(o => new InvestmentOrderDto
            {
                Id = o.Id,
                UserId = o.UserId,
                AssetType = o.AssetType,
                OrderType = o.OrderType,
                ExecutionMode = o.ExecutionMode,
                ExecutionType = o.ExecutionType,
                Quantity = o.Quantity,
                RequestedPrice = o.RequestedPrice,
                CurrentPrice = o.CurrentPrice,
                Status = o.Status,
                ExpiryDate = o.ExpiryDate,
                Conditions = o.Conditions.Select(c => new OrderConditionDto
                {
                    ConditionType = c.ConditionType,
                    Operator = c.Operator,
                    TargetValue = c.TargetValue
                }).ToList(),
                Trades = o.Trades.Select(t => new TradeDto
                {
                    Id = t.Id,
                    Side = t.Side,
                    Quantity = t.Quantity,
                    ExecutedPrice = t.ExecutedPrice,
                    TotalAmount = t.TotalAmount,
                    ExecutedAt = t.ExecutedAt
                }).ToList()
            }).ToList();

            return Result<List<InvestmentOrderDto>>.Success(dtos);
        }

        public async Task<Result<List<InvestmentOrderDto>>> GetUserStrategiesAsync(long userId)
        {
            var orders = _orderRepository.GetAll(o => o.UserId == userId && o.ExecutionMode == ExecutionMode.Strategy).ToList();

            var dtos = orders.Select(o => new InvestmentOrderDto
            {
                Id = o.Id,
                UserId = o.UserId,
                AssetType = o.AssetType,
                OrderType = o.OrderType,
                ExecutionMode = o.ExecutionMode,
                ExecutionType = o.ExecutionType,
                Quantity = o.Quantity,
                RequestedPrice = o.RequestedPrice,
                CurrentPrice = o.CurrentPrice,
                Status = o.Status,
                ExpiryDate = o.ExpiryDate,
                Conditions = o.Conditions.Select(c => new OrderConditionDto
                {
                    ConditionType = c.ConditionType,
                    Operator = c.Operator,
                    TargetValue = c.TargetValue
                }).ToList(),
                Trades = o.Trades.Select(t => new TradeDto
                {
                    Id = t.Id,
                    Side = t.Side,
                    Quantity = t.Quantity,
                    ExecutedPrice = t.ExecutedPrice,
                    TotalAmount = t.TotalAmount,
                    ExecutedAt = t.ExecutedAt
                }).ToList()
            }).ToList();

            return Result<List<InvestmentOrderDto>>.Success(dtos);
        }

        public async Task<Result<InvestmentOrderDto>> GetOrderByIdAsync(long userId, long orderId)
        {
            var order = await _orderRepository.GetByIdAsync(orderId);
            if (order is null || order.UserId != userId)
                return Result<InvestmentOrderDto>.Failure("Order not found.");

            var dto = new InvestmentOrderDto
            {
                Id = order.Id,
                UserId = order.UserId,
                AssetType = order.AssetType,
                OrderType = order.OrderType,
                ExecutionMode = order.ExecutionMode,
                ExecutionType = order.ExecutionType,
                Quantity = order.Quantity,
                RequestedPrice = order.RequestedPrice,
                CurrentPrice = order.CurrentPrice,
                Status = order.Status,
                ExpiryDate = order.ExpiryDate,
                Conditions = order.Conditions.Select(c => new OrderConditionDto
                {
                    ConditionType = c.ConditionType,
                    Operator = c.Operator,
                    TargetValue = c.TargetValue
                }).ToList(),
                Trades = order.Trades.Select(t => new TradeDto
                {
                    Id = t.Id,
                    Side = t.Side,
                    Quantity = t.Quantity,
                    ExecutedPrice = t.ExecutedPrice,
                    TotalAmount = t.TotalAmount,
                    ExecutedAt = t.ExecutedAt
                }).ToList()
            };

            return Result<InvestmentOrderDto>.Success(dto);
        }

        private static InvestmentOrderDto BuildOrderDto(OrdersInvestment order, List<OrderConditionDto> conditionDtos)
        {
            return new InvestmentOrderDto
            {
                Id = order.Id,
                UserId = order.UserId,
                AssetType = order.AssetType,
                OrderType = order.OrderType,
                ExecutionMode = order.ExecutionMode,
                ExecutionType = order.ExecutionType,
                Quantity = order.Quantity,
                RequestedPrice = order.RequestedPrice,
                CurrentPrice = order.CurrentPrice,
                Status = order.Status,
                ExpiryDate = order.ExpiryDate,
                Conditions = conditionDtos
            };
        }
    }
}
