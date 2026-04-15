using System.Text.Json.Serialization;

namespace ZansiHustle.Application.Payments.Providers
{
    /// <summary>
    /// Low-level Paystack request/response contracts. Field names use the
    /// casing Paystack expects on the wire (lowercase snake_case).
    /// </summary>

    public sealed class PaystackInitializeRequest
    {
        [JsonPropertyName("email")]
        public string Email { get; set; } = string.Empty;

        /// <summary>Amount in the currency's subunit (ZAR → cents).</summary>
        [JsonPropertyName("amount")]
        public long Amount { get; set; }

        [JsonPropertyName("currency")]
        public string Currency { get; set; } = "ZAR";

        [JsonPropertyName("reference")]
        public string Reference { get; set; } = string.Empty;

        [JsonPropertyName("callback_url")]
        public string? CallbackUrl { get; set; }

        [JsonPropertyName("metadata")]
        public object? Metadata { get; set; }
    }

    public sealed class PaystackInitializeResponse
    {
        [JsonPropertyName("status")]
        public bool Status { get; set; }

        [JsonPropertyName("message")]
        public string? Message { get; set; }

        [JsonPropertyName("data")]
        public PaystackInitializeData? Data { get; set; }
    }

    public sealed class PaystackInitializeData
    {
        [JsonPropertyName("authorization_url")]
        public string? AuthorizationUrl { get; set; }

        [JsonPropertyName("access_code")]
        public string? AccessCode { get; set; }

        [JsonPropertyName("reference")]
        public string? Reference { get; set; }
    }

    public sealed class PaystackVerifyResponse
    {
        [JsonPropertyName("status")]
        public bool Status { get; set; }

        [JsonPropertyName("message")]
        public string? Message { get; set; }

        [JsonPropertyName("data")]
        public PaystackVerifyData? Data { get; set; }
    }

    public sealed class PaystackVerifyData
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("reference")]
        public string? Reference { get; set; }

        /// <summary>"success", "failed", "abandoned", etc.</summary>
        [JsonPropertyName("status")]
        public string? Status { get; set; }

        /// <summary>Amount actually charged, in subunit (cents).</summary>
        [JsonPropertyName("amount")]
        public long Amount { get; set; }

        [JsonPropertyName("currency")]
        public string? Currency { get; set; }

        [JsonPropertyName("channel")]
        public string? Channel { get; set; }

        [JsonPropertyName("paid_at")]
        public string? PaidAt { get; set; }

        [JsonPropertyName("gateway_response")]
        public string? GatewayResponse { get; set; }
    }

    /// <summary>
    /// Minimal shape of the webhook JSON body. We parse just enough to dedup +
    /// route the event; the full payload is persisted verbatim on
    /// <see cref="Domain.Payments.PaymentEvent.RawPayload"/>.
    /// </summary>
    public sealed class PaystackWebhookPayload
    {
        [JsonPropertyName("event")]
        public string? Event { get; set; }

        [JsonPropertyName("data")]
        public PaystackWebhookData? Data { get; set; }
    }

    public sealed class PaystackWebhookData
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("reference")]
        public string? Reference { get; set; }

        [JsonPropertyName("status")]
        public string? Status { get; set; }

        [JsonPropertyName("amount")]
        public long Amount { get; set; }

        [JsonPropertyName("currency")]
        public string? Currency { get; set; }

        [JsonPropertyName("channel")]
        public string? Channel { get; set; }

        [JsonPropertyName("paid_at")]
        public string? PaidAt { get; set; }

        [JsonPropertyName("gateway_response")]
        public string? GatewayResponse { get; set; }
    }
}
