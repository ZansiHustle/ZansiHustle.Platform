using System;
using ZansiHustle.Shared.Enums.ZansiDispatch;

namespace ZansiHustle.Domain.ZansiDispatch
{
    /// <summary>
    /// The delivery lifecycle of an order after creation/payment. Created from
    /// the buyer's selected quote option; carries the quoted delivery fee and,
    /// once ops captures it, the actual courier cost + surplus/deficit/net for
    /// reconciliation. Reference ids are loose Guids. All timestamps are UTC.
    /// </summary>
    public class ZansiDispatchShipment
    {
        public Guid Id { get; set; }

        public Guid OrderId { get; set; }
        public Guid? QuoteId { get; set; }
        public Guid? QuoteOptionId { get; set; }

        public Guid UserId { get; set; }
        /// <summary>Seller — a <c>Merchant</c> id.</summary>
        public Guid? MerchantId { get; set; }
        public Guid? ShopId { get; set; }

        public ZansiDispatchProviderType ProviderType { get; set; }
        public ZansiDispatchServiceLevel ServiceLevel { get; set; }
        public string? ServiceLevelCode { get; set; }
        public string? ServiceLevelName { get; set; }

        /// <summary>What the buyer was charged for delivery (from the selected option).</summary>
        public decimal QuotedDeliveryFee { get; set; }
        /// <summary>What the courier actually charged. Null until ops captures it.</summary>
        public decimal? ActualCourierCost { get; set; }

        /// <summary>Positive margin when quoted &gt; actual. 0 otherwise.</summary>
        public decimal SurplusAmount { get; set; }
        /// <summary>Shortfall when actual &gt; quoted. 0 otherwise.</summary>
        public decimal DeficitAmount { get; set; }
        /// <summary>Quoted − actual (signed). 0 until actual cost captured.</summary>
        public decimal NetAmount { get; set; }

        public ZansiDispatchShipmentStatus Status { get; set; } = ZansiDispatchShipmentStatus.PendingDispatch;
        public ZansiDispatchReconciliationStatus ReconciliationStatus { get; set; } = ZansiDispatchReconciliationStatus.Pending;

        /// <summary>Provider's numeric/string shipment id (used by the label endpoint).</summary>
        public string? ProviderShipmentId { get; set; }
        public string? ProviderShipmentReference { get; set; }
        public string? TrackingNumber { get; set; }
        /// <summary>Courier short tracking reference (used to match webhook events).</summary>
        public string? ShortTrackingReference { get; set; }
        public string? CourierReference { get; set; }

        public string? PickupAddressSummary { get; set; }
        public string? DropoffAddressSummary { get; set; }

        public DateTime? PickupScheduledAt { get; set; }
        public DateTime? DeliveredAt { get; set; }

        /// <summary>Signed label/waybill PDF URL (expires ~24h — see <see cref="LabelUrlExpiresAt"/>).</summary>
        public string? LabelUrl { get; set; }
        public DateTime? LabelUrlExpiresAt { get; set; }

        public string? Notes { get; set; }
        /// <summary>Latest raw provider response (booking/tracking) for debugging. Never returned to buyers.</summary>
        public string? RawProviderResponseJson { get; set; }

        // ── Booking-attempt / failure tracking (auto-book + retry) ──────────
        /// <summary>
        /// Why the most recent courier-booking attempt failed (or was blocked).
        /// Set when <see cref="Status"/> is <c>NeedsAttention</c>; cleared on a
        /// successful booking. Provider-safe message (no secrets) — surfaced in
        /// the ops "Needs attention" queue.
        /// </summary>
        public string? FailureReason { get; set; }
        /// <summary>UTC time of the last courier-booking attempt (auto or retry).</summary>
        public DateTime? LastBookingAttemptAtUtc { get; set; }
        /// <summary>How many times a courier booking has been attempted for this shipment.</summary>
        public int BookingAttemptCount { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
