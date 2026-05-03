namespace ZansiHustle.Shared.Enums.Marketplace
{
    /// <summary>
    /// Condition of an item posted on the casual peer-to-peer
    /// Marketplace. Persisted as int on
    /// <see cref="Domain.Marketplace.MarketplaceListing.Condition"/>.
    ///
    /// Intentionally distinct from the merchant
    /// <c>ZansiHustle.Shared.Enums.Listings.ListingCondition</c> — the
    /// two domains share zero state and must not be conflated.
    /// </summary>
    public enum ProductCondition
    {
        New = 1,
        LikeNew = 2,
        Good = 3,
        Fair = 4,
        Poor = 5,
    }
}
