using System;
using ZansiHustle.Shared.Enums.Referrals;

namespace ZansiHustle.Application.Referrals.Dtos
{
    /// <summary>
    /// Called by registration / merchant onboarding after a successful join
    /// to attribute the new user to an affiliate code.
    ///
    /// ReferredUserId is taken from the JWT (current user) by the controller
    /// — the client never supplies it, so a malicious caller can't forge a
    /// referral for someone else. The attribution service is idempotent: a
    /// second call with the same (ReferredUserId, ReferralType) is a no-op.
    /// </summary>
    public class RecordReferralRequestDto
    {
        public string ReferralCode { get; set; } = string.Empty;
        public ReferralType ReferralType { get; set; } = ReferralType.Other;
        public string? SourcePath { get; set; }
    }
}
