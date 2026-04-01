namespace ZansiHustle.Application.Dashboard.Dtos
{
    /// <summary>
    /// Represents a platform-based metric breakdown.
    /// </summary>
    public class PlatformDistributionDto
    {
        public string Platform { get; set; } = string.Empty;
        public int Count { get; set; }
        public long TotalFollowers { get; set; }
        public decimal TotalSpend { get; set; }
    }
}
