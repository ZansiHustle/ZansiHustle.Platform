using System;

namespace ZansiHustle.Domain.Payments
{
    /// <summary>
    /// Raw webhook / provider event log. One row per inbound event, regardless
    /// of whether it was actionable or not. The unique <see cref="ProviderEventKey"/>
    /// is the primary idempotency barrier — two identical events from the provider
    /// can never produce two rows.
    /// </summary>
    public class PaymentEvent
    {
        public Guid Id { get; set; }

        /// <summary>Payment this event belongs to, if resolvable. Null for orphan events (unknown reference).</summary>
        public Guid? PaymentId { get; set; }
        public Payment? Payment { get; set; }

        public string Provider { get; set; } = string.Empty;

        /// <summary>
        /// Deduplication key. We compute it from the provider payload — typically
        /// <c>{eventType}:{reference}:{providerTransactionId}</c> — so webhook
        /// replays hit the unique index and are swallowed safely.
        /// </summary>
        public string ProviderEventKey { get; set; } = string.Empty;

        /// <summary>Event name as sent by the provider (e.g. "charge.success").</summary>
        public string EventType { get; set; } = string.Empty;

        /// <summary>Raw JSON payload — stored verbatim for audit and re-processing.</summary>
        public string RawPayload { get; set; } = string.Empty;

        /// <summary>The provider-supplied signature header we received.</summary>
        public string? SignatureHeader { get; set; }

        /// <summary>Result of HMAC signature verification at receipt time.</summary>
        public bool SignatureValid { get; set; }

        /// <summary>True once the event's side-effects have been applied successfully.</summary>
        public bool Processed { get; set; }

        /// <summary>Populated when <see cref="Processed"/> is false — diagnostic string for ops.</summary>
        public string? ProcessingError { get; set; }

        public DateTime ReceivedAtUtc { get; set; } = DateTime.UtcNow;
        public DateTime? ProcessedAtUtc { get; set; }
    }
}
