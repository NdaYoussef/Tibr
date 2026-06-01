using System;
using System.Collections.Generic;
using System.Text;
using Tibr.Domain.Common.Classes;
using Tibr.Domain.Enums;

namespace Tibr.Domain.Entities
{
    public class Alert : BaseEntity<long>
    {
        public Guid UserId { get; set; }

        public Guid OrderId { get; set; }

        public OrdersInvestment Order { get; set; } = default!;

        public AlertType AlertType { get; set; }

        public string Message { get; set; } = string.Empty;

        public DateTime SentAt { get; set; }
    }
}
