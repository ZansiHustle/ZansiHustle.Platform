namespace ZansiHustle.Shared.Enums.Reviews
{
    /// <summary>
    /// Discriminator for what a Review is about. The same Reviews
    /// table backs every reviewable surface (Store, Shop, Product,
    /// Service) — the (TargetType, TargetId) pair scopes the review.
    ///
    /// Currently implemented: Store (PhysicalStore merchant) and Shop
    /// (ShopProfile storefront). Product / Service targets are
    /// reserved — adding them is target-validation + aggregate-refresh
    /// work in <c>ReviewService</c>, no schema change needed.
    /// </summary>
    public enum ReviewTargetType
    {
        /// <summary>Review of a PhysicalStore merchant. TargetId = Merchant.Id.</summary>
        Store = 1,

        /// <summary>
        /// Review of an OnlineStore storefront. TargetId = ShopProfile.Id
        /// (NOT the owning Merchant.Id — owner lookup goes through
        /// ShopProfile.MerchantId at validation time).
        /// </summary>
        Shop = 2,

        /// <summary>Review of an individual product Listing. (Reserved.)</summary>
        Product = 3,

        /// <summary>Review of an individual service Listing. (Reserved.)</summary>
        Service = 4,
    }
}
