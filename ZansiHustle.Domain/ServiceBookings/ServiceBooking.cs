using System;
using ZansiHustle.Domain.Listings;
using ZansiHustle.Domain.Merchants;
using ZansiHustle.Domain.Orders;
using ZansiHustle.Shared.Enums.ServiceBookings;

namespace ZansiHustle.Domain.ServiceBookings
{
    /// <summary>
    /// A scheduled service booking — one slot a buyer reserved against a service
    /// <see cref="Listing"/>, tied to the <see cref="Order"/> that pays for it.
    ///
    /// This is the queryable source of truth the availability endpoint subtracts
    /// from seller availability to prevent double-booking. Before this entity
    /// existed the chosen date/time only lived in <c>Order.Notes</c> free text,
    /// which could not be queried.
    ///
    /// All times are stored in UTC. <see cref="StartAtUtc"/>/<see cref="EndAtUtc"/>
    /// are the canonical overlap window; <see cref="EstimatedDurationMinutes"/> +
    /// <see cref="BufferMinutes"/> are captured as a snapshot so a later listing
    /// edit can't retroactively change how long a confirmed booking blocks.
    /// </summary>
    public class ServiceBooking
    {
        public Guid Id { get; set; }

        // ── Links ────────────────────────────────────────────────────────────
        /// <summary>The order that pays for this booking.</summary>
        public Guid OrderId { get; set; }
        public Order? Order { get; set; }

        /// <summary>Optional line item this booking corresponds to.</summary>
        public Guid? OrderItemId { get; set; }

        /// <summary>The service listing being booked.</summary>
        public Guid ListingId { get; set; }
        public Listing? Listing { get; set; }

        /// <summary>Provider (merchant) — availability is computed per provider so
        /// one provider can't be double-booked across any of their services.</summary>
        public Guid MerchantId { get; set; }
        public Merchant? Merchant { get; set; }

        /// <summary>Buyer who placed the booking.</summary>
        public Guid CustomerUserId { get; set; }

        // ── Schedule (UTC) ───────────────────────────────────────────────────
        public DateTime StartAtUtc { get; set; }
        public DateTime EndAtUtc { get; set; }
        /// <summary>Snapshot of the duration used to compute EndAtUtc.</summary>
        public int EstimatedDurationMinutes { get; set; }
        /// <summary>Snapshot of the listing buffer applied before/after the slot.</summary>
        public int BufferMinutes { get; set; }

        // ── Booking detail ───────────────────────────────────────────────────
        public ServiceBookingMode Mode { get; set; }
        public ServiceBookingStatus Status { get; set; } = ServiceBookingStatus.PendingPayment;

        // Buyer-supplied location (house calls). Nullable for provider-location.
        public string? BuyerFormattedAddress { get; set; }
        public string? BuyerAddressLine1 { get; set; }
        public decimal? BuyerLatitude { get; set; }
        public decimal? BuyerLongitude { get; set; }
        public string? BuyerPlaceId { get; set; }

        /// <summary>Snapshot of the provider location summary at booking time.</summary>
        public string? ProviderLocationSnapshot { get; set; }

        public string? Notes { get; set; }

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAtUtc { get; set; }
    }
}
