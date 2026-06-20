using System;
using ZansiHustle.Shared.Enums.Shops;

namespace ZansiHustle.Domain.Shops
{
    /// <summary>
    /// Storefront / shop presentation layer. Distinct from
    /// <c>Merchant</c>:
    ///
    ///   • <c>Merchant</c>    — the seller's business / capability
    ///                          account. Owns listings, KYC, contact,
    ///                          bank, payout. Created from the seller
    ///                          application + admin approval.
    ///   • <see cref="ShopProfile"/> — the storefront the seller
    ///                          opens on top of the merchant. Has its
    ///                          own lifecycle, branding, and (later)
    ///                          subscription/billing fields. Created
    ///                          ONLY by the seller via the "Set up
    ///                          your shop" flow — never auto-created
    ///                          by seller approval.
    ///
    /// One ShopProfile per OnlineStore merchant for now (filtered
    /// unique index in <c>ShopProfileConfiguration</c> excludes
    /// Suspended rows so a re-open is possible). PhysicalStore
    /// merchants are out of scope for this entity.
    ///
    /// Listings are NOT moved here — they keep their <c>MerchantId</c>
    /// link. Public shop page resolves listings via
    /// <c>ShopProfile.MerchantId → Listings.MerchantId</c>.
    /// </summary>
    public class ShopProfile
    {
        public Guid Id { get; set; }

        /// <summary>FK to the owning OnlineStore <c>Merchant</c>.</summary>
        public Guid MerchantId { get; set; }

        /// <summary>URL-safe unique slug, generated from <see cref="Name"/>.</summary>
        public string Slug { get; set; } = string.Empty;

        /// <summary>Buyer-visible shop name. Required.</summary>
        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        public string? LogoUrl { get; set; }
        public string? BannerUrl { get; set; }

        /// <summary>
        /// Curated storefront theme preset key (see <see cref="ShopThemePresets"/>).
        /// Drives the Shop Profile page's visual styling on the client only.
        /// Defaults to <c>zansi_default</c>; existing shops are backfilled to it.
        /// </summary>
        public string ThemePresetKey { get; set; } = ShopThemePresets.Default;

        /// <summary>
        /// Storefront background mode (see <see cref="ShopThemeBackgroundModes"/>).
        /// Controls how strongly the preset affects the shop page background:
        /// <c>light</c> (accents only), <c>themed</c> (soft branded background),
        /// or <c>dark</c> (bold dark storefront). Defaults to <c>light</c>;
        /// existing shops are backfilled to it. Client-only presentation.
        /// </summary>
        public string ThemeBackgroundMode { get; set; } = ShopThemeBackgroundModes.Default;

        // Shop-specific contact, independent of merchant contact —
        // a seller may want a different public-facing contact for
        // their shop than the one tied to their KYC/bank record.
        public string? ContactEmail { get; set; }
        public string? ContactPhoneNumber { get; set; }
        public string? WhatsAppNumber { get; set; }

        public Guid? SellerCategoryId { get; set; }
        public Guid? SellerSubcategoryId { get; set; }

        public string? Province { get; set; }
        public string? City { get; set; }
        public string? AddressLine1 { get; set; }

        public ShopProfileStatus Status { get; set; } = ShopProfileStatus.Draft;
        public ShopSubscriptionStatus SubscriptionStatus { get; set; } = ShopSubscriptionStatus.None;

        public DateTime? EarlyAccessGrantedAtUtc { get; set; }
        public DateTime? EarlyAccessUntilUtc { get; set; }
        public DateTime? SubscriptionStartedAtUtc { get; set; }
        public DateTime? SubscriptionEndsAtUtc { get; set; }

        /// <summary>Reserved for the billing rollout (e.g. "Stripe", "Paystack").</summary>
        public string? BillingProvider { get; set; }
        /// <summary>Reserved — provider-side subscription / customer id.</summary>
        public string? BillingReference { get; set; }

        public DateTime? ActivatedAtUtc { get; set; }
        public DateTime? SuspendedAtUtc { get; set; }
        public string? SuspensionReason { get; set; }

        // Denormalised review aggregates. Mirrors the same pattern used
        // on Merchant.Rating / Merchant.ReviewCount: the canonical source
        // is the Reviews table, but the buyer-facing DTOs need a snappy
        // "4.7 (12)" without a join per row. ReviewService refreshes
        // these in the same DbContext after every create / update /
        // delete (see RefreshAggregateAsync) — never edited manually.
        public decimal? Rating { get; set; }
        public int ReviewCount { get; set; }

        /// <summary>
        /// Denormalised count of buyers following this shop. Source of
        /// truth is the <c>ShopFollows</c> table; updated transactionally
        /// with each follow row. Deliberately separate from the legacy
        /// <c>Merchant.FollowersCount</c> column — buyer "follow"
        /// semantics attach to the storefront, not the payout-merchant.
        /// </summary>
        public int FollowersCount { get; set; }

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAtUtc { get; set; }
    }
}
