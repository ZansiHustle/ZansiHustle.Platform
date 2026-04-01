using System.Collections.Generic;

namespace ZansiHustle.Application.Dashboard.Dtos
{
    /// <summary>
    /// Represents marketing intelligence dashboard summary data.
    /// </summary>
    public class MarketingSummaryDto
    {
        public int TotalInfluencers { get; set; }
        public int ApprovedInfluencers { get; set; }
        public long TotalInfluencerReach { get; set; }
        public decimal ApprovedInfluencerSpend { get; set; }
        public int TotalPodcasts { get; set; }
        public int ActiveCampaigns { get; set; }

        public List<PlatformDistributionDto> PlatformDistribution { get; set; } = new();
        public List<ProvinceDistributionDto> InfluencerProvinceDistribution { get; set; } = new();
        public List<CampaignPerformanceDto> Campaigns { get; set; } = new();
        public List<TrendPointDto> Trends { get; set; } = new();
    }
}
