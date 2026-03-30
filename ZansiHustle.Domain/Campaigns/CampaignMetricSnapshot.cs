using System;
using ZansiHustle.Shared.Enums.Common;

namespace ZansiHustle.Domain.Campaigns
{
    public class CampaignMetricSnapshot
    {
        public Guid Id { get; set; }
        public Guid CampaignId { get; set; }
        public virtual Campaign Campaign { get; set; } = null!;
        public DateTime SnapshotDateUtc { get; set; }
        public PlatformType? Platform { get; set; }
        public long Reach { get; set; }
        public long Engagements { get; set; }
        public long Impressions { get; set; }
        public long Clicks { get; set; }
        public long Conversions { get; set; }
        public decimal Spend { get; set; }
        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    }
}
