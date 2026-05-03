using System.Collections.Generic;
using ZansiHustle.Shared.Enums.Marketplace;

namespace ZansiHustle.Application.Marketplace.Dtos
{
    /// <summary>
    /// Body for <c>POST /api/marketplace-listings</c>. Owner is taken
    /// from the JWT (NOT this DTO) so a malicious client can't post on
    /// someone else's behalf.
    /// </summary>
    public class CreateMarketplaceListingRequestDto
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string Category { get; set; } = string.Empty;
        public ProductCondition Condition { get; set; }
        public string Province { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public bool AllowOffers { get; set; }

        /// <summary>
        /// Optional initial image URLs. Each becomes a
        /// <see cref="ZansiHustle.Domain.Marketplace.MarketplaceListingImage"/>
        /// row, ordered by array index. The client should have already
        /// uploaded these via the existing media-upload pipeline before
        /// calling this endpoint. Additional images can be added later
        /// via <c>POST /api/marketplace-listings/{id}/images</c>.
        /// </summary>
        public List<string>? ImageUrls { get; set; }
    }
}
