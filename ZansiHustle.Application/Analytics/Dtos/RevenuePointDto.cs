namespace ZansiHustle.Application.Analytics.Dtos
{
    /// <summary>
    /// One datapoint in the revenue trend. <see cref="Month"/> is a short
    /// label (e.g. "Sep") — the client renders it directly on chart axes.
    /// </summary>
    public class RevenuePointDto
    {
        public string Month { get; set; } = string.Empty;
        public decimal Revenue { get; set; }
        public int Orders { get; set; }
        public decimal Profit { get; set; }
    }
}
