using System;
using ZansiHustle.Shared.Enums.Campaigns;
using ZansiHustle.Shared.Enums.Common;

namespace ZansiHustle.Application.Campaigns.Dtos
{
    /// <summary>
    /// Request model used to update an existing campaign.
    /// </summary>
    public class UpdateCampaignRequestDto
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? CampaignType { get; set; }
        public CampaignStatus Status { get; set; }
        public PlatformType? PrimaryPlatform { get; set; }
        public decimal Budget { get; set; }
        public DateTime StartDateUtc { get; set; }
        public DateTime EndDateUtc { get; set; }
    }
}
