using System;

namespace ZansiHustle.Domain.Referrals
{
    /// <summary>
    /// One row per /join/{slug} hit. Lightweight, separate from
    /// UserReferral so we can answer "this agent's link gets traffic but
    /// nobody converts" — i.e. compare raw clicks against actual joins.
    ///
    /// IpHash is hashed (not raw IP) for privacy compliance. UserAgent is
    /// truncated to 500 chars on write.
    /// </summary>
    public class ReferralClick
    {
        public Guid Id { get; set; }

        public Guid AffiliateProfileId { get; set; }
        public virtual AffiliateProfile? AffiliateProfile { get; set; }

        public string ReferralCode { get; set; } = string.Empty;
        public string? LandingPath { get; set; }
        public string? UserAgent { get; set; }
        public string? IpHash { get; set; }

        // Set later by the attribution service when this click can be tied
        // to a successful registration (best-effort, may stay null).
        public Guid? ConvertedUserId { get; set; }

        public DateTime ClickedAtUtc { get; set; } = DateTime.UtcNow;
    }
}
