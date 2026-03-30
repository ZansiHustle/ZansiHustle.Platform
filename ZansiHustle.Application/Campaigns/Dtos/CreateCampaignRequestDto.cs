using System;
using ZansiHustle.Shared.Enums.Common;

namespace ZansiHustle.Application.Campaigns.Dtos
{
    /// <summary>
    /// Request model used to create a new campaign.
    /// </summary>
    public class CreateCampaignRequestDto
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? CampaignType { get; set; }
        public PlatformType? PrimaryPlatform { get; set; }
        public decimal Budget { get; set; }
        public DateTime StartDateUtc { get; set; }
        public DateTime EndDateUtc { get; set; }
        public Guid? CreatedByUserId { get; set; }
    }
}
