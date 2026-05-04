namespace ZansiHustle.Shared.Enums.Media
{
    /// <summary>
    /// Why this media exists. Each purpose maps deterministically to a default
    /// Visibility tier (Private vs Public) and to whether it requires admin
    /// review (e.g. IdDocument always needs review; ProductGallery never does).
    /// Keep stable — purposes are stored as integers in the DB.
    /// </summary>
    public enum MediaPurpose
    {
        // ── Private verification ─────────────────────────────────────
        IdDocument = 1,
        Portrait = 2,
        VerificationProductSample = 3,
        VerificationOther = 4,
        /// <summary>
        /// Bank confirmation letter / recent bank statement uploaded on the
        /// seller's /merchant/bank page to speed up payout verification.
        /// Same privacy + admin-review semantics as other verification docs.
        /// </summary>
        BankProof = 5,

        // ── Public listing / display ─────────────────────────────────
        ProductGallery = 10,
        ProductHero = 11,
        ServiceGallery = 12,
        ServiceHero = 13,

        // ── Profile / brand ──────────────────────────────────────────
        ShopLogo = 20,
        ShopBanner = 21,
        UserAvatar = 22,

        // ── Marketplace (peer-to-peer second-hand) ───────────────────
        // Permanent public asset attached to a MarketplaceListing.
        // Must NOT be Private — listings render the stored URL directly
        // for buyers, so a TTL'd signed URL would rot the image after
        // the user navigates away.
        MarketplaceListingImage = 30,

        Other = 99,
    }
}
