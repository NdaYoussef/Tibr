using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Tibr.Application.Dtos.Paymob;
using Tibr.Application.Services.DepositServices;
using Tibr.Application.Services.PaymentServices;
using Tibr.Infrastructure.Config;

namespace Tibr.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PaymentController : ControllerBase
    {
        private readonly IPaymobService _paymob;
        private readonly IDepositService _depositService;
        private readonly ILogger<PaymentController> _logger;
        private readonly PaymobSettings _settings;

        public PaymentController(
            IPaymobService paymob,
            IDepositService depositService,
            ILogger<PaymentController> logger,
            IOptions<PaymobSettings> settings
        )
        {
            _paymob = paymob;
            _depositService = depositService;
            _logger = logger;
            _settings = settings.Value;
        }

        /// <summary>
        /// Initiates a payment and returns the Paymob iframe URL.
        /// The client should redirect the user to this URL.
        /// </summary>
        [HttpPost("initiate")]
        public async Task<ActionResult<PaymentInitiateResponse>> Initiate(
            [FromBody] CreatePaymentRequest request
        )
        {
            var url = await _paymob.CreatePaymentUrlAsync(request);
            return Ok(new PaymentInitiateResponse(url));
        }

        /// <summary>
        /// Paymob POSTs here after a transaction completes.
        /// The HMAC is passed as a query parameter: ?hmac=...
        /// </summary>
        [HttpPost("callback/processed")]
        public async Task<ActionResult> Callback(
            [FromBody] PaymobCallbackPayload payload,
            [FromQuery] string hmac
        )
        {
            if (!_paymob.VerifyCallback(payload, hmac))
            {
                _logger.LogWarning("Paymob callback received with invalid HMAC.");
                return Unauthorized("Invalid HMAC signature.");
            }

            var specialRef = payload.Obj?.Order?.SpecialReference;

            if (specialRef?.StartsWith("deposit-") == true)
            {
                var success = payload.Obj?.Success ?? false;
                await _depositService.HandleCallbackAsync(specialRef, success);
                return Ok();
            }

            await _paymob.ProcessCallbackAsync(payload);

            return Ok();
        }

        /// <summary>
        /// Paymob redirects the user's browser here after payment completes.
        /// Not a webhook — this is a GET redirect for UX purposes only.
        /// Do NOT use this as source of truth for payment status; use the processed callback instead.
        /// </summary>
        [HttpGet("callback/response")]
        public ActionResult ResponseCallback([FromQuery] bool success)
        {
            _logger.LogInformation(
                "Paymob response callback: QueryString={Query}, Success={Success}",
                Request.QueryString,
                success
            );

            var orderId = Request.Query["merchant_order_id"];
            var status = success ? "success" : "failed";

            var redirectUrl = string.IsNullOrEmpty(orderId)
                ? $"{_settings.FrontendBaseUrl}/orders?payment={status}"
                : $"{_settings.FrontendBaseUrl}/orders/{orderId}?payment={status}";

            return Redirect(redirectUrl);
        }
    }
}
