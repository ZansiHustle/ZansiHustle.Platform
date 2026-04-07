using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using ZansiHustle.Shared.Enums.Influencers;

namespace ZansiHustle.Domain.Influencers
{
    public class Influencer
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
        public InfluencerStatus Status { get; set; } = InfluencerStatus.Pending;
        public Guid? AddedByUserId { get; set; }
        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAtUtc { get; set; }

        [Timestamp]
        public byte[] RowVersion { get; set; }

        public virtual ICollection<InfluencerPlatformAccount> PlatformAccounts { get; set; } = new List<InfluencerPlatformAccount>();
    }
}
