namespace ZansiHustle.Shared.Enums.Shops
{
    /// <summary>
    /// Buyer-facing visibility of a <c>ShopProfile</c> storefront. Distinct from
    /// <see cref="ShopProfileStatus"/> (the lifecycle state: Draft/Active/
    /// Suspended/PendingReview) — a shop can be lifecycle-Active yet
    /// seller-paused. Independent of the owning seller account's visibility.
    ///
    ///   • <c>Visible</c>     — default; shop + its attached listings appear publicly.
    ///   • <c>Paused</c>      — seller self-paused; shop + items hidden from buyers,
    ///                          nothing deleted, listing statuses unchanged. The
    ///                          owner can still preview/manage it.
    ///   • <c>UnderReview</c> — pending re-approval; hidden from buyers.
    ///   • <c>Blocked</c>     — admin/compliance hold; hidden from buyers; admin-only lift.
    /// </summary>
    public enum ShopVisibilityStatus
    {
        Visible = 1,
        Paused = 2,
        UnderReview = 3,
        Blocked = 4,
    }
}
