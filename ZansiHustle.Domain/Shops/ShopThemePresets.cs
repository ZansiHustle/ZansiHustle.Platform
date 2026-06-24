using System;
using System.Collections.Generic;

namespace ZansiHustle.Domain.Shops
{
    /// <summary>
    /// Whitelist of curated shop storefront theme presets. The actual colours
    /// live on the mobile client (presentation concern) — the backend only
    /// stores + validates the preset KEY so a shop can't persist an arbitrary
    /// or ugly value. Free-form custom colours are intentionally NOT supported
    /// yet (contrast/accessibility can't be guaranteed).
    /// </summary>
    public static class ShopThemePresets
    {
        public const string Default = "zansi_default";

        public static readonly IReadOnlySet<string> Allowed = new HashSet<string>(StringComparer.Ordinal)
        {
            // ── Curated, selectable presets (mobile picker) ──────────────────
            "zansi_default",
            "luxury_black_gold",
            "soft_beauty_nude",
            "pink_glam",
            "barber_black_silver",
            "clean_medical",
            "tech_blue",
            "fashion_cream_black",
            "afro_earth",
            "minimal_white_black",
            // ── Legacy keys — kept valid so existing shops never fail to save /
            // re-save, and old stored values stay accepted. ──────────────────
            "midnight_lime",
            "blush_pink",
            "royal_gold",
            "ocean_blue",
            "earth_olive",
            "purple_pop",
            "clean_mono",
        };

        public static bool IsValid(string? key)
            => !string.IsNullOrWhiteSpace(key) && Allowed.Contains(key.Trim());

        /// <summary>
        /// null/empty → <see cref="Default"/>; a known key → that key (trimmed);
        /// an unknown non-empty key → <c>null</c> so the caller can reject it
        /// with a 400 rather than silently saving bad data.
        /// </summary>
        public static string? Normalize(string? key)
        {
            if (string.IsNullOrWhiteSpace(key)) return Default;
            var trimmed = key.Trim();
            return Allowed.Contains(trimmed) ? trimmed : null;
        }
    }
}
