namespace ZansiHustle.Application.Dashboard.Dtos
{
    /// <summary>
    /// Represents a province-based metric breakdown.
    /// </summary>
    public class ProvinceDistributionDto
    {
        public string Province { get; set; } = string.Empty;
        public int Count { get; set; }
    }
}
