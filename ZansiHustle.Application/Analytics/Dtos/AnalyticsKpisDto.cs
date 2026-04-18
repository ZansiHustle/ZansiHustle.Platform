namespace ZansiHustle.Application.Analytics.Dtos
{
    /// <summary>
    /// Admin analytics KPI snapshot. Shape mirrors the pre-launch mock payload
    /// so the portal consumes it without a client-side mapping layer. Fields
    /// not yet tracked in the current schema (affiliates, tickets, disputes,
    /// platform profit) return zero rather than null — the UI treats "0" as a
    /// valid empty state, whereas null requires special casing.
    /// </summary>
    public class AnalyticsKpisDto
    {
        public decimal TotalGMV { get; set; }
        public decimal NetRevenue { get; set; }
        public decimal PlatformProfit { get; set; }
        public decimal PendingPayouts { get; set; }

        public int ActiveSellers { get; set; }
        public int ActiveShops { get; set; }
        public int ActiveAffiliates { get; set; }

        public int OpenTickets { get; set; }
        public int UnresolvedDisputes { get; set; }

        public int OrdersToday { get; set; }
        public int OrdersThisWeek { get; set; }
        public int OrdersThisMonth { get; set; }

        public int TotalCustomers { get; set; }
        public decimal ConversionRate { get; set; }
        public decimal AvgOrderValue { get; set; }
        public decimal ReturningCustomerRate { get; set; }
    }
}
