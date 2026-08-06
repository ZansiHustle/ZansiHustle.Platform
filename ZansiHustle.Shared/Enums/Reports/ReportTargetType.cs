namespace ZansiHustle.Shared.Enums.Reports
{
    /// <summary>
    /// The kind of user-generated object a <c>ContentReport</c> targets.
    /// Polymorphic (TargetType + TargetId), mirroring the Reviews pattern.
    /// </summary>
    public enum ReportTargetType
    {
        /// <summary>A merchant Listing (product or service).</summary>
        Listing = 1,
        /// <summary>A ShopProfile storefront.</summary>
        Shop = 2,
        /// <summary>A Merchant / seller / store account (its public profile).</summary>
        Merchant = 3,
        /// <summary>A peer-to-peer MarketplaceListing.</summary>
        MarketplaceListing = 4,
        /// <summary>A Review.</summary>
        Review = 5,
        /// <summary>A chat message.</summary>
        ChatMessage = 6,
        /// <summary>A user (their profile / conduct).</summary>
        User = 7
    }
}
