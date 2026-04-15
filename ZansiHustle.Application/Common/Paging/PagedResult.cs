using System;
using System.Collections.Generic;

namespace ZansiHustle.Application.Common.Paging
{
    /// <summary>
    /// Generic paged-response envelope returned inside <see cref="Shared.Results.Result{T}"/>
    /// for endpoints that page large collections.
    /// </summary>
    public sealed class PagedResult<T>
    {
        public List<T> Items { get; set; } = new();
        public int Total { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling((double)Total / PageSize);
        public bool HasNextPage => Page < TotalPages;
    }
}
