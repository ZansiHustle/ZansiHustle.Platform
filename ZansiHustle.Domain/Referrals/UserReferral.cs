using System;
using ZansiHustle.Shared.Enums.Referrals;

namespace ZansiHustle.Domain.Referrals
{
    /// <summary>
    /// One row per referred user. Authoritative record of "who referred
    /// whom" — answers: who referred this user, which code, what kind of
    /// join, when, and where in the lifecycle they sit.
    ///
    /// Snapshot fields (ReferralCodeUsed, ReferrerUserId) intentionally
    /// duplicate AffiliateProfile values so reports remain accurate even
    /// if an affiliate later edits/disables their code.
    /// </summary>
    public class UserReferral
    {
        public Guid Id { get; set; }

        public Guid AffiliateProfileId { get; set; }
        public virtual AffiliateProfile? AffiliateProfile { get; set; }

        public Guid ReferrerUserId { get; set; }
        public Guid ReferredUserId { get; set; }

        public string ReferralCodeUsed { get; set; } = string.Empty;
        public ReferralType ReferralType { get; set; } = ReferralType.Other;
        public ReferralStatus Status { get; set; } = ReferralStatus.Joined;

        // Where the link landed before registration. Useful for UTM-style
        // breakdown and for debugging "why didn't this attribute".
        public string? SourcePath { get; set; }

        public DateTime JoinedAtUtc { get; set; } = DateTime.UtcNow;
        public DateTime? ConvertedAtUtc { get; set; }
        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAtUtc { get; set; }
    }
}
