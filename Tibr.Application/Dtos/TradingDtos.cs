using Tibr.Domain.Enums;

namespace Tibr.Application.Dtos
{
    public class DirectBuyDto
    {
        public AssetType AssetType { get; set; }
        public decimal Quantity { get; set; }
        public decimal ExpectedPrice { get; set; }
    }

    public class DirectSellDto
    {
        public AssetType AssetType { get; set; }
        public decimal Quantity { get; set; }
        public decimal ExpectedPrice { get; set; }
    }

    public class CreateStrategyOrderDto
    {
        public AssetType AssetType { get; set; }
        public OrderType OrderType { get; set; }
        public ExecutionType ExecutionType { get; set; }
        public decimal Quantity { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public List<OrderConditionDto> Conditions { get; set; } = [];
    }

    public class OrderConditionDto
    {
        public ConditionType ConditionType { get; set; }
        public ConditionOperator Operator { get; set; }
        public decimal TargetValue { get; set; }
    }

    public class InvestmentOrderDto
    {
        public long Id { get; set; }
        public long UserId { get; set; }
        public AssetType AssetType { get; set; }
        public OrderType OrderType { get; set; }
        public ExecutionMode ExecutionMode { get; set; }
        public ExecutionType ExecutionType { get; set; }
        public decimal Quantity { get; set; }
        public decimal RequestedPrice { get; set; }
        public decimal CurrentPrice { get; set; }
        public OrderStatus Status { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public List<OrderConditionDto> Conditions { get; set; } = [];
        public List<TradeDto> Trades { get; set; } = [];
    }

    public class TradeDto
    {
        public long Id { get; set; }
        public TradeSide Side { get; set; }
        public decimal Quantity { get; set; }
        public decimal ExecutedPrice { get; set; }
        public decimal TotalAmount { get; set; }
        public DateTime ExecutedAt { get; set; }
    }
}
