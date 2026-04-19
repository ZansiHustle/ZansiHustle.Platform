using System;

namespace ZansiHustle.Application.Referrals.Dtos
{
    /// <summary>
    /// Public payload returned by /api/affiliates/resolve/{slug}. Contains
    /// only the fields the registration page needs to prefill the form and
    /// show "You were referred by …". No financials, no internal counters.
    /// </summary>
    public class ResolveReferralDto
    {
        public Guid AffiliateProfileId { get; set; }
        public Guid ReferrerUserId { get; set; }
        public string ReferralCode { get; set; } = string.Empty;
        public string? ReferrerDisplayName { get; set; }
        public bool IsActive { get; set; }
    }
}
