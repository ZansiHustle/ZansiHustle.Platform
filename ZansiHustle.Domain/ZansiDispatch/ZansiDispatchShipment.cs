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

        // ── Courier date promises + parcel facts (from provider) ────────────
        // Captured from the booking/tracking response when the provider returns
        // them; NEVER faked. Date-only promises stored at UTC midnight; display
        // in Africa/Johannesburg. Delivery is often a RANGE (from–to).
        /// <summary>Courier's expected collection date (date-only; UTC midnight).</summary>
        public DateTime? ExpectedCollectionDate { get; set; }
        /// <summary>Start of the courier's expected delivery window (date-only).</summary>
        public DateTime? ExpectedDeliveryFrom { get; set; }
        /// <summary>End of the courier's expected delivery window (date-only). Equals
        /// <see cref="ExpectedDeliveryFrom"/> when the provider gives a single date.</summary>
        public DateTime? ExpectedDeliveryTo { get; set; }

        /// <summary>Provider's charged (billable) weight in kg, if returned.</summary>
        public decimal? ChargedWeightKg { get; set; }
        /// <summary>Provider's measured actual weight in kg, if returned.</summary>
        public decimal? ActualWeightKg { get; set; }
        /// <summary>Provider's volumetric weight in kg, if returned.</summary>
        public decimal? VolumetricWeightKg { get; set; }
        /// <summary>Provider's base rate (before adjustments), if returned.</summary>
        public decimal? BaseRate { get; set; }

        /// <summary>Latest human-readable provider status message
        /// (e.g. "A driver has been allocated to collect the shipment.").</summary>
        public string? ProviderStatusMessage { get; set; }
        /// <summary>Provider package/waybill tracking reference (e.g. FP9GWK/1), if distinct.</summary>
        public string? PackageTrackingReference { get; set; }

        /// <summary>Set once the "shipment booked" customer+seller emails have been
        /// sent — idempotency guard so an idempotent retry never re-sends them.</summary>
        public DateTime? ShipmentBookedEmailSentAtUtc { get; set; }

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
