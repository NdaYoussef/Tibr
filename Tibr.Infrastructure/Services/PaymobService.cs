using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Tibr.Application.Dtos.Paymob;
using Tibr.Application.Services.PaymentServices;
using Tibr.Domain.Entities;
using Tibr.Domain.IRepositories;
using Tibr.Infrastructure.Config;

namespace Tibr.Infrastructure.Services
{
    public class PaymobService : IPaymobService
    {
        // In-memory mapping: PaymobOrderId → OurOrderId
        private static readonly ConcurrentDictionary<long, long> _paymobOrderMap = new();

        private readonly HttpClient _http;
        private readonly PaymobSettings _settings;
        private readonly IGenericRepository<Order, long> _orderRepository;
        private readonly IGenericRepository<Payment, long> _paymentRepository;
        private readonly ILogger<PaymobService> _logger;

        public PaymobService(
            HttpClient http,
            IOptions<PaymobSettings> settings,
            IGenericRepository<Order, long> orderRepository,
            IGenericRepository<Payment, long> paymentRepository,
            ILogger<PaymobService> logger
        )
        {
            _http = http;
            _settings = settings.Value;
            _orderRepository = orderRepository;
            _paymentRepository = paymentRepository;
            _logger = logger;

            _http.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Token", _settings.SecretKey);
        }

        // ─────────────────────────────────────────
        // PUBLIC: Single intention call → checkout URL
        // ─────────────────────────────────────────
        public async Task<string> CreatePaymentUrlAsync(CreatePaymentRequest request)
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
                    // Required by Paymob schema, not meaningful in sandbox
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
                special_reference = request.OrderId.ToString(),
            };

            var response = await PostJsonAsync($"{_settings.BaseUrl}/v1/intention/", body);

            // Save Paymob order ID → our Order ID mapping for the callback
            if (response.TryGetProperty("intention_order_id", out var paymobOrderIdProp))
            {
                var paymobOrderId = paymobOrderIdProp.GetInt64();
                _paymobOrderMap[paymobOrderId] = request.OrderId;
                _logger.LogInformation(
                    "Mapped PaymobOrderId={PaymobId} to OrderId={OrderId}",
                    paymobOrderId,
                    request.OrderId
                );
            }
            else
            {
                _logger.LogWarning("Could not extract intention_order_id from intention response");
            }

            var clientSecret =
                response.GetProperty("client_secret").GetString()
                ?? throw new Exception("Paymob: missing client_secret in intention response");

            // Unified checkout redirect URL
            return $"https://accept.paymob.com/unifiedcheckout/?publicKey={_settings.PublicKey}&clientSecret={clientSecret}";
        }

        // ─────────────────────────────────────────
        // PUBLIC: Verify HMAC callback
        // ─────────────────────────────────────────
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
        // PUBLIC: Process successful callback
        // ─────────────────────────────────────────
        public async Task ProcessCallbackAsync(PaymobCallbackPayload payload)
        {
            var transaction = payload.Obj;
            _logger.LogInformation(
                "Paymob callback received: Success={Success}, SpecialRef={Ref}, TxId={TxId}",
                transaction?.Success,
                transaction?.Order?.SpecialReference,
                transaction?.Id
            );

            if (transaction?.Success != true)
                return;

            // Try to look up our OrderId from the in-memory mapping using Paymob's order ID
            long orderId = 0;
            var paymobOrderId = transaction.Order?.Id;

            if (
                paymobOrderId.HasValue
                && _paymobOrderMap.TryGetValue(paymobOrderId.Value, out var mappedId)
            )
            {
                orderId = mappedId;
                _logger.LogInformation(
                    "Resolved OrderId={OrderId} from PaymobOrderId={PaymobId}",
                    orderId,
                    paymobOrderId.Value
                );
            }
            else
            {
                // Fallback: try special_reference
                var orderIdString = transaction.Order?.SpecialReference;
                if (!long.TryParse(orderIdString, out orderId))
                {
                    _logger.LogWarning(
                        "Paymob callback: no mapping for PaymobOrderId={PaymobId} and invalid special_reference={Ref}",
                        paymobOrderId,
                        orderIdString
                    );
                    return;
                }
            }

            var order = await _orderRepository.GetByIdAsync(orderId);
            if (order is null)
            {
                _logger.LogWarning("Paymob callback for non-existent OrderId={OrderId}", orderId);
                return;
            }

            order.PaymentStatus = "Paid";
            order.OrderStatus = "Processing";
            await _orderRepository.UpdateAsync(order);

            var payment = new Payment
            {
                Id = 0,
                OrderId = orderId,
                UserId = order.UserId,
                Amount = transaction.AmountCents / 100m,
                PaymentMethod = transaction.SourceData?.Type ?? "",
                Status = "Completed",
                PaidAt = DateTime.UtcNow,
            };

            await _paymentRepository.AddAsync(payment);

            await _orderRepository.SaveChangesAsync();

            _logger.LogInformation(
                "Payment recorded for OrderId={OrderId}, TxId={TxId}, Amount={Amount}",
                orderId,
                transaction.Id,
                payment.Amount
            );
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
