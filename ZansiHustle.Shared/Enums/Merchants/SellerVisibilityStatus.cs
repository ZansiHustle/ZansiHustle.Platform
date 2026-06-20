namespace ZansiHustle.Shared.Enums.Merchants
{
    /// <summary>
    /// Buyer-facing visibility of a seller ACCOUNT (the seller's
    /// <c>OnlineStore</c> Merchant). Independent of <see cref="MerchantStatus"/>
    /// (the lifecycle/approval state) and of a Shop's own visibility — a paused
    /// seller account hides only its SELLER-ACCOUNT listings from public
    /// discovery; shop-attached items follow the shop's own visibility.
    ///
    ///   • <c>Visible</c>     — default; seller-account listings appear publicly.
    ///   • <c>Paused</c>      — seller self-paused; listings hidden from buyers,
    ///                          nothing deleted, statuses unchanged. Reversible
    ///                          by the seller.
    ///   • <c>UnderReview</c> — a sensitive change is pending re-approval; hidden
    ///                          from buyers; the seller cannot self-resume.
    ///   • <c>Blocked</c>     — admin/compliance hold; hidden from buyers; only an
    ///                          admin can lift it.
    /// </summary>
    public enum SellerVisibilityStatus
    {
        Visible = 1,
        Paused = 2,
        UnderReview = 3,
        Blocked = 4,
    }
}
