using System;
using ZansiHustle.Domain.Identity;

namespace ZansiHustle.Domain.Referrals
{
    /// <summary>
    /// Affiliate identity for a user (agent, marketplace growth associate,
    /// partner, etc.). One per user. Owns the user-facing referral code
    /// and the rolling counters used by the affiliate dashboard.
    ///
    /// Counters are denormalised increments (NOT recomputed from
    /// UserReferrals on every read) so dashboard queries stay cheap. The
    /// authoritative source for a single relationship is still UserReferral.
    /// </summary>
    public class AffiliateProfile
    {
        public Guid Id { get; set; }

        public Guid UserId { get; set; }
        public virtual User? User { get; set; }

        /// <summary>
        /// URL-safe lowercase slug — the bit that goes after /join/.
        /// Globally unique. Generated once from the user's name and stable
        /// thereafter so existing share-links keep working.
        /// </summary>
        public string ReferralCode { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;

        // Rolling counters incremented by the attribution service.
        public int ClickCount { get; set; }
        public int JoinCount { get; set; }
        public int ConversionCount { get; set; }

        // Optional commission knobs — left null for now; future commission
        // engine reads them when present, falls back to a global default.
        public decimal? CommissionRate { get; set; }
        public string? Tier { get; set; }
        public string? Notes { get; set; }

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAtUtc { get; set; }
    }
}
