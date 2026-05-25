using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Tibr.Application.Dtos.Paymob
{
    public class CreatePaymentRequest
    {
        /// <summary>Amount in the smallest currency unit (e.g. cents/piasters).</summary>
        public int AmountCents { get; set; }

        /// <summary>The Order ID to pay for.</summary>
        public long OrderId { get; set; }

        public string Currency { get; set; } = "EGP";

        // Billing data — Paymob requires these even for sandbox
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
    }
}
