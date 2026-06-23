namespace ZansiHustle.Application.Admin.Listings.Dtos
{
    /// <summary>
    /// Platform-wide listing KPIs for the admin Listings page. Intentionally
    /// narrow (just the tiles the UI shows) so the controller round-trips the
    /// smallest payload possible.
    /// </summary>
    public class AdminListingsKpisDto
    {
        public int TotalListings { get; set; }
        public int Products { get; set; }
        public int Services { get; set; }
        public int Active { get; set; }
        public int Draft { get; set; }
    }
}
