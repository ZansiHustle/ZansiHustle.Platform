using System.Text.RegularExpressions;

namespace ZansiHustle.Domain.Shops
{
    /// <summary>
    /// Canonicalises a shop's display name for uniqueness comparison.
    /// Shop names must be unique across the platform (a separate business
    /// rule from slug uniqueness, which only guarantees stable public URLs).
    ///
    /// Normalisation: trim, collapse any run of internal whitespace to a
    /// single space, and lower-case (invariant). So "Zansi Tech", "zansi tech",
    /// " ZANSI  TECH " all normalise to "zansi tech" and are treated as the
    /// same name. Both the service guard and the repository lookup use THIS
    /// method, so the comparison stays consistent end-to-end.
    /// </summary>
    public static class ShopNames
    {
        public static string Normalize(string? value)
        {
            if (string.IsNullOrWhiteSpace(value)) return string.Empty;
            var collapsed = Regex.Replace(value.Trim(), @"\s+", " ");
            return collapsed.ToLowerInvariant();
        }
    }
}
