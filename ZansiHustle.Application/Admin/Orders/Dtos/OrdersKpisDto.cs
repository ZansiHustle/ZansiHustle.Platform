namespace ZansiHustle.Application.Admin.Orders.Dtos
{
    /// <summary>
    /// Platform-wide order KPIs for the admin Orders page. Intentionally narrow
    /// (just the four tiles the UI shows) so the controller round-trips the
    /// smallest payload possible — the broader KPI snapshot lives in the
    /// Analytics module.
    /// </summary>
    public class OrdersKpisDto
    {
        public int OrdersToday { get; set; }
        public int OrdersThisWeek { get; set; }
        public int OrdersThisMonth { get; set; }
        public decimal AvgOrderValue { get; set; }
    }
}
