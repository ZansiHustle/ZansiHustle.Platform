namespace ZansiHustle.Shared.Queries
{
    /// <summary>
    /// Clamps common paging parameters to safe ranges. Used by every admin
    /// service that accepts a <see cref="PagedListQueryBase"/> so a malicious
    /// client can't request <c>pageSize=1000000</c>.
    /// </summary>
    public static class PagedQueryGuard
    {
        /// <summary>Default max rows per page. Services can override via <see cref="Clamp"/>.</summary>
        public const int DefaultMaxPageSize = 200;

        /// <summary>
        /// Normalizes <paramref name="query"/> in place and returns it for fluent chaining.
        /// If null, a fresh instance with defaults is returned.
        /// </summary>
        public static PagedListQueryBase Clamp(PagedListQueryBase? query, int maxPageSize = DefaultMaxPageSize)
        {
            query ??= new PagedListQueryBase();
            if (query.Page < 1) query.Page = 1;
            if (query.PageSize < 1) query.PageSize = 25;
            if (query.PageSize > maxPageSize) query.PageSize = maxPageSize;
            return query;
        }
    }
}
