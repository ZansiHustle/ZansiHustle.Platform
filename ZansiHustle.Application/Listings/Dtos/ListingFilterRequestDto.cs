using System;
using ZansiHustle.Shared.Enums.Listings;

namespace ZansiHustle.Application.Listings.Dtos
{
    /// <summary>
    /// Filter + paging + sort inputs for <c>GET /api/listings</c>.
    /// Bound via [FromQuery] on the controller.
    /// </summary>
    public class ListingFilterRequestDto
    {
        public string? Q { get; set; }
        public ListingType? Type { get; set; }
        public ListingStatus? Status { get; set; }

        /// <summary>
        /// Optional explicit availability filter. Public listing search
        /// hard-excludes <see cref="AvailabilityMode.InStoreOnly"/>
        /// regardless of what callers pass — this filter narrows
        /// further (e.g. "OnlineOnly only") if needed.
        /// </summary>
        public AvailabilityMode? AvailabilityMode { get; set; }

        public Guid? MerchantId { get; set; }
        public Guid? SellerCategoryId { get; set; }
        public Guid? SellerSubcategoryId { get; set; }
        public string? Province { get; set; }
        public string? City { get; set; }
        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }
        public bool? FeaturedOnly { get; set; }

        /// <summary>
        /// One of: <c>newest</c> (default), <c>price_asc</c>, <c>price_desc</c>,
        /// <c>rating</c>, <c>featured</c>.
        /// </summary>
        public string? Sort { get; set; }

        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }
}
