namespace ZansiHustle.Shared.Enums.Marketplace
{
    /// <summary>
    /// Lifecycle state of a casual Marketplace listing.
    ///
    /// Values mirror the buyer-facing mobile type
    /// <c>features/marketplace/types/MarketplaceListing.ts</c>.
    /// </summary>
    public enum MarketplaceListingStatus
    {
        Active = 1,
        Sold = 2,
        Archived = 3,
    }
}
