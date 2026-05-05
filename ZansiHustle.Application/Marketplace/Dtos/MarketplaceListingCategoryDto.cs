namespace ZansiHustle.Application.Marketplace.Dtos
{
    /// <summary>
    /// One category bucket reported by
    /// <c>GET /api/marketplace-listings/categories</c>.
    ///
    /// Only categories with at least one Active listing are returned —
    /// the chip row on the Marketplace tab renders directly from this
    /// payload, so a stale chip can't appear for a category whose only
    /// listing was sold or archived. The buyer never taps a chip and
    /// sees an empty grid.
    /// </summary>
    public sealed class MarketplaceListingCategoryDto
    {
        /// <summary>
        /// Canonical case to render (the trimmed casing of the first
        /// listing in this bucket). The grouping key is case-insensitive
        /// so "Electronics" / "electronics" / " Electronics " collapse
        /// into a single chip.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>Number of currently-Active listings in this bucket.</summary>
        public int Count { get; set; }
    }
}
