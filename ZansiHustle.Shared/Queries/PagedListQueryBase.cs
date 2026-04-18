using System;

namespace ZansiHustle.Shared.Queries
{
    /// <summary>
    /// Common query parameters for paginated admin list endpoints.
    /// <para>
    /// Bound via <c>[FromQuery]</c>; all fields are optional so a naked GET
    /// (no query string) produces the first page with default size and no
    /// filters applied.
    /// </para>
    /// </summary>
    public class PagedListQueryBase
    {
        /// <summary>1-based page index. Values &lt; 1 are treated as 1.</summary>
        public int Page { get; set; } = 1;

        /// <summary>Rows per page. Services clamp to a sensible range (typically 1..200).</summary>
        public int PageSize { get; set; } = 25;

        /// <summary>Status filter — module-specific string (e.g. "pending", "active", "suspended").</summary>
        public string? Status { get; set; }

        /// <summary>Free-text search term applied over the module's designated searchable fields.</summary>
        public string? Search { get; set; }

        /// <summary>Inclusive date-range lower bound (UTC). Applied against the row's primary date.</summary>
        public DateTime? FromUtc { get; set; }

        /// <summary>Inclusive date-range upper bound (UTC).</summary>
        public DateTime? ToUtc { get; set; }
    }
}
