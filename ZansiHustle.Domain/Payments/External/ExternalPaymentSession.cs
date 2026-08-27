using System;
using ZansiHustle.Shared.Enums.Payments;

namespace ZansiHustle.Domain.Payments.External
{
    /// <summary>
    /// A payment ZansiHustle is processing on behalf of a registered external
    /// shop (e.g. ZansiTech) via the shared Ozow integration. Deliberately a
    /// standalone entity rather than a reuse of <see cref="Payment"/>: Payment
    /// requires a non-nullable <see cref="Payment.OrderId"/> pointing at a
    /// ZansiHustle <c>Order</c>, which does not exist for an external shop's
    /// own order. Provider-tracking fields therefore mirror Payment's shape
    /// (Provider/ProviderReference/ProviderAuthorizationUrl/ProviderAccessCode)
    /// rather than composing it.
    /// </summary>
    public class ExternalPaymentSession
    {
        public Guid Id { get; set; }

        /// <summary>Registered shop identity, e.g. "zansitech". Always the configured lookup key, never trusted verbatim from the request.</summary>
        public string ShopCode { get; set; } = string.Empty;

        /// <summary>Display name — taken from the shop's registered configuration, not the caller-supplied value.</summary>
        public string ShopName { get; set; } = string.Empty;

        /// <summary>The external shop's own order id (their Guid/string), opaque to ZansiHustle.</summary>
        public string ExternalOrderId { get; set; } = string.Empty;

        /// <summary>Human-readable order number from the external shop.</summary>
        public string ExternalOrderNumber { get; set; } = string.Empty;

        public decimal Amount { get; set; }
        public string Currency { get; set; } = "ZAR";
        public string? CustomerEmail { get; set; }

        /// <summary>Validated against the shop's AllowedReturnHosts at create time — never re-validated from a request later.</summary>
        public string ReturnUrl { get; set; } = string.Empty;

        /// <summary>Validated against the shop's AllowedCallbackHosts at create time.</summary>
        public string CallbackUrl { get; set; } = string.Empty;

        /// <summary>Always "Ozow" today — string (not enum) so a future provider needs no migration, mirroring <see cref="Payment.Provider"/>.</summary>
        public string Provider { get; set; } = string.Empty;

        /// <summary>Our own generated Ozow TransactionReference (e.g. "EXTPAY_...") — echoed back on the webhook.</summary>
        public string? ProviderReference { get; set; }

        /// <summary>Ozow's redirect URL — returned to the shop as-is on idempotent replay.</summary>
        public string? ProviderAuthorizationUrl { get; set; }

        /// <summary>Ozow's paymentRequestId.</summary>
        public string? ProviderAccessCode { get; set; }

        public ExternalPaymentSessionStatus Status { get; set; } = ExternalPaymentSessionStatus.Pending;

        /// <summary>Free-form reason when Status is Failed/Cancelled/Expired.</summary>
        public string? FailureReason { get; set; }

        /// <summary>True when created under Ozow:UatTestMode — the charged Amount may have been capped. See OzowSettings.UatTestMode.</summary>
        public bool IsTest { get; set; }

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAtUtc { get; set; }
        public DateTime? PaidAtUtc { get; set; }

        /// <summary>Set once the signed callback to the shop's CallbackUrl succeeds (2xx). Null means still pending/never delivered.</summary>
        public DateTime? CallbackDeliveredAtUtc { get; set; }

        /// <summary>Incremented on every delivery attempt (initial + opportunistic retries triggered by status polling).</summary>
        public int CallbackAttemptCount { get; set; }

        /// <summary>Diagnostic — last delivery failure reason, cleared once delivery succeeds.</summary>
        public string? LastCallbackError { get; set; }
    }
}
