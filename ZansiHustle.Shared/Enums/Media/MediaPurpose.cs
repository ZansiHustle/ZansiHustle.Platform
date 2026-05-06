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

        // ── Physical-store verification (Private + admin review) ────
        // New for the Nearby / Store Locator flow. Used during seller
        // onboarding when MerchantType == PhysicalStore. All four
        // share the same privacy + review semantics as the existing
        // ID/Portrait/Sample bucket: Private container, Visibility
        // Private, default 10MB cap, requires admin review.
        //
        //   Storefront / interior — required for every physical
        //   store. Prove the location actually exists and operates.
        //
        //   Shelves — optional but encouraged for product-based
        //   shops (hardware, grocery, pharmacy). Salon/repair-only
        //   businesses skip it.
        //
        //   BusinessRegistration — optional. Useful for formalised
        //   businesses; informal sellers skip it.
        //
        //   BusinessLicense — required only for regulated categories
        //   (healthcare/pharmacy/clinic etc.). The application layer
        //   decides per-category; the policy here is just "this is a
        //   verification doc, treat it like the rest".
        StorefrontPhoto              = 40,
        StoreInteriorPhoto           = 41,
        StoreShelvesPhoto            = 42,
        BusinessRegistrationDocument = 43,
        BusinessLicenseDocument      = 44,

        Other = 99,
    }
}
