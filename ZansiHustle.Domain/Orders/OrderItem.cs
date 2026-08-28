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

        /// <summary>
        /// The specific variant purchased, when the listing sells through
        /// variants. Nullable — a variant-less listing never sets this.
        /// Same "survives catalogue cleanup" nullable-FK pattern as
        /// <see cref="ListingId"/>: the snapshot fields below carry the
        /// purchased selection forward even if the variant row is later
        /// changed or removed by a catalog sync.
        /// </summary>
        public Guid? VariantId { get; set; }
        public ListingVariant? Variant { get; set; }

        public ListingType ListingType { get; set; }

        // Snapshot fields (immutable after order placement).
        public string TitleSnapshot { get; set; } = string.Empty;
        public string? ImageSnapshot { get; set; }
        public decimal UnitPrice { get; set; }

        /// <summary>Snapshot of ListingVariant.Name at purchase time. Null when no variant was selected.</summary>
        public string? VariantNameSnapshot { get; set; }

        /// <summary>Snapshot of ListingVariant.Sku at purchase time. Null when no variant was selected or the variant had no SKU.</summary>
        public string? SkuSnapshot { get; set; }

        public int Quantity { get; set; } = 1;
        public decimal LineTotal { get; set; }

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    }
}
