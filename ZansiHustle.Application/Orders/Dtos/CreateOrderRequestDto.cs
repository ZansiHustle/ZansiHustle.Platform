using System;
using System.Collections.Generic;

namespace ZansiHustle.Application.Orders.Dtos
{
    /// <summary>
    /// Buyer request to place a new order. All items must belong to the same
    /// merchant — the service resolves the merchant from the first item and
    /// rejects any mixed-merchant carts.
    /// </summary>
    public class CreateOrderRequestDto
    {
        public List<CreateOrderItemDto> Items { get; set; } = new();
        public string? DeliveryAddress { get; set; }
        public string? Notes { get; set; }

        /// <summary>
        /// Optional ZansiDispatch delivery quote option the buyer selected at
        /// checkout. When supplied, the backend validates it (ownership +
        /// unexpired), adds its fee to the order total, and creates the
        /// shipment. When omitted the order behaves exactly as before
        /// ZansiDispatch (no delivery fee, Total == Subtotal) — fully
        /// backward-compatible.
        /// </summary>
        public Guid? DeliveryQuoteOptionId { get; set; }

        /// <summary>
        /// Optional scheduled-booking details for a SERVICE order. When supplied
        /// (single service item), the backend re-validates the slot is still
        /// available, then persists a <c>ServiceBooking</c> tied to the order so
        /// the slot is removed from future availability. Omitted for product
        /// orders — fully backward-compatible.
        /// </summary>
        public ServiceBookingDetailsDto? ServiceBooking { get; set; }
    }

    public class CreateOrderItemDto
    {
        public Guid ListingId { get; set; }

        /// <summary>
        /// Required when the listing has any active variants; must be null
        /// for a listing with none. Ignored (never inferred) when the
        /// listing has no variants. Never a price source — the server
        /// always looks up the variant's own price server-side.
        /// </summary>
        public Guid? VariantId { get; set; }

        public int Quantity { get; set; } = 1;
    }

    /// <summary>
    /// Buyer-selected booking details (local SA date/time + chosen mode +
    /// optional house-call address). Times are local; the server converts to UTC.
    /// </summary>
    public class ServiceBookingDetailsDto
    {
        /// <summary>Local date, yyyy-MM-dd.</summary>
        public string Date { get; set; } = string.Empty;
        /// <summary>Local start time, HH:mm (24h).</summary>
        public string Time { get; set; } = string.Empty;
        /// <summary>Estimated duration; defaults to the platform default when null.</summary>
        public int? DurationMinutes { get; set; }
        /// <summary>"house_call" or "provider_location".</summary>
        public string? Mode { get; set; }

        // House-call address (ignored for provider-location bookings).
        public string? BuyerFormattedAddress { get; set; }
        public string? BuyerAddressLine1 { get; set; }
        public decimal? BuyerLatitude { get; set; }
        public decimal? BuyerLongitude { get; set; }
        public string? BuyerPlaceId { get; set; }

        /// <summary>Snapshot of the provider location summary at booking time.</summary>
        public string? ProviderLocationSummary { get; set; }
    }
}
