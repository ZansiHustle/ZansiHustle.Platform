using ZansiHustle.Shared.Enums.Marketplace;

namespace ZansiHustle.Application.Marketplace.Dtos
{
    /// <summary>
    /// Body for <c>PATCH /api/marketplace-listings/{id}/status</c>.
    /// Sellers use this to mark an item Sold or Archived; Status is the
    /// only field this endpoint changes.
    /// </summary>
    public class UpdateMarketplaceListingStatusRequestDto
    {
        public MarketplaceListingStatus Status { get; set; }
    }
}
