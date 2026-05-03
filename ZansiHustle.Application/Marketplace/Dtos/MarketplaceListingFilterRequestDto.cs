using ZansiHustle.Shared.Enums.Marketplace;

namespace ZansiHustle.Application.Marketplace.Dtos
{
    /// <summary>
    /// Filter + paging + sort inputs for <c>GET /api/marketplace-listings</c>.
    /// Bound via [FromQuery] on the controller.
    /// </summary>
    public class MarketplaceListingFilterRequestDto
    {
        /// <summary>Free-text search across Title and Description.</summary>
        public string? Q { get; set; }

        public string? Category { get; set; }
        public ProductCondition? Condition { get; set; }
        public string? Province { get; set; }

        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }

        /// <summary>
        /// When true, return only the seller's own listings regardless
        /// of status (sold + archived included). Used by /mine.
        /// </summary>
        public MarketplaceListingStatus? Status { get; set; }

        /// <summary>
        /// One of: <c>newest</c> (default), <c>price_asc</c>,
        /// <c>price_desc</c>, <c>featured</c>.
        /// </summary>
        public string? Sort { get; set; }

        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }
}
