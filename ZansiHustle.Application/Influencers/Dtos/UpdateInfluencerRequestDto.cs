using System.Collections.Generic;

namespace ZansiHustle.Application.Influencers.Dtos
{
    /// <summary>
    /// Request model used to update an existing influencer.
    /// </summary>
    public class UpdateInfluencerRequestDto
    {
        public string FullName { get; set; } = string.Empty;
        public string? Niche { get; set; }
        public string? Province { get; set; }
        public string? City { get; set; }
        public decimal? Rate { get; set; }
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Notes { get; set; }
        public List<InfluencerPlatformAccountDto> PlatformAccounts { get; set; } = new();
    }
}
