namespace ZansiHustle.Application.Merchants.Dtos
{
    /// <summary>
    /// Query input for <c>GET /api/merchants/public</c>. Bound via
    /// <c>[FromQuery]</c> on the controller, so every property must be
    /// nullable or have a sensible default — missing query params bind
    /// to default values without an explicit error.
    ///
    /// Defaults applied by the service / repository (NOT here, so a
    /// future caller passing <c>page=0</c> or <c>pageSize=999</c> still
    /// gets sane behaviour):
    ///   • <c>Page &lt;= 0</c> → 1
    ///   • <c>PageSize &lt;= 0</c> → 20
    ///   • <c>PageSize &gt; 100</c> → 100
    ///   • coords supplied without <c>RadiusKm</c> → 25 km
    ///   • coords supplied without <c>Sort</c>     → "nearest"
    ///   • no coords + no <c>Sort</c>              → "rating"
    /// </summary>
    public class MerchantPublicFilterRequestDto
    {
        /// <summary>
        /// Free-text search. Matches against name, description, city,
        /// suburb, and the joined seller-category name with case-
        /// insensitive <c>LIKE %q%</c>.
        /// </summary>
        public string? Q { get; set; }

        /// <summary>Filter by category — matches <c>SellerCategory.Name</c>.</summary>
        public string? Category { get; set; }

        public string? Province { get; set; }
        public string? City { get; set; }

        // Geo — supplied as a pair. Either both or neither; a lone Lat
        // or lone Lng is treated as "no coords" by the repository.
        public decimal? Lat { get; set; }
        public decimal? Lng { get; set; }
        public decimal? RadiusKm { get; set; }

        /// <summary>
        /// One of: <c>nearest</c> (only valid when coords supplied),
        /// <c>rating</c>, <c>newest</c>. Case-insensitive. Unknown values
        /// fall through to the default-by-coords rule.
        /// </summary>
        public string? Sort { get; set; }

        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }
}
