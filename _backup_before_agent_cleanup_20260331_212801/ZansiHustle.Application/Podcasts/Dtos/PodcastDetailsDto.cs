using System;
using System.Collections.Generic;
using ZansiHustle.Shared.Enums.Podcasts;

namespace ZansiHustle.Application.Podcasts.Dtos
{
    /// <summary>
    /// Detailed podcast DTO for detail screens.
    /// </summary>
    public class PodcastDetailsDto
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
        public string? Notes { get; set; }
        public Guid? AddedByUserId { get; set; }
        public PodcastOutreachStatus OutreachStatus { get; set; }
        public PodcastResponseStatus ResponseStatus { get; set; }
        public List<PodcastAdFormatDto> AdFormats { get; set; } = new();
        public DateTime CreatedAtUtc { get; set; }
        public DateTime? UpdatedAtUtc { get; set; }
    }
}
