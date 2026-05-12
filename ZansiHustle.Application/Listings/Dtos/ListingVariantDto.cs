using System;

namespace ZansiHustle.Application.Listings.Dtos
{
    /// <summary>
    /// Public projection of a <see cref="ZansiHustle.Domain.Listings.ListingVariant"/>.
    /// Returned inside <see cref="ListingDto.Variants"/> on the detail
    /// endpoint. Inactive variants are filtered out by the service so
    /// the DTO never carries them to buyers; the seller-side edit
    /// surface uses the same DTO but receives the full set (including
    /// inactive) through the create/update round-trip response.
    /// </summary>
    public class ListingVariantDto
    {
        public Guid Id { get; set; }
        public Guid ListingId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public decimal? Price { get; set; }
        public bool UsesCustomPrice { get; set; }
        public int? Stock { get; set; }
        public string? Sku { get; set; }
        public int SortOrder { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAtUtc { get; set; }
        public DateTime? UpdatedAtUtc { get; set; }
    }

    /// <summary>
    /// Seller payload for a single variant inside a create / update
    /// listing request.
    ///
    /// IMPORTANT — replace semantics on update: when this DTO appears
    /// inside <c>UpdateListingRequestDto.Variants</c>, the entire
    /// variant set on the listing is REPLACED with the supplied list.
    /// The optional <see cref="Id"/> field is therefore IGNORED on
    /// update — the server issues a fresh PK for every row. (The
    /// field is kept on the wire for symmetry with the read DTO; the
    /// frontend can omit it on new rows or pass it on existing rows,
    /// either is accepted but neither is honoured.)
    ///
    /// See <see cref="UpdateListingRequestDto.Variants"/> for the
    /// full rationale on why we switched away from a diff strategy.
    /// </summary>
    public class ListingVariantRequestDto
    {
        /// <summary>Ignored on update (variants are replaced wholesale). Null/empty on new rows.</summary>
        public Guid? Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public decimal? Price { get; set; }
        public bool UsesCustomPrice { get; set; }
        public int? Stock { get; set; }
        public string? Sku { get; set; }
        public int SortOrder { get; set; }
        public bool IsActive { get; set; } = true;
    }
}
