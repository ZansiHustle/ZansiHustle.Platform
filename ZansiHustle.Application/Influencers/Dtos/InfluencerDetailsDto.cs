using System;
using System.Collections.Generic;
using ZansiHustle.Shared.Enums.Influencers;

namespace ZansiHustle.Application.Influencers.Dtos
{
    /// <summary>
    /// Detailed influencer DTO for detail screens.
    /// </summary>
    public class InfluencerDetailsDto
    {
        public Guid Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string? Niche { get; set; }
        public string? Province { get; set; }
        public string? City { get; set; }
        public decimal? Rate { get; set; }
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Notes { get; set; }
        public Guid? AddedByUserId { get; set; }
        public InfluencerStatus Status { get; set; }
        public List<InfluencerPlatformAccountDto> PlatformAccounts { get; set; } = new();
        public DateTime CreatedAtUtc { get; set; }
        public DateTime? UpdatedAtUtc { get; set; }
    }
}
