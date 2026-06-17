using System;
using System.Collections.Generic;
using ZansiHustle.Shared.Enums.Orders;
using ZansiHustle.Shared.Enums.ZansiDispatch;

namespace ZansiHustle.Application.Orders.Dtos
{
    /// <summary>
    /// Customer-facing order tracking for a PRODUCT order. Deliberately carries
    /// NO seller private contact/address fields — only the public shop display
    /// name and the buyer's own delivery destination. This is the response of
    /// <c>GET /api/orders/{id}/tracking</c>.
    /// </summary>
    public sealed class OrderTrackingDto
    {
        public Guid OrderId { get; set; }
        public string OrderCode { get; set; } = string.Empty;

        public OrderStatus CurrentOrderStatus { get; set; }
        public PaymentStatus PaymentStatus { get; set; }

        /// <summary>Dispatch lifecycle state. Null until a shipment exists.</summary>
        public ZansiDispatchShipmentStatus? DispatchStatus { get; set; }

        /// <summary>True once a ZansiDispatch shipment has been created for the order.</summary>
        public bool HasDispatch { get; set; }

        /// <summary>Provider label (e.g. "CourierGuy"). Null until dispatch starts.</summary>
        public string? TrackingProvider { get; set; }
        public string? TrackingNumber { get; set; }
        /// <summary>Customer-displayable tracking reference: <c>TrackingNumber</c> when
        /// present, otherwise the courier short reference (e.g. <c>7D67MD</c>). Use this
        /// for display so a null tracking number never hides a usable short ref.</summary>
        public string? TrackingReference { get; set; }
        /// <summary>Public tracking URL. Null — not surfaced to customers yet.</summary>
        public string? TrackingUrl { get; set; }

        public DateTime? EstimatedDeliveryUtc { get; set; }
        public DateTime? DeliveredAtUtc { get; set; }

        // ── Courier date promises (null until the provider returns them) ────
        /// <summary>Courier's expected collection date (date-only).</summary>
        public DateTime? ExpectedCollectionDate { get; set; }
        /// <summary>Start of the expected delivery window (date-only).</summary>
        public DateTime? ExpectedDeliveryFrom { get; set; }
        /// <summary>End of the expected delivery window (date-only). May equal
        /// <see cref="ExpectedDeliveryFrom"/> for a single-date promise.</summary>
        public DateTime? ExpectedDeliveryTo { get; set; }

        /// <summary>Shop/seller display name only — never seller phone/address.</summary>
        public string? SellerDisplayName { get; set; }

        /// <summary>The buyer's delivery destination summary (NOT the seller pickup address).</summary>
        public string? DestinationSummary { get; set; }

        public List<OrderTrackingTimelineItemDto> Timeline { get; set; } = new();

        /// <summary>
        /// Customer-safe lifecycle updates (reschedule / cancellation requests &amp;
        /// confirmations) shown beneath the fixed checkpoint timeline. Friendly
        /// labels only — never raw provider errors or admin notes. Latest last.
        /// </summary>
        public List<OrderTrackingUpdateDto> Updates { get; set; } = new();
    }

    /// <summary>One checkpoint in the customer tracking timeline.</summary>
    public sealed class OrderTrackingTimelineItemDto
    {
        public string Key { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
        /// <summary>One of: Pending | Current | Done | Failed.</summary>
        public string Status { get; set; } = "Pending";
        public DateTime? OccurredAtUtc { get; set; }
        public string? Description { get; set; }
    }

    /// <summary>A customer-safe lifecycle update (label + time).</summary>
    public sealed class OrderTrackingUpdateDto
    {
        public string Label { get; set; } = string.Empty;
        public DateTime OccurredAtUtc { get; set; }
    }
}
