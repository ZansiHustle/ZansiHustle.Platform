using System;

namespace ZansiHustle.Application.Dashboard.Dtos
{
    /// <summary>
    /// Represents campaign performance on the dashboard.
    /// </summary>
    public class CampaignPerformanceDto
    {
        public Guid Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? CampaignType { get; set; }
        public string Status { get; set; } = string.Empty;
        public decimal Budget { get; set; }
        public long Reach { get; set; }
        public long Engagements { get; set; }
        public long Clicks { get; set; }
        public long Conversions { get; set; }
        public decimal Spend { get; set; }
    }
}
