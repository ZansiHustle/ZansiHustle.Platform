using System;

namespace ZansiHustle.Application.Referrals.Dtos
{
    /// <summary>Public-facing affiliate profile (full).</summary>
    public class AffiliateProfileDto
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string? UserDisplayName { get; set; }
        public string ReferralCode { get; set; } = string.Empty;
        public string ReferralLink { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public int ClickCount { get; set; }
        public int JoinCount { get; set; }
        public int ConversionCount { get; set; }
        public decimal? CommissionRate { get; set; }
        public string? Tier { get; set; }
        public string? Notes { get; set; }
        public DateTime CreatedAtUtc { get; set; }
        public DateTime? UpdatedAtUtc { get; set; }
    }
}
