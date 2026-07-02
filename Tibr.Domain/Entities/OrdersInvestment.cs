using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Tibr.Domain.Common.Classes;
using Tibr.Domain.Enums;

namespace Tibr.Domain.Entities
{
    public class OrdersInvestment : BaseEntity<long>
    {
        public long UserId { get; set; }

        public AssetType AssetType { get; set; }

        public OrderType OrderType { get; set; }

        public ExecutionMode ExecutionMode { get; set; }

        public decimal Quantity { get; set; }

        public decimal RequestedPrice { get; set; }

        public decimal CurrentPrice { get; set; }

        public OrderStatus Status { get; set; }

        public ExecutionType ExecutionType { get; set; }

        public decimal? MaxBudgetEgp { get; set; }

        public DateTime? ExpiryDate { get; set; }

        public ICollection<OrderCondition> Conditions { get; set; } = [];

        public ICollection<Trade> Trades { get; set; } = [];
    }
}
