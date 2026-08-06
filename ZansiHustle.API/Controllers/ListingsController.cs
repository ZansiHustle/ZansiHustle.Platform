using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ZansiHustle.Application.Common.Interfaces.Shared;
using ZansiHustle.Application.Common.Paging;
using ZansiHustle.Application.Listings;
using ZansiHustle.Application.Listings.Dtos;
using ZansiHustle.Shared.Errors;
using ZansiHustle.Shared.Results;

namespace ZansiHustle.API.Controllers
{
    /// <summary>
    /// Exposes marketplace listing (product + service) endpoints.
    /// Read endpoints are filtered/paged; write endpoints are scoped to the
    /// authenticated seller via their JWT.
    /// </summary>
    [Route("api/[controller]")]
    [Authorize]
    public class ListingsController : BaseController
    {
        private readonly IListingService _listingService;
        private readonly ICurrentUserService _currentUserService;

        public ListingsController(IListingService listingService, ICurrentUserService currentUserService)
        {
            _listingService = listingService;
            _currentUserService = currentUserService;
        }

        /// <summary>
        /// Searches listings with filters, paging, and sorting.
        /// Anonymous: buyers (including guest / signed-out visitors) browse
        /// the product & service catalogue without an account. Write actions
        /// (create/update/delete) below remain seller-scoped via JWT.
        /// </summary>
        [HttpGet]
        [AllowAnonymous]
        [ProducesResponseType(typeof(Result<PagedResult<ListingListItemDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> Search([FromQuery] ListingFilterRequestDto filter)
        {
            var result = await _listingService.SearchAsync(filter);
            return ToActionResult(result);
        }

        /// <summary>
        /// Gets the listings owned by the current authenticated user (across all their shops).
        /// </summary>
        [HttpGet("mine")]
        [ProducesResponseType(typeof(Result<List<ListingListItemDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetMine()
        {
            var userId = _currentUserService.UserId;

            if (!userId.HasValue)
                return ToActionResult(Result<List<ListingListItemDto>>.Failure(ErrorCodes.Unauthorized, "User identifier not found in token."));

            var result = await _listingService.GetMineAsync(userId.Value);
            return ToActionResult(result);
        }

        /// <summary>
        /// Gets a listing by identifier.
        /// Anonymous: the product / service detail page is buyer-facing and
        /// reachable by guest visitors (App Store Guideline 5.1.1(v) — browse
        /// without registration).
        /// </summary>
        [HttpGet("{id:guid}")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(Result<ListingDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetById(Guid id)
        {
            var result = await _listingService.GetByIdAsync(id);
            return ToActionResult(result);
        }

        /// <summary>
        /// Lists listings explicitly attached to a ShopProfile. Used
        /// by the public ShopProfile catalog so SellerAccount listings
        /// under the same merchant don't appear on the shop's page.
        /// Anonymous: shop profiles are buyer-facing.
        /// </summary>
        [HttpGet("by-shop/{shopProfileId:guid}")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(Result<List<ListingListItemDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetByShopProfile(Guid shopProfileId)
        {
            var result = await _listingService.GetByShopProfileAsync(shopProfileId);
            return ToActionResult(result);
        }

        /// <summary>
        /// Creates a listing in a shop owned by the current authenticated user.
        /// </summary>
        [HttpPost]
        [ProducesResponseType(typeof(Result<ListingDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> Create([FromBody] CreateListingRequestDto request)
        {
            var userId = _currentUserService.UserId;

            if (!userId.HasValue)
                return ToActionResult(Result<ListingDto>.Failure(ErrorCodes.Unauthorized, "User identifier not found in token."));

            var result = await _listingService.CreateAsync(userId.Value, request);
            return ToActionResult(result);
        }

        /// <summary>
        /// Updates a listing owned (via shop) by the current authenticated user.
        /// </summary>
        [HttpPut("{id:guid}")]
        [ProducesResponseType(typeof(Result<ListingDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateListingRequestDto request)
        {
            var userId = _currentUserService.UserId;

            if (!userId.HasValue)
                return ToActionResult(Result<ListingDto>.Failure(ErrorCodes.Unauthorized, "User identifier not found in token."));

            var result = await _listingService.UpdateAsync(userId.Value, id, request);
            return ToActionResult(result);
        }

        /// <summary>
        /// Deletes a listing owned (via shop) by the current authenticated user.
        /// </summary>
        [HttpDelete("{id:guid}")]
        [ProducesResponseType(typeof(Result), StatusCodes.Status200OK)]
        public async Task<IActionResult> Delete(Guid id)
        {
            var userId = _currentUserService.UserId;

            if (!userId.HasValue)
                return ToActionResult(Result.Failure(ErrorCodes.Unauthorized, "User identifier not found in token."));

            var result = await _listingService.DeleteAsync(userId.Value, id);
            return ToActionResult(result);
        }
    }
}
