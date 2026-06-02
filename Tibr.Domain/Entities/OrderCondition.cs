using System;
using System.Collections.Generic;
using System.Text;
using Tibr.Domain.Common.Classes;
using Tibr.Domain.Enums;

namespace Tibr.Domain.Entities
{
    public class OrderCondition : BaseEntity<long>
    {
        public long OrderId { get; set; }

        public OrdersInvestment Order { get; set; } = default!;

        public ConditionType ConditionType { get; set; }

        public ConditionOperator Operator { get; set; }

        public decimal TargetValue { get; set; }
    }
}
