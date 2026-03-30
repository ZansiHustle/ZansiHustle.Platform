using System.Collections.Generic;

namespace ZansiHustle.Application.Podcasts.Dtos
{
    /// <summary>
    /// Request model used to create a new podcast.
    /// </summary>
    public class CreatePodcastRequestDto
    {
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
        public string? Notes { get; set; }
        public System.Guid? AddedByUserId { get; set; }
        public List<PodcastAdFormatDto> AdFormats { get; set; } = new();
    }
}
