namespace ZansiHustle.Shared.Queries
{
    /// <summary>
    /// Tickets-specific paged list query. Adds <see cref="Priority"/> on top
    /// of the shared filter set.
    /// </summary>
    public class TicketsListQuery : PagedListQueryBase
    {
        /// <summary>Priority filter — e.g. "high", "medium", "low".</summary>
        public string? Priority { get; set; }
    }
}
