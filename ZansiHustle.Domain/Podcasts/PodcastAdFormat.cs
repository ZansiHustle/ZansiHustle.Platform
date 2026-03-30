using System;

namespace ZansiHustle.Domain.Podcasts
{
    public class PodcastAdFormat
    {
        public Guid Id { get; set; }
        public Guid PodcastId { get; set; }
        public virtual Podcast Podcast { get; set; } = null!;
        public string FormatName { get; set; } = string.Empty;
        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    }
}
