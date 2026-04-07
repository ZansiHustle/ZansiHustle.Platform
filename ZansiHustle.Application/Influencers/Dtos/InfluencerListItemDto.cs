using System;
using System.Collections.Generic;
using ZansiHustle.Shared.Enums.Influencers;

namespace ZansiHustle.Application.Influencers.Dtos
{
    /// <summary>
    /// Lightweight influencer DTO for list screens.
    /// </summary>
    public class InfluencerListItemDto
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
        public InfluencerStatus Status { get; set; }
        public long TotalFollowers { get; set; }
        public List<InfluencerPlatformAccountDto> PlatformAccounts { get; set; } = new();
        public Guid? AddedByUserId { get; set; }
        public string? AddedByUserFullname { get; set; }
        public DateTime CreatedAtUtc { get; set; }
    }
}
