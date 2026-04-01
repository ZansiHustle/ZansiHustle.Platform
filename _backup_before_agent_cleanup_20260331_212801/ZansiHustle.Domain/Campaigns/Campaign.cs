using System;
using System.Collections.Generic;
using ZansiHustle.Domain.ContentTasks;
using ZansiHustle.Shared.Enums.Campaigns;
using ZansiHustle.Shared.Enums.Common;

namespace ZansiHustle.Domain.Campaigns
{
    public class Campaign
    {
        public Guid Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? CampaignType { get; set; }
        public CampaignStatus Status { get; set; } = CampaignStatus.Planned;
        public PlatformType? PrimaryPlatform { get; set; }
        public decimal Budget { get; set; }
        public DateTime StartDateUtc { get; set; }
        public DateTime EndDateUtc { get; set; }
        public Guid? CreatedByUserId { get; set; }
        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAtUtc { get; set; }

        public virtual ICollection<CampaignMetricSnapshot> MetricSnapshots { get; set; } = new List<CampaignMetricSnapshot>();
        public virtual ICollection<ContentTask> ContentTasks { get; set; } = new List<ContentTask>();
    }
}
