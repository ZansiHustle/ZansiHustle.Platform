using System;
using System.Threading.Tasks;
using ZansiHustle.Application.Seller.AccountProfile.Dtos;
using ZansiHustle.Shared.Results;

namespace ZansiHustle.Application.Seller.AccountProfile
{
    /// <summary>
    /// Read + visibility operations for the seller's account profile (the
    /// seller's <c>OnlineStore</c> Merchant). Distinct from <c>IShopProfileService</c>
    /// (the brandable storefront) and <c>IMerchantService</c> (admin/CRUD).
    /// </summary>
    public interface ISellerAccountProfileService
    {
        /// <summary>Aggregated account profile for the authenticated seller.</summary>
        Task<Result<SellerAccountProfileDto>> GetMyProfileAsync(Guid ownerUserId);

        /// <summary>
        /// Seller pauses / resumes their seller-account visibility. Pausing hides
        /// SELLER-ACCOUNT listings from buyers (shops keep their own control) —
        /// nothing is deleted, listing statuses are unchanged. Self-resume is
        /// blocked when the account is admin-held (UnderReview / Blocked) or not
        /// yet approved.
        /// </summary>
        Task<Result<SellerAccountProfileDto>> SetVisibilityAsync(Guid ownerUserId, bool isPaused, string? reason);

        /// <summary>
        /// Saves NON-sensitive public identity fields (trading name, bio, public
        /// location, profile photo). These never trigger review; sensitive changes
        /// route through the Verification flow.
        /// </summary>
        Task<Result<SellerAccountProfileDto>> UpdateTradingProfileAsync(Guid ownerUserId, SellerTradingProfileRequestDto request);

        /// <summary>
        /// Saves the seller's courier-collection (pickup) address. Operational
        /// data used for product-order dispatch — not a public-facing change,
        /// so it never triggers review. AddressLine1 is required.
        /// </summary>
        Task<Result<SellerAccountProfileDto>> UpdatePickupAddressAsync(Guid ownerUserId, SellerPickupAddressRequestDto request);
    }
}
