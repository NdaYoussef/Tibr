using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Tibr.Application.Dtos.Payment;
using Tibr.Application.Services.PaymentServices;
using Tibr.Infrastructure.Config;

namespace Tibr.Infrastructure.Services;

public class PaymobPaymentGateway : IPaymentGateway
{
    private readonly HttpClient _http;
    private readonly PaymobSettings _settings;
    private readonly ILogger<PaymobPaymentGateway> _logger;

    public PaymobPaymentGateway(
        HttpClient http,
        IOptions<PaymobSettings> settings,
        ILogger<PaymobPaymentGateway> logger)
    {
        _http = http;
        _settings = settings.Value;
        _logger = logger;

        _http.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Token", _settings.SecretKey);
    }

    public async Task<PaymentInitiationResult> CreateIntentionAsync(PaymentIntentionRequest request)
    {
        var body = new
        {
            amount = request.AmountCents,
            currency = request.Currency,
            payment_methods = new[] { int.Parse(_settings.IntegrationId) },
            items = Array.Empty<object>(),
            billing_data = new
            {
                first_name = request.FirstName,
                last_name = request.LastName,
                email = request.Email,
                phone_number = request.Phone,
                apartment = "NA",
                floor = "NA",
                street = "NA",
                building = "NA",
                postal_code = "NA",
                city = "NA",
                country = "EGY",
                state = "NA",
            },
            customer = new
            {
                first_name = request.FirstName,
                last_name = request.LastName,
                email = request.Email,
            },
            special_reference = request.SpecialReference,
        };

        try
        {
            var responseJson = await PostJsonAsync($"{_settings.BaseUrl}/v1/intention/", body);

            var clientSecret = responseJson.GetProperty("client_secret").GetString();
            if (string.IsNullOrEmpty(clientSecret))
                return PaymentInitiationResult.Failure("Paymob: missing client_secret in response");

            var checkoutUrl = $"https://accept.paymob.com/unifiedcheckout/?publicKey={_settings.PublicKey}&clientSecret={clientSecret}";

            return PaymentInitiationResult.Success(checkoutUrl);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Paymob intention creation failed for ref={Ref}", request.SpecialReference);
            return PaymentInitiationResult.Failure("Payment provider request failed.");
        }
    }

    public bool VerifyWebhook(string rawBody, string signature)
    {
        PaymobTransaction? transaction;
        try
        {
            var payload = JsonSerializer.Deserialize<PaymobWebhookPayload>(rawBody);
            transaction = payload?.Obj;
        }
        catch
        {
            return false;
        }

        if (transaction is null)
            return false;

        var data = string.Concat(
            transaction.AmountCents,
            transaction.CreatedAt,
            transaction.Currency,
            transaction.ErrorOccured.ToString().ToLower(),
            transaction.HasParentTransaction.ToString().ToLower(),
            transaction.Id,
            transaction.IntegrationId,
            transaction.Is3dSecure.ToString().ToLower(),
            transaction.IsAuth.ToString().ToLower(),
            transaction.IsCapture.ToString().ToLower(),
            transaction.IsRefunded.ToString().ToLower(),
            transaction.IsStandalonePayment.ToString().ToLower(),
            transaction.IsVoided.ToString().ToLower(),
            transaction.Order?.Id,
            transaction.Owner,
            transaction.Pending.ToString().ToLower(),
            transaction.SourceData?.Pan,
            transaction.SourceData?.SubType,
            transaction.SourceData?.Type,
            transaction.Success.ToString().ToLower()
        );

        using var hmac = new HMACSHA512(Encoding.UTF8.GetBytes(_settings.HmacSecret));
        var computed = Convert
            .ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(data)))
            .ToLower();

        return computed == signature.ToLower();
    }

    public PaymentWebhookData ExtractWebhookData(string rawBody)
    {
        var payload = JsonSerializer.Deserialize<PaymobWebhookPayload>(rawBody);
        var t = payload?.Obj;

        return new PaymentWebhookData
        {
            TransactionId = t?.Id.ToString() ?? "",
            Success = t?.Success ?? false,
            AmountCents = t?.AmountCents ?? 0,
            Currency = t?.Currency ?? "",
            SpecialReference = t?.Order?.SpecialReference ?? "",
            PaymentMethod = t?.SourceData?.Type ?? "",
        };
    }

    private async Task<JsonElement> PostJsonAsync(string url, object body)
    {
        var json = JsonSerializer.Serialize(body);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var response = await _http.PostAsync(url, content);
        response.EnsureSuccessStatusCode();
        var bodyStr = await response.Content.ReadAsStringAsync();
        return JsonDocument.Parse(bodyStr).RootElement;
    }

    // ─── Paymob-specific DTOs (internal) ───

    private class PaymobWebhookPayload
    {
        [JsonPropertyName("obj")]
        public PaymobTransaction? Obj { get; set; }

        [JsonPropertyName("type")]
        public string? Type { get; set; }
    }

    private class PaymobTransaction
    {
        [JsonPropertyName("id")] public long Id { get; set; }
        [JsonPropertyName("success")] public bool Success { get; set; }
        [JsonPropertyName("pending")] public bool Pending { get; set; }
        [JsonPropertyName("amount_cents")] public int AmountCents { get; set; }
        [JsonPropertyName("currency")] public string Currency { get; set; } = string.Empty;
        [JsonPropertyName("created_at")] public string CreatedAt { get; set; } = string.Empty;
        [JsonPropertyName("error_occured")] public bool ErrorOccured { get; set; }
        [JsonPropertyName("has_parent_transaction")] public bool HasParentTransaction { get; set; }
        [JsonPropertyName("integration_id")] public int IntegrationId { get; set; }
        [JsonPropertyName("is_3d_secure")] public bool Is3dSecure { get; set; }
        [JsonPropertyName("is_auth")] public bool IsAuth { get; set; }
        [JsonPropertyName("is_capture")] public bool IsCapture { get; set; }
        [JsonPropertyName("is_refunded")] public bool IsRefunded { get; set; }
        [JsonPropertyName("is_standalone_payment")] public bool IsStandalonePayment { get; set; }
        [JsonPropertyName("is_voided")] public bool IsVoided { get; set; }
        [JsonPropertyName("owner")] public int Owner { get; set; }
        [JsonPropertyName("order")] public PaymobWebhookOrder? Order { get; set; }
        [JsonPropertyName("source_data")] public PaymobSourceData? SourceData { get; set; }
    }

    private class PaymobWebhookOrder
    {
        [JsonPropertyName("id")] public long Id { get; set; }
        [JsonPropertyName("merchant_order_id")] public string? SpecialReference { get; set; }
    }

    private class PaymobSourceData
    {
        [JsonPropertyName("pan")] public string Pan { get; set; } = string.Empty;
        [JsonPropertyName("type")] public string Type { get; set; } = string.Empty;
        [JsonPropertyName("sub_type")] public string SubType { get; set; } = string.Empty;
    }
}
