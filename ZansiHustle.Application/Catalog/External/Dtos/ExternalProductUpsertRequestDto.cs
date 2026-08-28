using System;
using System.Collections.Generic;

namespace ZansiHustle.Application.Catalog.External.Dtos
{
    /// <summary>
    /// Integration-focused product payload for POST
    /// /external-catalogs/{sourceCode}/products/upsert (and the batch
    /// variant used by full-snapshot sync). Deliberately NOT a mirror of
    /// ZansiHustle's internal CreateListingRequestDto — this is ZansiTech's
    /// own stronger model (structured variants, stable Guids), shaped
    /// around integration stability rather than ZansiHustle's UI form.
    ///
    /// Variants are OPTIONAL — ZansiTech supports both simple products
    /// (no ProductVariant rows) and variant products, and this contract
    /// mirrors that: omit/null/empty <see cref="Variants"/> for a simple
    /// product (mirrored as a Listing with zero ListingVariant rows,
    /// Price/Stock taken from <see cref="BasePrice"/>/<see cref="AvailableQuantity"/>
    /// directly); a non-empty list for a variant product (unchanged stable-id
    /// upsert behaviour). ZansiTech is never required to manufacture a
    /// synthetic variant for a single-SKU product.
    /// </summary>
    public sealed class ExternalProductUpsertRequestDto
    {
        /// <summary>Stable ZansiTech Product Guid. This — combined with the sourceCode in the route — is the synchronization identity. Never Slug/Title/SKU.</summary>
        public string ExternalProductId { get; set; } = string.Empty;

        public string Title { get; set; } = string.Empty;
        public string? Slug { get; set; }
        public string? Description { get; set; }

        /// <summary>The product's base/default price. See price-mapping rule: a variant whose own price equals this becomes UsesCustomPrice=false (inherits it); a different price becomes an explicit custom price.</summary>
        public decimal BasePrice { get; set; }
        public string Currency { get; set; } = "ZAR";

        /// <summary>"Active" | "Draft" | "Archived" (case-insensitive). Unrecognised values are rejected, not silently defaulted.</summary>
        public string Status { get; set; } = "Active";

        public ExternalProductCategoryDto? Category { get; set; }

        /// <summary>Public ZansiTech media URLs — referenced directly, never re-uploaded into ZansiHustle storage.</summary>
        public List<string> Images { get; set; } = new();

        /// <summary>Source-side last-modified timestamp/version. Stale-update protection: an upsert with a value <= the currently-stored one is a safe no-op.</summary>
        public DateTime SourceUpdatedAtUtc { get; set; }

        /// <summary>
        /// Product-level sellable quantity — used ONLY for a simple product
        /// (see <see cref="Variants"/>). Mirrored verbatim into Listing.Stock
        /// the same way a variant's AvailableQuantity mirrors into
        /// ListingVariant.Stock. Null = not tracked (matches Listing.Stock's
        /// existing nullable-means-untracked convention). Ignored when
        /// Variants is non-empty (Listing.Stock is then the sum of variant
        /// quantities instead).
        /// </summary>
        public int? AvailableQuantity { get; set; }

        /// <summary>
        /// Omit/null/empty = simple product (no ListingVariant rows). Non-empty
        /// = variant product (stable-id upsert). See class remarks.
        /// </summary>
        public List<ExternalProductVariantDto>? Variants { get; set; }
    }

    public sealed class ExternalProductCategoryDto
    {
        /// <summary>Source-side category code, looked up against ExternalCategoryMapping. No match = product syncs with no category (never blocks the sync).</summary>
        public string? Code { get; set; }
    }

    public sealed class ExternalProductVariantDto
    {
        /// <summary>Stable ZansiTech ProductVariant Guid — the per-variant synchronization identity. Existing id = update same row; new id = new row; an existing id missing from a later payload = deactivated.</summary>
        public string ExternalVariantId { get; set; } = string.Empty;

        public string? Sku { get; set; }

        /// <summary>Buyer-visible label. ZansiTech pre-flattens its structured options into this (e.g. "256GB · Black") — ZansiHustle stores it verbatim.</summary>
        public string Name { get; set; } = string.Empty;

        public decimal Price { get; set; }
        public bool IsActive { get; set; } = true;

        /// <summary>ZansiTech's authoritative sellable quantity — mirrored as-is into ListingVariant.Stock. ZansiHustle never treats this as its own source of truth (see distributed-inventory recommendation).</summary>
        public int AvailableQuantity { get; set; }

        /// <summary>Accepted for forward-compatibility; NOT persisted in this phase (Name already carries the flattened combination and nothing in ordering needs structured options yet).</summary>
        public List<ExternalProductOptionDto>? Options { get; set; }
    }

    public sealed class ExternalProductOptionDto
    {
        public string Name { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
    }
}
