using System;

namespace ZansiHustle.Domain.Listings
{
    /// <summary>
    /// One option of a parent <see cref="Listing"/>. The MVP model is a
    /// flat list of variants per listing — e.g. colours for a t-shirt,
    /// storage tiers for a phone, or packages for a service. Multi-
    /// dimensional combinations (Color × Storage) are intentionally
    /// out of scope; sellers express them as a flat row
    /// ("Black · 256GB") for now.
    ///
    /// A listing with zero variants behaves exactly as it did pre-MVP:
    /// price and stock come from <see cref="Listing.Price"/> and
    /// <see cref="Listing.Stock"/>. A listing with one or more variants
    /// surfaces them on the detail screen; buyer must pick one and the
    /// (future) cart will snapshot variant name + price.
    ///
    /// Price model:
    ///   • <see cref="UsesCustomPrice"/> = false → <see cref="Price"/>
    ///     is ignored, the variant inherits the listing's base price
    ///     and follows any base-price edits.
    ///   • <see cref="UsesCustomPrice"/> = true  → <see cref="Price"/>
    ///     is authoritative for this variant and survives base-price
    ///     edits.
    ///
    /// Stock model:
    ///   • Null → not tracked at the variant level (product falls back
    ///     to <see cref="Listing.Stock"/> or behaves as if stock isn't
    ///     tracked). For services, stock is typically null.
    ///   • Non-null → variant has its own inventory count.
    /// </summary>
    public class ListingVariant
    {
        public Guid Id { get; set; }

        /// <summary>FK to the parent listing. Cascade-deletes with the listing.</summary>
        public Guid ListingId { get; set; }
        public Listing? Listing { get; set; }

        /// <summary>
        /// Buyer-visible variant label (e.g. "White", "Black · 256GB",
        /// "Premium package"). Required. Max 80 chars.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>Optional buyer-visible note for the variant. Max 300 chars.</summary>
        public string? Description { get; set; }

        /// <summary>
        /// Per-variant price. Only authoritative when
        /// <see cref="UsesCustomPrice"/> is true; otherwise ignored.
        /// Stored as nullable so a "uses base price" variant has an
        /// honest NULL rather than a stale duplicate of the base price.
        /// </summary>
        public decimal? Price { get; set; }

        /// <summary>
        /// When true, <see cref="Price"/> is used. When false, the
        /// variant follows the listing's base price.
        /// </summary>
        public bool UsesCustomPrice { get; set; }

        /// <summary>
        /// Optional variant-level stock. Null = not tracked at this
        /// level. For services, typically null.
        /// </summary>
        public int? Stock { get; set; }

        /// <summary>Optional SKU / vendor code. Max 64 chars.</summary>
        public string? Sku { get; set; }

        /// <summary>Display order within the parent listing (ascending).</summary>
        public int SortOrder { get; set; }

        /// <summary>
        /// Soft-hide flag. Inactive variants are persisted but not
        /// shown to buyers — useful when a colour sells out for a
        /// season but the seller wants to keep the row for next year.
        /// </summary>
        public bool IsActive { get; set; } = true;

        // ── External catalog sync ────────────────────────────────────
        // Denormalised copy of the parent Listing's ExternalSourceCode so
        // (ExternalSourceCode, ExternalVariantId) uniquely identifies a
        // mirrored variant without a join. Both null for a manually-created
        // variant. For externally-managed listings, sync matches on
        // ExternalVariantId (stable across syncs) instead of the normal
        // wholesale-replace-with-fresh-ids strategy used by seller edits.
        public string? ExternalSourceCode { get; set; }
        public string? ExternalVariantId { get; set; }

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAtUtc { get; set; }
    }
}
