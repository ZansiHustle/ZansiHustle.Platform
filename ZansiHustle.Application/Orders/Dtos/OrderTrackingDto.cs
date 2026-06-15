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
        /// <summary>Public tracking URL. Null — not surfaced to customers yet.</summary>
        public string? TrackingUrl { get; set; }

        public DateTime? EstimatedDeliveryUtc { get; set; }
        public DateTime? DeliveredAtUtc { get; set; }

        /// <summary>Shop/seller display name only — never seller phone/address.</summary>
        public string? SellerDisplayName { get; set; }

        /// <summary>The buyer's delivery destination summary (NOT the seller pickup address).</summary>
        public string? DestinationSummary { get; set; }

        public List<OrderTrackingTimelineItemDto> Timeline { get; set; } = new();
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
}
