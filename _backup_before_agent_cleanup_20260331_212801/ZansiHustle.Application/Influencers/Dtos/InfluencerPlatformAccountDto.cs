using ZansiHustle.Shared.Enums.Common;

namespace ZansiHustle.Application.Influencers.Dtos
{
    /// <summary>
    /// Represents a single influencer platform account.
    /// </summary>
    public class InfluencerPlatformAccountDto
    {
        public PlatformType Platform { get; set; }
        public string? Handle { get; set; }
        public string? Url { get; set; }
        public long FollowersCount { get; set; }
    }
}
