using ZansiHustle.Shared.Queries;

namespace ZansiHustle.Application.Admin.Listings.Dtos
{
    /// <summary>
    /// Query parameters for the cross-merchant admin Listings grid. Extends the
    /// common paged/status/search/date contract with two listing-specific
    /// filters. Bound via <c>[FromQuery]</c>; every field is optional.
    /// </summary>
    public class AdminListingQuery : PagedListQueryBase
    {
        /// <summary>
        /// Listing type filter. Accepts <c>"product"</c> / <c>"service"</c>;
        /// <c>"all"</c> / null means no type filter.
        /// </summary>
        public string? Type { get; set; }

        /// <summary>
        /// Sales-channel filter. Accepts <c>"selleraccount"</c> /
        /// <c>"shopprofile"</c> / <c>"physicalstore"</c>; <c>"all"</c> / null
        /// means no source filter.
        /// </summary>
        public string? Source { get; set; }
    }
}
