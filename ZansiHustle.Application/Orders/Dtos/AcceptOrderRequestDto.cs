using System;

namespace ZansiHustle.Application.Orders.Dtos
{
    /// <summary>
    /// OPTIONAL body for the seller "accept product order" endpoint. Lets the
    /// seller indicate when the courier can collect. All fields are optional —
    /// older app versions that POST no body still work and default to
    /// "AnyDay / earliest available". The seller can NOT change the delivery
    /// service or price here — only pickup availability.
    /// </summary>
    public sealed class AcceptOrderRequestDto
    {
        /// <summary>Pickup preference: "Today" | "Tomorrow" | "AnyDay" | "Custom".
        /// Defaults to "AnyDay" when omitted.</summary>
        public string? PickupPreference { get; set; }

        /// <summary>Concrete requested pickup date (used when PickupPreference="Custom",
        /// or as a resolved date for Today/Tomorrow). Capped server-side to ≤5 days out.</summary>
        public DateTime? RequestedPickupDate { get; set; }

        /// <summary>Optional free-text note from the seller about collection.</summary>
        public string? SellerPickupNote { get; set; }

        /// <summary>OPTIONAL confirmed/edited collection address for THIS shipment.
        /// When omitted, the order's existing (merchant-resolved) pickup address is
        /// used. Never affects the buyer's delivery address.</summary>
        public ConfirmCollectionAddressDto? CollectionAddress { get; set; }

        /// <summary>When true, also persist <see cref="CollectionAddress"/> as the
        /// seller's pickup origin for FUTURE orders (updates the merchant collection
        /// address). When false, the edited address is used for this shipment only.</summary>
        public bool UpdateListingPickupAddress { get; set; }
    }

    /// <summary>Seller-confirmed collection (pickup) address snapshot. All optional;
    /// the seller edits/confirms it in the accept modal.</summary>
    public sealed class ConfirmCollectionAddressDto
    {
        public string? Company { get; set; }
        public string? StreetAddress { get; set; }
        public string? LocalArea { get; set; }
        public string? City { get; set; }
        public string? Province { get; set; }
        public string? PostalCode { get; set; }
        public string? Country { get; set; }
        public decimal? Lat { get; set; }
        public decimal? Lng { get; set; }
        /// <summary>Human-readable one-line summary (for display + the shipment pickup summary).</summary>
        public string? Summary { get; set; }
    }
}
