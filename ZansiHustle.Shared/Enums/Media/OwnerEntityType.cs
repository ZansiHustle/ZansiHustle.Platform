namespace ZansiHustle.Shared.Enums.Media
{
    /// <summary>
    /// Stable code for the entity that "owns" a media asset. Stored as int
    /// in MediaAssets.OwnerEntityType so the table can carry media for
    /// multiple consumer entities without per-entity FKs.
    /// </summary>
    public enum OwnerEntityType
    {
        Merchant = 1,
        Listing = 2,
        User = 3,
        SellerLead = 4,
        MarketplaceListing = 5,
        Other = 99,
    }
}
