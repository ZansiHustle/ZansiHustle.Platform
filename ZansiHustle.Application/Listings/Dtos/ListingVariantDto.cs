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
    /// Seller payload for adding or updating a single variant inside
    /// a create/update listing request. The optional <see cref="Id"/>
    /// is the diff signal: when supplied AND it matches an existing
    /// variant of the parent listing, that row is updated in place
    /// (so client references like cart/wishlist stay stable). When
    /// omitted (or unmatched), a new variant is inserted. Variants
    /// missing from the request are removed.
    /// </summary>
    public class ListingVariantRequestDto
    {
        /// <summary>Existing variant id for diff updates. Null/empty = new row.</summary>
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
