using System;
using System.Collections.Generic;
using ZansiHustle.Shared.Enums.Podcasts;

namespace ZansiHustle.Domain.Podcasts
{
    public class Podcast
    {
        public Guid Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? HostName { get; set; }
        public string? Country { get; set; }
        public string? Region { get; set; }
        public string? Niche { get; set; }
        public long? AudienceSize { get; set; }
        public string? WebsiteUrl { get; set; }
        public string? ContactEmail { get; set; }
        public string? MediaKitUrl { get; set; }
        public bool AllowsGuestAppearance { get; set; }
        public bool AllowsSponsoredSegments { get; set; }
        public decimal? QuotedPrice { get; set; }
        public PodcastOutreachStatus OutreachStatus { get; set; } = PodcastOutreachStatus.NotStarted;
        public PodcastResponseStatus ResponseStatus { get; set; } = PodcastResponseStatus.NoResponse;
        public string? Notes { get; set; }
        public Guid? AddedByUserId { get; set; }
        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAtUtc { get; set; }

        public virtual ICollection<PodcastAdFormat> AdFormats { get; set; } = new List<PodcastAdFormat>();
    }
}
