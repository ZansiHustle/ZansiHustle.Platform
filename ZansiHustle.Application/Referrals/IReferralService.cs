using System;
using System.Threading.Tasks;
using ZansiHustle.Application.Referrals.Dtos;
using ZansiHustle.Shared.Results;

namespace ZansiHustle.Application.Referrals
{
    /// <summary>
    /// One service for the full standalone referral / affiliate system. Owns:
    ///  - affiliate identity (auto-create on first request, slug generation)
    ///  - public slug resolution for /join/{slug}
    ///  - click + join attribution and counter increments
    /// </summary>
    public interface IReferralService
    {
        /// <summary>
        /// Returns the signed-in user's affiliate profile, creating one
        /// (with a generated slug) on first call. Idempotent.
        /// </summary>
        Task<Result<AffiliateProfileDto>> GetOrCreateMineAsync();

        /// <summary>Admin lookup.</summary>
        Task<Result<AffiliateProfileDto>> GetByIdAsync(Guid id);

        /// <summary>
        /// Public — used by /join/{slug}. Returns 404 if unknown OR if the
        /// profile is inactive (we deliberately don't expose existence of
        /// inactive codes).
        /// </summary>
        Task<Result<ResolveReferralDto>> ResolveBySlugAsync(string slug);

        /// <summary>Records a click against an affiliate code. Public.</summary>
        Task<Result> RecordClickAsync(RecordReferralClickRequestDto request, string? ipHash);

        /// <summary>
        /// Records a referral relationship for the current authenticated user
        /// (the referred user). Idempotent per (ReferredUserId, ReferralType).
        /// Returns the created or existing UserReferral id.
        /// </summary>
        Task<Result<Guid>> RecordReferralAsync(Guid referredUserId, RecordReferralRequestDto request);
    }
}
