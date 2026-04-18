namespace ZansiHustle.Application.Admin.Affiliates.Dtos
{
    /// <summary>
    /// One datapoint in the monthly affiliate performance trend. <see cref="Month"/>
    /// is a short label (e.g. "Sep") the chart axis renders directly.
    /// </summary>
    public class AffiliatePerformancePointDto
    {
        public string Month { get; set; } = string.Empty;
        public int Referrals { get; set; }
        public int Conversions { get; set; }
        public decimal Commission { get; set; }
    }
}
