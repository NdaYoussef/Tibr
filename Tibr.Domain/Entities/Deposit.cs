using System;
using System.Collections.Generic;
using System.Text;
using Tibr.Domain.Common.Classes;
using Tibr.Domain.Enums;

namespace Tibr.Domain.Entities
{
    public class Deposit : BaseEntity<long>
    {
        public Guid UserId { get; set; }

        public decimal Amount { get; set; }

        public DepositStatus Status { get; set; }

        public PaymentMethod PaymentMethod { get; set; }

        public string TransactionRef { get; set; } = string.Empty;
    }
}
