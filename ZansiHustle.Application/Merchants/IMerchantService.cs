using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ZansiHustle.Application.Common.Paging;
using ZansiHustle.Application.Merchants.Dtos;
using ZansiHustle.Shared.Results;

namespace ZansiHustle.Application.Merchants
{
    /// <summary>
    /// Service contract for merchant operations.
    /// </summary>
    public interface IMerchantService
    {
        Task<Result<List<MerchantDto>>> GetAllAsync();
        Task<Result<MerchantDto>> GetByIdAsync(Guid id);

        /// <summary>
        /// Public, unauthenticated discovery — backs the Store Locator /
        /// Nearby tab. Returns only Active merchants and projects them
        /// through <see cref="MerchantPublicDto"/> so bank, KYC, payout,
        /// referral, contact-email, owner-id and revenue fields are
        /// never exposed.
        /// </summary>
        Task<Result<PagedResult<MerchantPublicDto>>> SearchPublicAsync(
            MerchantPublicFilterRequestDto filter);

        /// <summary>
        /// Public, unauthenticated detail. Returns 404
        /// (<see cref="Shared.Errors.ErrorCodes.NotFound"/>) if the
        /// merchant does not exist OR <c>Status != Active</c> — the
        /// existence of Pending/Suspended merchants is never disclosed.
        /// </summary>
        Task<Result<MerchantPublicDto>> GetPublicByIdAsync(
            Guid id,
            decimal? lat = null,
            decimal? lng = null);

        Task<Result<MerchantDto>> CreateAsync(CreateMerchantRequestDto request);
        Task<Result<MerchantDto>> UpdateAsync(Guid id, UpdateMerchantRequestDto request);
        Task<Result<MerchantDto>> VerifyKycAsync(Guid id);
        Task<Result<MerchantDto>> UpdatePayoutEligibilityAsync(Guid id, bool eligible);
        Task<Result<MerchantDto>> ApproveAsync(Guid id);
        Task<Result<MerchantDto>> RejectAsync(Guid id, string? reason = null);
        Task<Result> DeleteAsync(Guid id);

        // Seller self-service (owned shops) — ownership derived from JWT.
        Task<Result<List<MerchantDto>>> GetMineAsync(Guid ownerUserId);
        Task<Result<MerchantDto>> CreateMineAsync(Guid ownerUserId, CreateMyMerchantRequestDto request);
        Task<Result<MerchantDto>> UpdateMineAsync(Guid ownerUserId, Guid merchantId, UpdateMyMerchantRequestDto request);
        /// <summary>
        /// Update only the bank/payout details on a merchant the caller
        /// owns. Keeps bank updates separate from profile updates so the
        /// two paths never clobber each other. Any change resets
        /// IsBankVerified → false so payouts pause until re-verification.
        /// </summary>
        Task<Result<MerchantDto>> UpdateMyBankAsync(Guid ownerUserId, Guid merchantId, UpdateMyBankRequestDto request);
        Task<Result> DeleteMineAsync(Guid ownerUserId, Guid merchantId);
    }
}
