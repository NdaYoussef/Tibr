using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Tibr.Application.Dtos.Paymob;

namespace Tibr.Application.Services
{
    public interface IPaymobService
    {
        /// <summary>
        /// Runs the 3-step Paymob flow and returns the iframe URL to redirect the user to.
        /// </summary>
        Task<string> CreatePaymentUrlAsync(CreatePaymentRequest request);

        /// <summary>
        /// Validates the HMAC signature on an incoming Paymob callback.
        /// Returns true if the signature is valid and the transaction succeeded.
        /// </summary>
        bool VerifyCallback(PaymobCallbackPayload payload, string receivedHmac);

        /// <summary>
        /// Processes a successful Paymob callback: marks the order as paid
        /// and creates a Payment record.
        /// </summary>
        Task ProcessCallbackAsync(PaymobCallbackPayload payload);
    }
}
