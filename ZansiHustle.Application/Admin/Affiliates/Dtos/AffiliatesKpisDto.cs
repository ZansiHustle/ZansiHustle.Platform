namespace ZansiHustle.Application.Admin.Affiliates.Dtos
{
    /// <summary>
    /// Admin Affiliates KPIs. Covers the five tracking metrics the portal
    /// cares about today: counts, referral activity, and commission totals.
    ///
    /// The platform does not yet persist an Affiliate aggregate, so the
    /// repository returns zero values in real-login mode. Once an Affiliate
    /// entity is introduced the repo swaps in real aggregations without
    /// touching the DTO or the UI.
    /// </summary>
    public class AffiliatesKpisDto
    {
        public int TotalAffiliates { get; set; }
        public int ActiveAffiliates { get; set; }
        public int TotalReferrals { get; set; }
        public int TotalConversions { get; set; }
        public decimal TotalCommissions { get; set; }
        public decimal TotalEarnings { get; set; }
        public decimal ConversionRate { get; set; }
    }
}
