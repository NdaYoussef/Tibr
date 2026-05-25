using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Tibr.Application.Dtos.Paymob;
using Tibr.Application.Services;
using Tibr.Infrastructure.Config;

namespace Tibr.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PaymentController : ControllerBase
    {
        private readonly IPaymobService _paymob;
        private readonly ILogger<PaymentController> _logger;
        private readonly PaymobSettings _settings;

        public PaymentController(
            IPaymobService paymob,
            ILogger<PaymentController> logger,
            IOptions<PaymobSettings> settings)
        {
            _paymob = paymob;
            _logger = logger;
            _settings = settings.Value;
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
        public async Task<IActionResult> Callback(
            [FromBody] PaymobCallbackPayload payload,
            [FromQuery] string hmac
        )
        {
            if (!_paymob.VerifyCallback(payload, hmac))
            {
                _logger.LogWarning("Paymob callback received with invalid HMAC.");
                return Unauthorized("Invalid HMAC signature.");
            }

            await _paymob.ProcessCallbackAsync(payload);

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
            return Redirect(
                success
                    ? _settings.SuccessRedirectUrl
                    : _settings.FailureRedirectUrl
            );
        }
    }
}
