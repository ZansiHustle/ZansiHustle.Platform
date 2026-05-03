using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace ZansiHustle.Application.Payments.Providers
{
    // Low-level Yoco Online Payments request/response contracts. Field names
    // use the camelCase convention Yoco's API expects on JSON bodies.

    // ─── Create checkout (POST /checkouts) ───────────────────────────────────

    public sealed class YocoCheckoutRequest
    {
        /// <summary>Amount in the currency's minor unit (ZAR → cents).</summary>
        [JsonPropertyName("amount")]
        public long Amount { get; set; }

        [JsonPropertyName("currency")]
        public string Currency { get; set; } = "ZAR";

        [JsonPropertyName("cancelUrl")]
        public string? CancelUrl { get; set; }

        [JsonPropertyName("successUrl")]
        public string? SuccessUrl { get; set; }

        [JsonPropertyName("failureUrl")]
        public string? FailureUrl { get; set; }

        /// <summary>
        /// Free-form key/value pairs Yoco will echo back on the webhook
        /// payload's <c>metadata</c>. We use this to carry our own Payment
        /// Code so reconciliation is trivial.
        /// </summary>
        [JsonPropertyName("metadata")]
        public Dictionary<string, string>? Metadata { get; set; }
    }

    public sealed class YocoCheckoutResponse
    {
        /// <summary>Yoco checkout id, e.g. <c>ch_...</c>.</summary>
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        /// <summary>Hosted-checkout URL the buyer is redirected to.</summary>
        [JsonPropertyName("redirectUrl")]
        public string? RedirectUrl { get; set; }

        [JsonPropertyName("amount")]
        public long Amount { get; set; }

        [JsonPropertyName("currency")]
        public string? Currency { get; set; }

        /// <summary>"created" — checkout is ready for the buyer.</summary>
        [JsonPropertyName("status")]
        public string? Status { get; set; }

        /// <summary>"pending" | "succeeded" | "failed" | "canceled".</summary>
        [JsonPropertyName("paymentStatus")]
        public string? PaymentStatus { get; set; }

        [JsonPropertyName("metadata")]
        public Dictionary<string, string>? Metadata { get; set; }

        [JsonPropertyName("paymentId")]
        public string? PaymentId { get; set; }
    }

    // ─── Webhook payload ─────────────────────────────────────────────────────

    /// <summary>
    /// Top-level shape of an inbound Yoco webhook event. Yoco emits one of
    /// <c>payment.succeeded</c>, <c>payment.failed</c>, or
    /// <c>payment.canceled</c> for our flows; the controller binds via
    /// <see cref="System.Text.Json.JsonSerializer"/> from the raw body
    /// (the same body must be re-used for HMAC verification, so we never
    /// re-serialize it before verifying).
    /// </summary>
    public sealed class YocoWebhookEvent
    {
        /// <summary>Event id, e.g. <c>evt_...</c>. Used for webhook dedup.</summary>
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        /// <summary>e.g. <c>payment.succeeded</c>.</summary>
        [JsonPropertyName("type")]
        public string? Type { get; set; }

        [JsonPropertyName("createdDate")]
        public string? CreatedDate { get; set; }

        [JsonPropertyName("payload")]
        public YocoWebhookPayload? Payload { get; set; }
    }

    public sealed class YocoWebhookPayload
    {
        /// <summary>Payment id, e.g. <c>p_...</c>.</summary>
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("type")]
        public string? Type { get; set; }

        /// <summary>"succeeded" | "failed" | "canceled" | "pending".</summary>
        [JsonPropertyName("status")]
        public string? Status { get; set; }

        [JsonPropertyName("amount")]
        public long Amount { get; set; }

        [JsonPropertyName("currency")]
        public string? Currency { get; set; }

        [JsonPropertyName("createdDate")]
        public string? CreatedDate { get; set; }

        [JsonPropertyName("metadata")]
        public Dictionary<string, string>? Metadata { get; set; }

        [JsonPropertyName("paymentMethodDetails")]
        public YocoPaymentMethodDetails? PaymentMethodDetails { get; set; }

        [JsonPropertyName("checkoutId")]
        public string? CheckoutId { get; set; }
    }

    public sealed class YocoPaymentMethodDetails
    {
        /// <summary>"card", "applepay", "googlepay", etc.</summary>
        [JsonPropertyName("type")]
        public string? Type { get; set; }
    }
}
