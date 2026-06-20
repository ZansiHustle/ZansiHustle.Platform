using System;
using ZansiHustle.Domain.Shops;
using ZansiHustle.Shared.Enums.Shops;

namespace ZansiHustle.Application.Shops.Dtos
{
    /// <summary>
    /// Owner / admin-facing shop profile. Includes lifecycle and
    /// subscription fields. Public buyers receive
    /// <see cref="ShopProfilePublicDto"/> instead, which projects
    /// only the buyer-safe subset.
    /// </summary>
    public class ShopProfileDto
    {
        public Guid Id { get; set; }
        public Guid MerchantId { get; set; }
        public string Slug { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? LogoUrl { get; set; }
        public string? BannerUrl { get; set; }
        /// <summary>Curated storefront theme preset key (defaults to zansi_default).</summary>
        public string ThemePresetKey { get; set; } = ShopThemePresets.Default;
        /// <summary>Storefront background mode: light/themed/dark (defaults to light).</summary>
        public string ThemeBackgroundMode { get; set; } = ShopThemeBackgroundModes.Default;
        public string? ContactEmail { get; set; }
        public string? ContactPhoneNumber { get; set; }
        public string? WhatsAppNumber { get; set; }
        public Guid? SellerCategoryId { get; set; }
        public string? SellerCategoryName { get; set; }
        public Guid? SellerSubcategoryId { get; set; }
        public string? SellerSubcategoryName { get; set; }
        public string? Province { get; set; }
        public string? City { get; set; }
        public string? AddressLine1 { get; set; }

        public ShopProfileStatus Status { get; set; }
        public ShopSubscriptionStatus SubscriptionStatus { get; set; }

        /// <summary>Buyer-facing visibility (Visible / Paused / UnderReview / Blocked).</summary>
        public ShopVisibilityStatus VisibilityStatus { get; set; } = ShopVisibilityStatus.Visible;
        public DateTime? VisibilityPausedAtUtc { get; set; }
        public string? VisibilityPauseReason { get; set; }

        public DateTime? EarlyAccessGrantedAtUtc { get; set; }
        public DateTime? EarlyAccessUntilUtc { get; set; }
        public DateTime? SubscriptionStartedAtUtc { get; set; }
        public DateTime? SubscriptionEndsAtUtc { get; set; }
        public string? BillingProvider { get; set; }

        public DateTime? ActivatedAtUtc { get; set; }
        public DateTime? SuspendedAtUtc { get; set; }
        public string? SuspensionReason { get; set; }

        public decimal? Rating { get; set; }
        public int ReviewCount { get; set; }

        // ── Engagement ───────────────────────────────────────────────
        public int FollowersCount { get; set; }
        /// <summary>True when the authenticated caller follows this shop. False on anonymous reads.</summary>
        public bool IsFollowedByMe { get; set; }

        public DateTime CreatedAtUtc { get; set; }
        public DateTime? UpdatedAtUtc { get; set; }
    }
}
