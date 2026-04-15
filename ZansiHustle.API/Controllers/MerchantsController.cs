using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ZansiHustle.Application.Common.Interfaces.Shared;
using ZansiHustle.Application.Listings;
using ZansiHustle.Application.Listings.Dtos;
using ZansiHustle.Application.Merchants;
using ZansiHustle.Application.Merchants.Dtos;
using ZansiHustle.Shared.Errors;
using ZansiHustle.Shared.Results;

namespace ZansiHustle.API.Controllers
{
    /// <summary>
    /// Exposes merchant (shop) management endpoints.
    /// Admin-style endpoints operate on any merchant; <c>/mine</c> endpoints are
    /// scoped to shops owned by the current authenticated user.
    /// </summary>
    [Route("api/[controller]")]
    [Authorize]
    public class MerchantsController : BaseController
    {
        private readonly IMerchantService _merchantService;
        private readonly IListingService _listingService;
        private readonly ICurrentUserService _currentUserService;

        public MerchantsController(IMerchantService merchantService, IListingService listingService, ICurrentUserService currentUserService)
        {
            _merchantService = merchantService;
            _listingService = listingService;
            _currentUserService = currentUserService;
        }

        /// <summary>
        /// Gets all merchants (admin/public discovery).
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(Result<List<MerchantDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAll()
        {
            var result = await _merchantService.GetAllAsync();
            return ToActionResult(result);
        }

        /// <summary>
        /// Gets the merchants (shops) owned by the current authenticated user.
        /// </summary>
        [HttpGet("mine")]
        [ProducesResponseType(typeof(Result<List<MerchantDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetMine()
        {
            var userId = _currentUserService.UserId;

            if (!userId.HasValue)
                return ToActionResult(Result<List<MerchantDto>>.Failure(ErrorCodes.Unauthorized, "User identifier not found in token."));

            var result = await _merchantService.GetMineAsync(userId.Value);
            return ToActionResult(result);
        }

        /// <summary>
        /// Gets a merchant by identifier.
        /// </summary>
        [HttpGet("{id:guid}")]
        [ProducesResponseType(typeof(Result<MerchantDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetById(Guid id)
        {
            var result = await _merchantService.GetByIdAsync(id);
            return ToActionResult(result);
        }

        /// <summary>
        /// Creates a merchant (admin).
        /// </summary>
        [HttpPost]
        [ProducesResponseType(typeof(Result<MerchantDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> Create([FromBody] CreateMerchantRequestDto request)
        {
            var result = await _merchantService.CreateAsync(request);
            return ToActionResult(result);
        }

        /// <summary>
        /// Creates a shop owned by the current authenticated user.
        /// </summary>
        [HttpPost("mine")]
        [ProducesResponseType(typeof(Result<MerchantDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> CreateMine([FromBody] CreateMyMerchantRequestDto request)
        {
            var userId = _currentUserService.UserId;

            if (!userId.HasValue)
                return ToActionResult(Result<MerchantDto>.Failure(ErrorCodes.Unauthorized, "User identifier not found in token."));

            var result = await _merchantService.CreateMineAsync(userId.Value, request);
            return ToActionResult(result);
        }

        /// <summary>
        /// Updates a merchant (admin).
        /// </summary>
        [HttpPut("{id:guid}")]
        [ProducesResponseType(typeof(Result<MerchantDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateMerchantRequestDto request)
        {
            var result = await _merchantService.UpdateAsync(id, request);
            return ToActionResult(result);
        }

        /// <summary>
        /// Updates a shop owned by the current authenticated user.
        /// </summary>
        [HttpPut("mine/{id:guid}")]
        [ProducesResponseType(typeof(Result<MerchantDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> UpdateMine(Guid id, [FromBody] UpdateMyMerchantRequestDto request)
        {
            var userId = _currentUserService.UserId;

            if (!userId.HasValue)
                return ToActionResult(Result<MerchantDto>.Failure(ErrorCodes.Unauthorized, "User identifier not found in token."));

            var result = await _merchantService.UpdateMineAsync(userId.Value, id, request);
            return ToActionResult(result);
        }

        /// <summary>
        /// Deletes a merchant (admin).
        /// </summary>
        [HttpDelete("{id:guid}")]
        [ProducesResponseType(typeof(Result), StatusCodes.Status200OK)]
        public async Task<IActionResult> Delete(Guid id)
        {
            var result = await _merchantService.DeleteAsync(id);
            return ToActionResult(result);
        }

        /// <summary>
        /// Deletes a shop owned by the current authenticated user.
        /// </summary>
        [HttpDelete("mine/{id:guid}")]
        [ProducesResponseType(typeof(Result), StatusCodes.Status200OK)]
        public async Task<IActionResult> DeleteMine(Guid id)
        {
            var userId = _currentUserService.UserId;

            if (!userId.HasValue)
                return ToActionResult(Result.Failure(ErrorCodes.Unauthorized, "User identifier not found in token."));

            var result = await _merchantService.DeleteMineAsync(userId.Value, id);
            return ToActionResult(result);
        }

        /// <summary>
        /// Gets the listings offered by a specific shop.
        /// </summary>
        [HttpGet("{merchantId:guid}/listings")]
        [ProducesResponseType(typeof(Result<List<ListingListItemDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetListings(Guid merchantId)
        {
            var result = await _listingService.GetByMerchantAsync(merchantId);
            return ToActionResult(result);
        }

        /// <summary>
        /// Verifies merchant KYC (admin).
        /// </summary>
        [HttpPost("{id:guid}/verify-kyc")]
        [ProducesResponseType(typeof(Result<MerchantDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> VerifyKyc(Guid id)
        {
            var result = await _merchantService.VerifyKycAsync(id);
            return ToActionResult(result);
        }

        /// <summary>
        /// Updates merchant payout eligibility (admin).
        /// </summary>
        [HttpPatch("{id:guid}/payout-eligible")]
        [ProducesResponseType(typeof(Result<MerchantDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> UpdatePayoutEligibility(Guid id, [FromBody] UpdateMerchantPayoutEligibilityRequestDto request)
        {
            var result = await _merchantService.UpdatePayoutEligibilityAsync(id, request.Eligible);
            return ToActionResult(result);
        }
    }
}
