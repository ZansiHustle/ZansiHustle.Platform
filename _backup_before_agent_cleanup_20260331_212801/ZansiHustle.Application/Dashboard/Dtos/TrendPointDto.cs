namespace ZansiHustle.Application.Dashboard.Dtos
{
    /// <summary>
    /// Represents a time-based trend point.
    /// </summary>
    public class TrendPointDto
    {
        public string Label { get; set; } = string.Empty;
        public int Count { get; set; }
        public long Reach { get; set; }
        public decimal Spend { get; set; }
    }
}
