using System;
using System.Collections.Generic;
using ZansiHustle.Domain.Orders;
using ZansiHustle.Shared.Enums.Payments;

namespace ZansiHustle.Domain.Payments
{
    /// <summary>
    /// A single payment attempt against an <see cref="Order"/>. One order may
    /// have many Payments (retry flow) but at most one <see cref="PaymentTransactionStatus.Succeeded"/> row.
    /// </summary>
    public class Payment
    {
        public Guid Id { get; set; }

        /// <summary>Internal reference (e.g. <c>PAY-20260415...</c>) — sent to the provider as their reference too.</summary>
        public string Code { get; set; } = string.Empty;

        public Guid OrderId { get; set; }
        public Order? Order { get; set; }

        /// <summary>Buyer user id. Copy of Order.BuyerUserId for fast indexing.</summary>
        public Guid UserId { get; set; }

        /// <summary>Provider name, e.g. "Paystack". Stored as string so new providers can be added without migrations.</summary>
        public string Provider { get; set; } = string.Empty;

        /// <summary>The provider's own reference id (typically mirrors our <see cref="Code"/>).</summary>
        public string? ProviderReference { get; set; }

        /// <summary>Checkout URL returned by the provider for redirect-based flows.</summary>
        public string? ProviderAuthorizationUrl { get; set; }

        /// <summary>Provider access code (Paystack-specific).</summary>
        public string? ProviderAccessCode { get; set; }

        public decimal Amount { get; set; }
        public string Currency { get; set; } = "ZAR";

        public PaymentTransactionStatus Status { get; set; } = PaymentTransactionStatus.Initialized;

        public DateTime? PaidAtUtc { get; set; }
        public DateTime? FailedAtUtc { get; set; }
        public DateTime? CancelledAtUtc { get; set; }
        public DateTime? RefundedAtUtc { get; set; }

        /// <summary>Free-form reason when <see cref="Status"/> is Failed or Cancelled.</summary>
        public string? FailureReason { get; set; }

        /// <summary>Channel the buyer used (e.g. "card", "eft") — populated from provider response.</summary>
        public string? ChannelUsed { get; set; }

        /// <summary>Last raw provider response JSON for debugging/audit.</summary>
        public string? RawProviderMetadata { get; set; }

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAtUtc { get; set; }

        public List<PaymentEvent> Events { get; set; } = new();
    }
}
