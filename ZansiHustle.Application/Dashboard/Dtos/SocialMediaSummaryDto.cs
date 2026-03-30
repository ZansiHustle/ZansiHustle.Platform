using System.Collections.Generic;

namespace ZansiHustle.Application.Dashboard.Dtos
{
    /// <summary>
    /// Represents social media dashboard summary data.
    /// </summary>
    public class SocialMediaSummaryDto
    {
        public long TotalReach { get; set; }
        public long TotalEngagements { get; set; }
        public long TotalClicks { get; set; }
        public long TotalConversions { get; set; }
        public decimal TotalSpend { get; set; }

        public List<PlatformDistributionDto> PlatformDistribution { get; set; } = new();
        public List<CampaignPerformanceDto> Campaigns { get; set; } = new();
        public List<TrendPointDto> Trends { get; set; } = new();
    }
}
