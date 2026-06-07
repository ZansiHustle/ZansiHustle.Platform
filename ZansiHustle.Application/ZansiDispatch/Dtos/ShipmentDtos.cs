using System;
using ZansiHustle.Shared.Enums.ZansiDispatch;

namespace ZansiHustle.Application.ZansiDispatch.Dtos
{
    /// <summary>Full shipment detail for the command centre.</summary>
    public sealed class ShipmentDto
    {
        public Guid Id { get; set; }
        public Guid OrderId { get; set; }
        public Guid? QuoteId { get; set; }
        public Guid? QuoteOptionId { get; set; }
        public Guid UserId { get; set; }
        public Guid? MerchantId { get; set; }
        public Guid? ShopId { get; set; }

        public ZansiDispatchProviderType ProviderType { get; set; }
        public ZansiDispatchServiceLevel ServiceLevel { get; set; }

        public decimal QuotedDeliveryFee { get; set; }
        public decimal? ActualCourierCost { get; set; }
        public decimal SurplusAmount { get; set; }
        public decimal DeficitAmount { get; set; }
        public decimal NetAmount { get; set; }

        public string? ServiceLevelCode { get; set; }
        public string? ServiceLevelName { get; set; }

        public ZansiDispatchShipmentStatus Status { get; set; }
        public ZansiDispatchReconciliationStatus ReconciliationStatus { get; set; }

        public string? ProviderShipmentId { get; set; }
        public string? TrackingNumber { get; set; }
        public string? ShortTrackingReference { get; set; }
        public string? ProviderShipmentReference { get; set; }
        public string? CourierReference { get; set; }
        public string? PickupAddressSummary { get; set; }
        public string? DropoffAddressSummary { get; set; }
        public DateTime? PickupScheduledAt { get; set; }
        public DateTime? DeliveredAt { get; set; }
        public string? LabelUrl { get; set; }
        public DateTime? LabelUrlExpiresAt { get; set; }
        public string? Notes { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    /// <summary>Lighter row for the shipments list.</summary>
    public sealed class ShipmentListItemDto
    {
        public Guid Id { get; set; }
        public Guid OrderId { get; set; }
        public ZansiDispatchProviderType ProviderType { get; set; }
        public ZansiDispatchServiceLevel ServiceLevel { get; set; }
        public ZansiDispatchShipmentStatus Status { get; set; }
        public ZansiDispatchReconciliationStatus ReconciliationStatus { get; set; }
        public decimal QuotedDeliveryFee { get; set; }
        public decimal? ActualCourierCost { get; set; }
        public decimal NetAmount { get; set; }
        public string? TrackingNumber { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    /// <summary>Body for <c>POST .../shipments/{id}/actual-cost</c>.</summary>
    public sealed class CaptureActualCostRequestDto
    {
        public decimal ActualCourierCost { get; set; }
        public string? CourierReference { get; set; }
        public string? Notes { get; set; }
    }

    /// <summary>Body for <c>POST .../shipments/{id}/status</c>.</summary>
    public sealed class UpdateShipmentStatusRequestDto
    {
        public ZansiDispatchShipmentStatus Status { get; set; }
        public string? TrackingNumber { get; set; }
        public string? Notes { get; set; }
        public DateTime? DeliveredAt { get; set; }
    }
}
