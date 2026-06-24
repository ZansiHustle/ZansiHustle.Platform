using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ZansiHustle.Application.Common.Interfaces.Shared;
using ZansiHustle.Application.Common.Paging;
using ZansiHustle.Application.Listings;
using ZansiHustle.Application.Listings.Dtos;
using ZansiHustle.Application.Shops;
using ZansiHustle.Application.Shops.Dtos;
using ZansiHustle.Shared.Enums.Shops;
using System.Collections.Generic;
using System.Linq;
using ZansiHustle.Shared.Errors;
using ZansiHustle.Shared.Results;

namespace ZansiHustle.API.Controllers
{
    /// <summary>
    /// Storefront / shop endpoints. Distinct from
    /// <c>MerchantsController</c>: merchants are the seller's business
    /// account; this controller manages the optional shop profile
    /// layered on top.
    ///
    ///   • Owner endpoints   ( /mine, /mine, PATCH /mine/{id} )      — JWT auth
    ///   • Public endpoints  ( /public, /{id} )                       — anonymous
    ///   • Admin endpoints   ( GET, /{id}/suspend, /{id}/reactivate ) — JWT auth (admin policy added when role gate ships)
    /// </summary>
    [Route("api/[controller]")]
    [Authorize]
    public class ShopsController : BaseController
    {
        private readonly IShopProfileService _shopService;
        private readonly IListingService _listingService;
        private readonly ICurrentUserService _currentUserService;

        public ShopsController(
            IShopProfileService shopService,
            IListingService listingService,
            ICurrentUserService currentUserService)
        {
            _shopService = shopService;
            _listingService = listingService;
            _currentUserService = currentUserService;
        }

