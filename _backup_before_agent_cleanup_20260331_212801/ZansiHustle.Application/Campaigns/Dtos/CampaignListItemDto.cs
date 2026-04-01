using System;
using ZansiHustle.Shared.Enums.Campaigns;
using ZansiHustle.Shared.Enums.Common;

namespace ZansiHustle.Application.Campaigns.Dtos
{
    /// <summary>
    /// Lightweight campaign DTO for list screens.
    /// </summary>
    public class CampaignListItemDto
    {
        public Guid Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? CampaignType { get; set; }
        public CampaignStatus Status { get; set; }
        public PlatformType? PrimaryPlatform { get; set; }
        public decimal Budget { get; set; }
        public DateTime StartDateUtc { get; set; }
        public DateTime EndDateUtc { get; set; }
        public int ContentTasksCount { get; set; }
        public int MetricSnapshotsCount { get; set; }
        public DateTime CreatedAtUtc { get; set; }
    }
}
