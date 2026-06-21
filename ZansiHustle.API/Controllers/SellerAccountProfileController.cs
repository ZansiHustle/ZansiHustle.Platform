using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ZansiHustle.Application.Common.Interfaces.Shared;
using ZansiHustle.Application.Seller.AccountProfile;
using ZansiHustle.Application.Seller.AccountProfile.Dtos;
using ZansiHustle.Shared.Errors;
using ZansiHustle.Shared.Results;

namespace ZansiHustle.API.Controllers
{
    /// <summary>
    /// Seller Account Profile / control centre. The seller account is the
    /// caller's OnlineStore merchant (products / services + visibility / trust).
    /// Distinct from <c>ShopsController</c> (the brandable storefront).
    /// </summary>
    [Route("api/seller/account-profile")]
    [Authorize]
    public class SellerAccountProfileController : BaseController
    {
        private readonly ISellerAccountProfileService _profile;
        private readonly ICurrentUserService _currentUserService;

        public SellerAccountProfileController(
            ISellerAccountProfileService profile,
            ICurrentUserService currentUserService)
        {
            _profile = profile;
            _currentUserService = currentUserService;
        }

        /// <summary>Aggregated seller account profile for the authenticated seller.</summary>
        [HttpGet]
        [ProducesResponseType(typeof(Result<SellerAccountProfileDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetMine()
        {
            var userId = _currentUserService.UserId;
            if (!userId.HasValue)
                return ToActionResult(Result<SellerAccountProfileDto>.Failure(
                    ErrorCodes.Unauthorized, "User identifier not found in token."));

            return ToActionResult(await _profile.GetMyProfileAsync(userId.Value));
        }

        /// <summary>
        /// Pause / resume the seller account's buyer-facing visibility. Body:
        /// { isPaused, reason? }. Pausing hides seller-account listings from
        /// buyers (shops keep their own control); nothing is deleted.
        /// </summary>
        [HttpPut("visibility")]
        [ProducesResponseType(typeof(Result<SellerAccountProfileDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> UpdateVisibility([FromBody] SellerVisibilityRequestDto request)
        {
            var userId = _currentUserService.UserId;
            if (!userId.HasValue)
                return ToActionResult(Result<SellerAccountProfileDto>.Failure(
                    ErrorCodes.Unauthorized, "User identifier not found in token."));

            return ToActionResult(await _profile.SetVisibilityAsync(userId.Value, request?.IsPaused ?? false, request?.Reason));
        }

        /// <summary>
        /// Save non-sensitive public identity (trading name, bio, public location,
        /// profile photo). No review triggered.
        /// </summary>
        [HttpPut("trading-profile")]
        [ProducesResponseType(typeof(Result<SellerAccountProfileDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> UpdateTradingProfile([FromBody] SellerTradingProfileRequestDto request)
        {
            var userId = _currentUserService.UserId;
            if (!userId.HasValue)
                return ToActionResult(Result<SellerAccountProfileDto>.Failure(
                    ErrorCodes.Unauthorized, "User identifier not found in token."));

            return ToActionResult(await _profile.UpdateTradingProfileAsync(userId.Value, request));
        }

        /// <summary>
        /// Save the seller's courier-collection (pickup) address used for
        /// product-order dispatch. Operational/private — not a public change.
        /// </summary>
        [HttpPut("pickup-address")]
        [ProducesResponseType(typeof(Result<SellerAccountProfileDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> UpdatePickupAddress([FromBody] SellerPickupAddressRequestDto request)
        {
            var userId = _currentUserService.UserId;
            if (!userId.HasValue)
                return ToActionResult(Result<SellerAccountProfileDto>.Failure(
                    ErrorCodes.Unauthorized, "User identifier not found in token."));

            return ToActionResult(await _profile.UpdatePickupAddressAsync(userId.Value, request));
        }
    }
}
