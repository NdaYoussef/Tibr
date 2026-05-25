using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Tibr.Application.Dtos.Paymob;
using Tibr.Application.Services;
using Tibr.Infrastructure.Config;

namespace Tibr.Infrastructure.Services
{
    public class PaymobService : IPaymobService
    {
        private readonly HttpClient _http;
        private readonly PaymobSettings _settings;

        public PaymobService(HttpClient http, IOptions<PaymobSettings> settings)
        {
            _http = http;
            _settings = settings.Value;
        }

        // ─────────────────────────────────────────
        // STEP 1 — Get auth token
        // ─────────────────────────────────────────
        private async Task<string> GetAuthTokenAsync()
        {
            var body = new { api_key = _settings.ApiKey };
            var response = await PostJsonAsync($"{_settings.BaseUrl}/auth/tokens", body);

            return response.GetProperty("token").GetString()
                ?? throw new Exception("Paymob: failed to get auth token");
        }

        // ─────────────────────────────────────────
        // STEP 2 — Create order
        // ─────────────────────────────────────────
        private async Task<long> CreateOrderAsync(string token, int amountCents, string currency)
        {
            var body = new
            {
                auth_token = token,
                delivery_needed = false,
                amount_cents = amountCents,
                currency,
                items = Array.Empty<object>(),
            };

            var response = await PostJsonAsync($"{_settings.BaseUrl}/ecommerce/orders", body);

            return response.GetProperty("id").GetInt64();
        }

        // ─────────────────────────────────────────
        // STEP 3 — Get payment key
        // ─────────────────────────────────────────
        private async Task<string> GetPaymentKeyAsync(
            string token,
            long orderId,
            int amountCents,
            string currency,
            CreatePaymentRequest request
        )
        {
            var body = new
            {
                auth_token = token,
                amount_cents = amountCents,
                expiration = 3600,
                order_id = orderId,
                billing_data = new
                {
                    first_name = request.FirstName,
                    last_name = request.LastName,
                    email = request.Email,
                    phone_number = request.Phone,
                    // These are required by Paymob but irrelevant for sandbox
                    apartment = "NA",
                    floor = "NA",
                    street = "NA",
                    building = "NA",
                    shipping_method = "NA",
                    postal_code = "NA",
                    city = "NA",
                    country = "EG",
                    state = "NA",
                },
                currency,
                integration_id = int.Parse(_settings.IntegrationId),
            };

            var response = await PostJsonAsync(
                $"{_settings.BaseUrl}/acceptance/payment_keys",
                body
            );

            return response.GetProperty("token").GetString()
                ?? throw new Exception("Paymob: failed to get payment key");
        }

        public async Task<string> CreatePaymentUrlAsync(CreatePaymentRequest request)
        {
            var token = await GetAuthTokenAsync();
            var orderId = await CreateOrderAsync(token, request.AmountCents, request.Currency);
            var paymentKey = await GetPaymentKeyAsync(
                token,
                orderId,
                request.AmountCents,
                request.Currency,
                request
            );

            return $"https://accept.paymob.com/api/acceptance/iframes/{_settings.IframeId}?payment_token={paymentKey}";
        }

        public bool VerifyCallback(PaymobCallbackPayload payload, string receivedHmac)
        {
            var t = payload.Obj;
            if (t is null)
                return false;

            // Paymob requires these fields concatenated in this exact order
            var data = string.Concat(
                t.AmountCents,
                t.CreatedAt,
                t.Currency,
                t.ErrorOccured.ToString().ToLower(),
                t.HasParentTransaction.ToString().ToLower(),
                t.Id,
                t.IntegrationId,
                t.Is3dSecure.ToString().ToLower(),
                t.IsAuth.ToString().ToLower(),
                t.IsCapture.ToString().ToLower(),
                t.IsRefunded.ToString().ToLower(),
                t.IsStandalonePayment.ToString().ToLower(),
                t.IsVoided.ToString().ToLower(),
                t.Order?.Id,
                t.Owner,
                t.Pending.ToString().ToLower(),
                t.SourceData?.Pan,
                t.SourceData?.SubType,
                t.SourceData?.Type,
                t.Success.ToString().ToLower()
            );

            using var hmac = new HMACSHA512(Encoding.UTF8.GetBytes(_settings.HmacSecret));
            var computed = Convert
                .ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(data)))
                .ToLower();

            return computed == receivedHmac.ToLower();
        }

        // ─────────────────────────────────────────
        // Helper
        // ─────────────────────────────────────────
        private async Task<JsonElement> PostJsonAsync(string url, object body)
        {
            var json = JsonSerializer.Serialize(body);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _http.PostAsync(url, content);

            response.EnsureSuccessStatusCode();

            var responseBody = await response.Content.ReadAsStringAsync();
            return JsonDocument.Parse(responseBody).RootElement;
        }
    }
}
