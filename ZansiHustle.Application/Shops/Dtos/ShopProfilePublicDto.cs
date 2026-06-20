using System;
using ZansiHustle.Domain.Shops;

namespace ZansiHustle.Application.Shops.Dtos
{
    /// <summary>
    /// Buyer-facing shop projection. Zero billing / subscription /
    /// suspension fields — anything that's an owner/admin concern
    /// stays in <see cref="ShopProfileDto"/>. Returned by
    /// <c>GET /api/shops/public</c> and <c>GET /api/shops/{id}</c>
    /// for unauthenticated callers.
    /// </summary>
    public class ShopProfilePublicDto
    {
        public Guid Id { get; set; }
        public Guid MerchantId { get; set; }
        public string Slug { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? LogoUrl { get; set; }
        public string? BannerUrl { get; set; }
        /// <summary>Curated storefront theme preset key (defaults to zansi_default).
        /// Drives the Shop Profile page styling on the client.</summary>
        public string ThemePresetKey { get; set; } = ShopThemePresets.Default;
        public string? SellerCategoryName { get; set; }
        public string? SellerSubcategoryName { get; set; }
        public string? Province { get; set; }
        public string? City { get; set; }

        // Review aggregates surfaced to buyers. Null Rating + 0
        // ReviewCount means "no reviews yet" — the client renders an
        // empty-state pill rather than "0.0 (0)".
        public decimal? Rating { get; set; }
        public int ReviewCount { get; set; }

        // ── About-tab enrichment ─────────────────────────────────────
        // Populated by the DETAIL endpoint (GET /api/shops/{id}) only.
        // The list endpoint (GET /api/shops/public) leaves these at
        // their defaults (null / false / DateTime.MinValue) to avoid an
        // N+1 join across the page of merchants + owner users — cards
        // don't need this data, only the profile screen does.

        /// <summary>When the shop opened. Public — used for the
        /// "Shop opened May 2026" trust line on the About tab.</summary>
        public DateTime CreatedAtUtc { get; set; }

        /// <summary>
        /// Public display name of the shop's owning user
        /// (FirstName + LastName). Same source the reviews surface uses
        /// for reviewer names — so this leaks no information that isn't
        /// already visible elsewhere in the app. Null when the owning
        /// user record has neither first nor last name.
        /// </summary>
        public string? OwnerDisplayName { get; set; }

        /// <summary>
        /// Owning <c>Merchant.WebsiteUrl</c>. Surfaced as the shop's
        /// "Online presence" link. Never includes raw shop email/phone —
        /// those stay on the owner's private merchant record.
        /// </summary>
        public string? WebsiteUrl { get; set; }

        /// <summary>
        /// Trust badge derived from the owning Merchant's KYC. True iff
        /// <c>Merchant.KycStatus == Verified</c>. Not a payment / payout
        /// claim — purely identity verification.
        /// </summary>
        public bool IsVerified { get; set; }

        // ── Engagement ───────────────────────────────────────────────
        /// <summary>Denormalised follower count from <c>ShopProfile.FollowersCount</c>.</summary>
        public int FollowersCount { get; set; }
        /// <summary>
        /// True when the authenticated caller follows this shop.
        /// Always <c>false</c> for anonymous / unauthenticated reads.
        /// </summary>
        public bool IsFollowedByMe { get; set; }
    }
}
