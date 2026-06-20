using System;
using System.Collections.Generic;

namespace ZansiHustle.Domain.Shops
{
    /// <summary>
    /// Whitelist of shop storefront background modes. Controls HOW strongly the
    /// chosen <see cref="ShopThemePresets"/> preset affects the public shop page:
    ///
    ///   • <c>light</c>  — white shop with theme accents (the safe default).
    ///   • <c>themed</c> — soft branded page background, white cards.
    ///   • <c>dark</c>   — bold dark / branded storefront background.
    ///
    /// Like the preset key, the backend only stores + validates the MODE string;
    /// the actual colours are resolved on the mobile client (presentation
    /// concern). Free-form colours are intentionally NOT supported.
    /// </summary>
    public static class ShopThemeBackgroundModes
    {
        public const string Default = "light";

        public static readonly IReadOnlySet<string> Allowed = new HashSet<string>(StringComparer.Ordinal)
        {
            "light",
            "themed",
            "dark",
        };

        public static bool IsValid(string? mode)
            => !string.IsNullOrWhiteSpace(mode) && Allowed.Contains(mode.Trim());

        /// <summary>
        /// null/empty → <see cref="Default"/> (<c>light</c>); a known value → that
        /// value (trimmed); an unknown non-empty value → <c>null</c> so the caller
        /// can reject it with a 400 rather than silently saving bad data.
        /// </summary>
        public static string? Normalize(string? mode)
        {
            if (string.IsNullOrWhiteSpace(mode)) return Default;
            var trimmed = mode.Trim();
            return Allowed.Contains(trimmed) ? trimmed : null;
        }
    }
}
