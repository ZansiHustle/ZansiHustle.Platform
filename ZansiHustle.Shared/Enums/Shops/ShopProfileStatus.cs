namespace ZansiHustle.Shared.Enums.Shops
{
    /// <summary>
    /// Lifecycle state of a <c>ShopProfile</c> storefront. Distinct
    /// from <c>MerchantStatus</c> — a merchant can be Active while its
    /// ShopProfile is Draft / Suspended / not yet created at all.
    ///
    ///   • <see cref="Draft"/>         — seller is still configuring; not visible to buyers.
    ///   • <see cref="Active"/>        — live on public surfaces.
    ///   • <see cref="Suspended"/>     — admin-suspended; hidden from buyers, kept on file.
    ///   • <see cref="PendingReview"/> — reserved for the future paid-tier rollout
    ///                                    where admin review may gate activation.
    ///                                    During early access we skip this state.
    /// </summary>
    public enum ShopProfileStatus
    {
        Draft = 1,
        Active = 2,
        Suspended = 3,
        PendingReview = 4
    }
}