        /// <summary>
        /// Attaches the caller's EXISTING listings to their shop storefront.
        /// Body: { productIds: [], serviceIds: [] }. Idempotent + ownership-
        /// enforced in the service (only the caller's own items, only the
        /// shop's merchant, never another seller's listing, no duplicates).
        /// </summary>
        [HttpPost("{shopId:guid}/items")]
        [ProducesResponseType(typeof(Result<AssignShopItemsResultDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> AddItems(Guid shopId, [FromBody] AssignShopItemsRequestDto request)
        {
            var userId = _currentUserService.UserId;
            if (!userId.HasValue)
                return ToActionResult(Result<AssignShopItemsResultDto>.Failure(ErrorCodes.Unauthorized, "User identifier not found in token."));

            var ids = new List<Guid>();
            if (request?.ProductIds is { Count: > 0 }) ids.AddRange(request.ProductIds);
            if (request?.ServiceIds is { Count: > 0 }) ids.AddRange(request.ServiceIds);

            var result = await _listingService.AssignToShopAsync(userId.Value, shopId, ids.Distinct().ToList());
            return ToActionResult(result);
        }

        // ─── Owner ──────────────────────────────────────────────────

        /// <summary>
        /// Returns the caller's shop profile. Success with
        /// <c>data: null</c> when the seller has not yet created a
        /// shop — the app reads that as the empty-state ("Set up
        /// your shop") CTA.
        /// </summary>
        [HttpGet("mine")]
        [ProducesResponseType(typeof(Result<ShopProfileDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetMine()
        {
            var userId = _currentUserService.UserId;
            if (!userId.HasValue)
                return ToActionResult(Result<ShopProfileDto?>.Failure(ErrorCodes.Unauthorized, "User identifier not found in token."));

            var result = await _shopService.GetMineAsync(userId.Value);
            return ToActionResult(result);
        }

        /// <summary>
        /// Returns ALL the caller's shops (owner-facing My Shops list). Normal
        /// sellers get 0 or 1; Admin/SuperAdmin owners may get several. Returns
        /// an empty array (not 404) when the seller has no shop yet. Distinct
        /// from GET /mine, which returns a single primary shop and is still used
        /// by the "do I have a shop?" tri-state across other screens.
        /// </summary>
        [HttpGet("mine/all")]
        [ProducesResponseType(typeof(Result<List<ShopProfileDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAllMine()
        {
            var userId = _currentUserService.UserId;
            if (!userId.HasValue)
                return ToActionResult(Result<List<ShopProfileDto>>.Failure(ErrorCodes.Unauthorized, "User identifier not found in token."));

            var result = await _shopService.GetAllMineAsync(userId.Value);
            return ToActionResult(result);
        }

        /// <summary>
        /// Creates the caller's shop. Service-layer guards:
        ///   • caller must own an Active OnlineStore Merchant (403);
        ///   • only one non-Suspended shop per merchant (409).
        /// During early access the new shop is auto-Active and tagged
        /// <c>EarlyAccess</c>; no admin review.
        /// </summary>
        [HttpPost("mine")]
        [ProducesResponseType(typeof(Result<ShopProfileDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> CreateMine([FromBody] CreateShopRequestDto request)
        {
            var userId = _currentUserService.UserId;
            if (!userId.HasValue)
                return ToActionResult(Result<ShopProfileDto>.Failure(ErrorCodes.Unauthorized, "User identifier not found in token."));

            var result = await _shopService.CreateMineAsync(userId.Value, request);
            return ToActionResult(result);
        }

        /// <summary>
        /// PATCH the caller's shop. Only supplied fields are applied;
        /// omitted fields stay as-is. Slug is immutable after create.
        /// </summary>
        [HttpPatch("mine/{shopId:guid}")]
        [ProducesResponseType(typeof(Result<ShopProfileDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> UpdateMine(Guid shopId, [FromBody] UpdateShopRequestDto request)
        {
            var userId = _currentUserService.UserId;
            if (!userId.HasValue)
                return ToActionResult(Result<ShopProfileDto>.Failure(ErrorCodes.Unauthorized, "User identifier not found in token."));

            var result = await _shopService.UpdateMineAsync(userId.Value, shopId, request);
            return ToActionResult(result);
        }

        // ─── Public ─────────────────────────────────────────────────

        /// <summary>
        /// Public, unauthenticated paged shop search. Returns only
        /// <see cref="ShopProfileStatus.Active"/> rows and the narrow
        /// <see cref="ShopProfilePublicDto"/> projection.
        /// </summary>
        [HttpGet("public")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(Result<PagedResult<ShopProfilePublicDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> SearchPublic(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string? q = null)
        {
            var result = await _shopService.SearchPublicAsync(page, pageSize, q);
            return ToActionResult(result);
        }

        /// <summary>
        /// Public, unauthenticated detail. 404 for non-Active rows —
        /// the existence of Draft / Suspended / PendingReview shops is
        /// never disclosed to public callers.
        /// </summary>
        [HttpGet("{id:guid}")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(Result<ShopProfilePublicDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetPublicById(Guid id)
        {
            // Pass the caller (null when anonymous) so the OWNER can still load
            // their own paused/hidden shop to preview it; buyers get a 404.
            var result = await _shopService.GetPublicByIdAsync(id, _currentUserService.UserId);
            return ToActionResult(result);
        }

        /// <summary>
        /// Owner: pause / resume this shop's buyer-facing visibility. Body:
        /// { isPaused, reason? }. Pausing hides the shop + its attached listings
        /// from buyers without deleting anything. Self-resume is blocked when the
        /// shop is admin-held (UnderReview / Blocked).
        /// </summary>
        [HttpPut("mine/{shopId:guid}/visibility")]
        [ProducesResponseType(typeof(Result<ShopProfileDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> UpdateMyVisibility(Guid shopId, [FromBody] ShopVisibilityRequestDto request)
        {
            var userId = _currentUserService.UserId;
            if (!userId.HasValue)
                return ToActionResult(Result<ShopProfileDto>.Failure(ErrorCodes.Unauthorized, "User identifier not found in token."));

            var result = await _shopService.UpdateVisibilityAsync(userId.Value, shopId, request?.IsPaused ?? false, request?.Reason);
            return ToActionResult(result);
        }

        // ─── Admin ──────────────────────────────────────────────────

        /// <summary>
        /// Admin paged list. Powers Portal <c>/shops</c>. All statuses
        /// returned; optional <c>status</c> filter narrows.
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(Result<PagedResult<ShopProfileDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> SearchAdmin(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] ShopProfileStatus? status = null,
            [FromQuery] string? q = null)
        {
            var result = await _shopService.SearchAdminAsync(page, pageSize, status, q);
            return ToActionResult(result);
        }

        /// <summary>Admin: suspend a shop. Hides it from public surfaces.</summary>
        [HttpPost("{id:guid}/suspend")]
        [ProducesResponseType(typeof(Result<ShopProfileDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> Suspend(Guid id, [FromBody] SuspendShopRequestDto? request)
        {
            var result = await _shopService.SuspendAsync(id, request?.Reason);
            return ToActionResult(result);
        }

        /// <summary>Admin: re-activate a previously-Suspended shop.</summary>
        [HttpPost("{id:guid}/reactivate")]
        [ProducesResponseType(typeof(Result<ShopProfileDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> Reactivate(Guid id)
        {
            var result = await _shopService.ReactivateAsync(id);
            return ToActionResult(result);
        }
    }
}
