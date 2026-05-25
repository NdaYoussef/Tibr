using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Tibr.Application.Dtos.Paymob;
using Tibr.Application.Services;

namespace Tibr.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PaymentController : ControllerBase
    {
        private readonly IPaymobService _paymob;
        private readonly ILogger<PaymentController> _logger;

        public PaymentController(IPaymobService paymob, ILogger<PaymentController> logger)
        {
            _paymob = paymob;
            _logger = logger;
        }

        /// <summary>
        /// Initiates a payment and returns the Paymob iframe URL.
        /// The client should redirect the user to this URL.
        /// </summary>
        [HttpPost("initiate")]
        public async Task<IActionResult> Initiate([FromBody] CreatePaymentRequest request)
        {
            var url = await _paymob.CreatePaymentUrlAsync(request);
            return Ok(new { paymentUrl = url });
        }

        /// <summary>
        /// Paymob POSTs here after a transaction completes.
        /// The HMAC is passed as a query parameter: ?hmac=...
        /// </summary>
        [HttpPost("callback/processed")]
        public IActionResult Callback(
            [FromBody] PaymobCallbackPayload payload,
            [FromQuery] string hmac
        )
        {
            if (!_paymob.VerifyCallback(payload, hmac))
            {
                _logger.LogWarning("Paymob callback received with invalid HMAC.");
                return Unauthorized("Invalid HMAC signature.");
            }

            var transaction = payload.Obj;

            if (transaction?.Success == true)
            {
                _logger.LogInformation(
                    "Payment succeeded. TransactionId={TxId}, OrderId={OrderId}, Amount={Amount} {Currency}",
                    transaction.Id,
                    transaction.Order?.Id,
                    transaction.AmountCents,
                    transaction.Currency
                );

                // TODO: mark your order as paid in the database here
            }
            else
            {
                _logger.LogWarning(
                    "Payment failed or pending. TransactionId={TxId}, Success={Success}, Pending={Pending}",
                    transaction?.Id,
                    transaction?.Success,
                    transaction?.Pending
                );
            }

            // Paymob expects a 200 OK — always return 200 even on failure
            return Ok();
        }

        /// <summary>
        /// Paymob redirects the user's browser here after payment completes.
        /// Not a webhook — this is a GET redirect for UX purposes only.
        /// Do NOT use this as source of truth for payment status; use the processed callback instead.
        /// </summary>
        [HttpGet("callback/response")]
        public IActionResult ResponseCallback([FromQuery] bool success)
        {
            // Just redirect user to your frontend success/failure page
            return Redirect(
                success
                    ? "https://yourapp.com/payment/success"
                    : "https://yourapp.com/payment/failed"
            );
        }
    }
}
