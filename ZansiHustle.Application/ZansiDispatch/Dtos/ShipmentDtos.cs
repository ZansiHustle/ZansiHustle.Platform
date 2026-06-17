using System;
using ZansiHustle.Shared.Enums.ZansiDispatch;

namespace ZansiHustle.Application.ZansiDispatch.Dtos
{
    /// <summary>
    /// Server-side query for the command-centre shipments grid: paging, sorting,
    /// and filtering all applied in the database (no full-table client fetch).
    /// </summary>
    public sealed class ShipmentQueryDto
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 25;
        /// <summary>createdAt | orderId | customer | provider | status | quoted | actual | net. Default createdAt.</summary>
        public string? SortBy { get; set; }
        /// <summary>asc | desc. Default desc (latest first).</summary>
        public string? SortDirection { get; set; }
        public ZansiDispatchShipmentStatus? Status { get; set; }
        public ZansiDispatchProviderType? Provider { get; set; }
        /// <summary>Free-text: tracking ref / short ref / provider shipment id / order code / customer name+email.</summary>
        public string? Search { get; set; }
        public DateTime? DateFrom { get; set; }
        public DateTime? DateTo { get; set; }
    }

    /// <summary>Full shipment detail for the command centre.</summary>
    public sealed class ShipmentDto
    {
        public Guid Id { get; set; }
        public Guid OrderId { get; set; }
        public Guid? QuoteId { get; set; }
        public Guid? QuoteOptionId { get; set; }
        public Guid UserId { get; set; }
        /// <summary>Buyer display name (full name, falling back to email). Null if the user can't be resolved.</summary>
        public string? CustomerName { get; set; }
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
        /// <summary>Display tracking reference: <c>TrackingNumber</c> when present,
        /// otherwise the courier <c>ShortTrackingReference</c>. Avoids showing a
        /// null tracking number when a usable short ref exists.</summary>
        public string? TrackingReference { get; set; }
        public string? ProviderShipmentReference { get; set; }
        public string? CourierReference { get; set; }
        public string? PickupAddressSummary { get; set; }
        public string? DropoffAddressSummary { get; set; }
        public DateTime? PickupScheduledAt { get; set; }
        public DateTime? DeliveredAt { get; set; }

        // ── Seller pickup preference (requested on accept) vs provider truth ──
        public DateTime? SellerRequestedPickupDate { get; set; }
        public string? SellerPickupPreference { get; set; }
        public string? SellerPickupNote { get; set; }

        // ── Courier date promises + parcel facts (from provider; null until returned) ──
        public DateTime? ExpectedCollectionDate { get; set; }
        public DateTime? ExpectedDeliveryFrom { get; set; }
        public DateTime? ExpectedDeliveryTo { get; set; }
        public decimal? ChargedWeightKg { get; set; }
        public decimal? ActualWeightKg { get; set; }
        public decimal? VolumetricWeightKg { get; set; }
        public decimal? BaseRate { get; set; }
        public string? ProviderStatusMessage { get; set; }
        public string? PackageTrackingReference { get; set; }

        public string? LabelUrl { get; set; }
        public DateTime? LabelUrlExpiresAt { get; set; }
        public string? Notes { get; set; }

        // ── Booking-attempt / failure (NeedsAttention queue) ────────────
        /// <summary>Why the last courier-booking attempt failed (NeedsAttention).</summary>
        public string? FailureReason { get; set; }
        public DateTime? LastBookingAttemptAtUtc { get; set; }
        public int BookingAttemptCount { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    /// <summary>Lighter row for the shipments list.</summary>
    public sealed class ShipmentListItemDto
    {
        public Guid Id { get; set; }
        public Guid OrderId { get; set; }
        public Guid UserId { get; set; }
        /// <summary>Buyer display name (full name, falling back to email). Null if the user can't be resolved.</summary>
        public string? CustomerName { get; set; }
        public ZansiDispatchProviderType ProviderType { get; set; }
        public ZansiDispatchServiceLevel ServiceLevel { get; set; }
        public ZansiDispatchShipmentStatus Status { get; set; }
        public ZansiDispatchReconciliationStatus ReconciliationStatus { get; set; }
        public decimal QuotedDeliveryFee { get; set; }
        public decimal? ActualCourierCost { get; set; }
        public decimal SurplusAmount { get; set; }
        public decimal DeficitAmount { get; set; }
        public decimal NetAmount { get; set; }
        public string? TrackingNumber { get; set; }
        public string? ShortTrackingReference { get; set; }
        /// <summary>Display tracking reference: TrackingNumber ?? ShortTrackingReference.</summary>
        public string? TrackingReference { get; set; }
        /// <summary>Why the last courier-booking attempt failed (NeedsAttention queue).</summary>
        public string? FailureReason { get; set; }
        public DateTime? LastBookingAttemptAtUtc { get; set; }
        public int BookingAttemptCount { get; set; }
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
