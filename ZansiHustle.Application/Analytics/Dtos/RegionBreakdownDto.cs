namespace ZansiHustle.Application.Analytics.Dtos
{
    /// <summary>
    /// Per-province aggregate. "Sellers" and "Shops" are synonymous in v1
    /// (one merchant = one shop) but kept separate so the UI keeps its two
    /// legends and so a future "multi-shop per seller" model fits without a
    /// DTO change.
    /// </summary>
    public class RegionBreakdownDto
    {
        public string Name { get; set; } = string.Empty;
        public int Sellers { get; set; }
        public int Shops { get; set; }
        public decimal Revenue { get; set; }
    }
}
