using System;
using System.Collections.Generic;
using ZansiHustle.Shared.Enums.ZansiDispatch;

namespace ZansiHustle.Application.ZansiDispatch.Dtos
{
    /// <summary>A pickup/delivery contact (Courier Guy needs email and/or mobile).</summary>
    public sealed class DispatchContactDto
    {
        public string? Name { get; set; }
        public string? MobileNumber { get; set; }
        public string? Email { get; set; }
    }

    /// <summary>Body for <c>POST /api/zansidispatch/shipments/create-from-quote</c>.</summary>
    public sealed class CreateShipmentFromQuoteRequestDto
    {
        public Guid OrderId { get; set; }
        public Guid QuoteOptionId { get; set; }

        public DispatchContactDto CollectionContact { get; set; } = new();
        public DispatchContactDto DeliveryContact { get; set; } = new();

        public string? CustomerReference { get; set; }
        public string? CustomerReferenceName { get; set; } = "Order no.";
        public string? SpecialInstructionsCollection { get; set; }
        public string? SpecialInstructionsDelivery { get; set; }
        public bool MuteNotifications { get; set; }
    }

    /// <summary>One tracking event surfaced to ops.</summary>
    public sealed class ShipmentEventDto
    {
        public string ProviderStatus { get; set; } = string.Empty;
        public ZansiDispatchShipmentStatus InternalStatus { get; set; }
        public string? Message { get; set; }
        public string? Location { get; set; }
        public DateTime EventTime { get; set; }
    }

    /// <summary>Result of <c>GET /api/zansidispatch/shipments/{id}/track</c>.</summary>
    public sealed class TrackingResultDto
    {
        public Guid ShipmentId { get; set; }
        public ZansiDispatchShipmentStatus Status { get; set; }
        public ZansiDispatchReconciliationStatus ReconciliationStatus { get; set; }
        public string? TrackingNumber { get; set; }
        public DateTime? DeliveredAt { get; set; }
        public List<ShipmentEventDto> Events { get; set; } = new();
    }

    /// <summary>Result of <c>GET /api/zansidispatch/shipments/{id}/label</c>.</summary>
    public sealed class ShipmentLabelDto
    {
        public Guid ShipmentId { get; set; }
        public string LabelUrl { get; set; } = string.Empty;
        public DateTime? ExpiresAt { get; set; }
    }

    /// <summary>Optional body for the cancel endpoint.</summary>
    public sealed class CancelShipmentRequestDto
    {
        public string? Reason { get; set; }
    }

    /// <summary>Lightweight ack for the webhook endpoint.</summary>
    public sealed class WebhookAckDto
    {
        public bool Received { get; set; }
        public bool ShipmentMatched { get; set; }
        public int EventsRecorded { get; set; }
    }

    /// <summary>
    /// CUSTOMER-SAFE dispatch snapshot for an order, read from STORED shipment
    /// state + events (no live provider poll, no reconciliation/cost fields, no
    /// seller pickup address, no raw provider payload). Consumed by the order
    /// tracking endpoint to build the buyer timeline. <see cref="HasShipment"/>
    /// is false when dispatch hasn't started yet (all other fields default).
    /// </summary>
    public sealed class OrderDispatchSnapshotDto
    {
        public bool HasShipment { get; set; }
        public ZansiDispatchShipmentStatus? Status { get; set; }
        /// <summary>Provider label (e.g. "CourierGuy"). Null until dispatch starts.</summary>
        public string? TrackingProvider { get; set; }
        public string? TrackingNumber { get; set; }
        /// <summary>Customer-displayable tracking reference: full tracking number when
        /// present, else the courier short reference (e.g. <c>7D67MD</c>).</summary>
        public string? TrackingReference { get; set; }
        public DateTime? DeliveredAt { get; set; }

        // ── Courier date promises (customer-safe; null until provider returns them) ──
        public DateTime? ExpectedCollectionDate { get; set; }
        public DateTime? ExpectedDeliveryFrom { get; set; }
        public DateTime? ExpectedDeliveryTo { get; set; }
        /// <summary>Latest human-readable provider status message (customer-safe).</summary>
        public string? ProviderStatusMessage { get; set; }

        public List<OrderDispatchEventDto> Events { get; set; } = new();
        /// <summary>
        /// Customer-safe lifecycle updates derived from the shipment action log
        /// (reschedule/cancellation requests etc.). Friendly labels only — never
        /// raw provider errors, reasons, or admin notes.
        /// </summary>
        public List<OrderDispatchCustomerUpdateDto> Updates { get; set; } = new();
    }

    /// <summary>A single customer-safe dispatch checkpoint (mapped status + time).</summary>
    public sealed class OrderDispatchEventDto
    {
        public ZansiDispatchShipmentStatus InternalStatus { get; set; }
        public string? Message { get; set; }
        public DateTime EventTime { get; set; }
    }

    /// <summary>A customer-safe lifecycle update (friendly label + time).</summary>
    public sealed class OrderDispatchCustomerUpdateDto
    {
        public string Label { get; set; } = string.Empty;
        public DateTime OccurredAtUtc { get; set; }
    }
}
