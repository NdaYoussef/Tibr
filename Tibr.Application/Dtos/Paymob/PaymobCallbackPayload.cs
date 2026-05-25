using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Tibr.Application.Dtos.Paymob
{
    public class PaymobCallbackPayload
    {
        [JsonPropertyName("obj")]
        public TransactionObject? Obj { get; set; }

        [JsonPropertyName("type")]
        public string? Type { get; set; }
    }

    public class TransactionObject
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("pending")]
        public bool Pending { get; set; }

        [JsonPropertyName("amount_cents")]
        public int AmountCents { get; set; }

        [JsonPropertyName("currency")]
        public string Currency { get; set; } = string.Empty;

        [JsonPropertyName("created_at")]
        public string CreatedAt { get; set; } = string.Empty;

        [JsonPropertyName("error_occured")]
        public bool ErrorOccured { get; set; }

        [JsonPropertyName("has_parent_transaction")]
        public bool HasParentTransaction { get; set; }

        [JsonPropertyName("integration_id")]
        public int IntegrationId { get; set; }

        [JsonPropertyName("is_3d_secure")]
        public bool Is3dSecure { get; set; }

        [JsonPropertyName("is_auth")]
        public bool IsAuth { get; set; }

        [JsonPropertyName("is_capture")]
        public bool IsCapture { get; set; }

        [JsonPropertyName("is_refunded")]
        public bool IsRefunded { get; set; }

        [JsonPropertyName("is_standalone_payment")]
        public bool IsStandalonePayment { get; set; }

        [JsonPropertyName("is_voided")]
        public bool IsVoided { get; set; }

        [JsonPropertyName("owner")]
        public int Owner { get; set; }

        [JsonPropertyName("order")]
        public PaymobOrder? Order { get; set; }

        [JsonPropertyName("source_data")]
        public SourceData? SourceData { get; set; }
    }

    public class PaymobOrder
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }
    }

    public class SourceData
    {
        [JsonPropertyName("pan")]
        public string Pan { get; set; } = string.Empty;

        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;

        [JsonPropertyName("sub_type")]
        public string SubType { get; set; } = string.Empty;
    }
}
