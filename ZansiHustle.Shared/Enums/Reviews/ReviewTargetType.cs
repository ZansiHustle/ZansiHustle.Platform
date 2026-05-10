namespace ZansiHustle.Shared.Enums.Reviews
{
    /// <summary>
    /// Discriminator for what a Review is about. The same Reviews
    /// table backs every reviewable surface (Store, Shop, Product,
    /// Service) — the (TargetType, TargetId) pair scopes the review.
    ///
    /// V1 implements `Store` fully. The other values are reserved so
    /// future review surfaces (online shop, individual product /
    /// service listings) plug in without a schema change.
    /// </summary>
    public enum ReviewTargetType
    {
        /// <summary>Review of a PhysicalStore merchant. (V1.)</summary>
        Store = 1,

        /// <summary>Review of an OnlineStore merchant. (Reserved.)</summary>
        Shop = 2,

        /// <summary>Review of an individual product Listing. (Reserved.)</summary>
        Product = 3,

        /// <summary>Review of an individual service Listing. (Reserved.)</summary>
        Service = 4,
    }
}
