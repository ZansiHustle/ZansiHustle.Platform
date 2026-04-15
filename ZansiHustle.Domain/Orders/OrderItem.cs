using System;
using ZansiHustle.Domain.Listings;
using ZansiHustle.Shared.Enums.Listings;

namespace ZansiHustle.Domain.Orders
{
    /// <summary>
    /// A single line within an <see cref="Order"/> referencing a listing.
    /// Key fields (title, unit price, image) are snapshotted at purchase time so
    /// the order remains stable if the underlying listing is later edited or
    /// removed.
    /// </summary>
    public class OrderItem
    {
        public Guid Id { get; set; }

        public Guid OrderId { get; set; }
        public Order? Order { get; set; }

        /// <summary>
        /// FK to the listing the buyer purchased. Nullable so that catalogue
        /// cleanup (seller deletes a listing) does not cascade-delete historical
        /// orders — the snapshot fields carry the line item forward.
        /// </summary>
        public Guid? ListingId { get; set; }
        public Listing? Listing { get; set; }

        public ListingType ListingType { get; set; }

        // Snapshot fields (immutable after order placement).
        public string TitleSnapshot { get; set; } = string.Empty;
        public string? ImageSnapshot { get; set; }
        public decimal UnitPrice { get; set; }

        public int Quantity { get; set; } = 1;
        public decimal LineTotal { get; set; }

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    }
}
