using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Tibr.Application.Dtos.Paymob;
using Tibr.Application.Services.PaymentServices;
using Tibr.Infrastructure.Config;

namespace Tibr.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PaymentController : ControllerBase
{
    private readonly IPaymentGateway _gateway;
    private readonly PaymentService _paymentService;
    private readonly ILogger<PaymentController> _logger;
    private readonly PaymobSettings _settings;

    public PaymentController(
        IPaymentGateway gateway,
        PaymentService paymentService,
        ILogger<PaymentController> logger,
        IOptions<PaymobSettings> settings)
    {
        _gateway = gateway;
        _paymentService = paymentService;
        _logger = logger;
        _settings = settings.Value;
    }

    [HttpPost("initiate")]
    public async Task<ActionResult<PaymentInitiateResponse>> Initiate(
        [FromBody] CreatePaymentRequest request)
    {
        var result = await _paymentService.InitiateOrderPaymentAsync(request.OrderId, request);

        if (result.IsFailure)
            return BadRequest(result.ErrorMessage);

        return Ok(new PaymentInitiateResponse(result.Data!));
    }

    [HttpPost("callback/processed"), AllowAnonymous]
    public async Task<ActionResult> Callback(
        [FromBody] JsonElement payload,
        [FromQuery] string hmac)
    {
        var rawBody = payload.GetRawText();

        if (!_gateway.VerifyWebhook(rawBody, hmac))
        {
            _logger.LogWarning("Paymob callback received with invalid HMAC.");
            return Unauthorized("Invalid HMAC signature.");
        }
        
        await _paymentService.HandlePaymentCallbackAsync(rawBody);

        return Ok();
    }

    [HttpGet("callback/response"), AllowAnonymous]
    public ActionResult ResponseCallback([FromQuery] bool success)
    {
        _logger.LogInformation(
            "Paymob response callback: QueryString={Query}, Success={Success}",
            Request.QueryString,
            success);

        var merchantOrderId = Request.Query["merchant_order_id"].ToString();
        var status = success ? "success" : "failed";

        var parts = merchantOrderId.Split(':');
        var entityType = parts.Length >= 1 ? parts[0] : null;
        var entityId = parts.Length >= 3 ? parts[2] : null;

        var routes = new Dictionary<string, string>
        {
            ["payment"] = "/orders/{0}?payment={1}",
            ["deposit"] = "/wallet?deposit={1}",
        };

        if (entityType is not null && routes.TryGetValue(entityType, out var template))
        {
            var path = string.Format(template, entityId ?? "", status);
            return Redirect($"{_settings.FrontendBaseUrl}{path}");
        }

        return Redirect($"{_settings.FrontendBaseUrl}/orders?payment={status}");
    }
}
