using System;
using ZansiHustle.Shared.Enums.Common;

namespace ZansiHustle.Domain.Influencers
{
    public class InfluencerPlatformAccount
    {
        public Guid Id { get; set; }
        public Guid InfluencerId { get; set; }
        public virtual Influencer Influencer { get; set; } = null!;
        public PlatformType Platform { get; set; }
        public string? Handle { get; set; }
        public string? Url { get; set; }
        public long FollowersCount { get; set; }
        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAtUtc { get; set; }
    }
}
