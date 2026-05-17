using System;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ZansiHustle.Application.Common.Interfaces.Shared;
using ZansiHustle.Application.Engagement;
using ZansiHustle.Application.Engagement.Dtos;
using ZansiHustle.Application.Listings.Dtos;
using ZansiHustle.Application.Marketplace.Dtos;
using ZansiHustle.Shared.Errors;
using ZansiHustle.Shared.Results;

namespace ZansiHustle.API.Controllers
{
    /// <summary>
    /// Buyer-engagement endpoints — like / unlike normal listings,
    /// like / unlike casual marketplace listings, follow / unfollow
    /// shop profiles, save / unsave physical-store merchants, plus
    /// the matching "what have I engaged with" list endpoints.
    ///
    /// All routes require an authenticated caller; the class-level
    /// <c>[Authorize]</c> attribute drives that. There is no
    /// <c>[AllowAnonymous]</c> on any action — engagement state is
    /// inherently per-user.
    ///
    /// Mutation responses are uniform: a
    /// <see cref="EngagementToggleResultDto"/> carrying the new
    /// <c>Active</c> flag (true after POST, false after DELETE) and
    /// the parent's denormalised <c>Count</c> so the client can update
    /// its card badge without a follow-up GET.
    /// </summary>
    [ApiController]
    [Authorize]
    public class EngagementController : BaseController
    {
        private readonly IEngagementService _engagement;
        private readonly ICurrentUserService _currentUser;

        public EngagementController(IEngagementService engagement, ICurrentUserService currentUser)
        {
            _engagement = engagement;
            _currentUser = currentUser;
        }

        private bool TryGetUserId(out Guid userId)
        {
            userId = _currentUser.UserId ?? Guid.Empty;
            return userId != Guid.Empty;
        }

        // ─────────────────────────────────────────────────────────────────
        // Listings — POST/DELETE /api/listings/{id}/like + GET /api/me/liked-listings
        // ─────────────────────────────────────────────────────────────────

        [HttpPost("api/listings/{listingId:guid}/like")]
        public async Task<IActionResult> LikeListing(Guid listingId)
        {
            if (!TryGetUserId(out var userId))
                return ToActionResult(Result<EngagementToggleResultDto>.Failure(ErrorCodes.Unauthorized, "Sign in to like listings."));
            return ToActionResult(await _engagement.LikeListingAsync(userId, listingId));
        }

        [HttpDelete("api/listings/{listingId:guid}/like")]
        public async Task<IActionResult> UnlikeListing(Guid listingId)
        {
            if (!TryGetUserId(out var userId))
                return ToActionResult(Result<EngagementToggleResultDto>.Failure(ErrorCodes.Unauthorized, "Sign in to manage likes."));
            return ToActionResult(await _engagement.UnlikeListingAsync(userId, listingId));
        }

        [HttpGet("api/me/liked-listings")]
        public async Task<IActionResult> MyLikedListings([FromQuery] int page = 1, [FromQuery] int pageSize = 30)
        {
            if (!TryGetUserId(out var userId))
                return ToActionResult(Result<System.Collections.Generic.List<ListingListItemDto>>.Failure(ErrorCodes.Unauthorized, "Sign in to view your liked items."));
            return ToActionResult(await _engagement.GetMyLikedListingsAsync(userId, page, pageSize));
        }

        // ─────────────────────────────────────────────────────────────────
        // Marketplace — POST/DELETE /api/marketplace-listings/{id}/like + GET /api/me/liked-marketplace-listings
        // ─────────────────────────────────────────────────────────────────

        [HttpPost("api/marketplace-listings/{id:guid}/like")]
        public async Task<IActionResult> LikeMarketplace(Guid id)
        {
            if (!TryGetUserId(out var userId))
                return ToActionResult(Result<EngagementToggleResultDto>.Failure(ErrorCodes.Unauthorized, "Sign in to like marketplace items."));
            return ToActionResult(await _engagement.LikeMarketplaceListingAsync(userId, id));
        }

        [HttpDelete("api/marketplace-listings/{id:guid}/like")]
        public async Task<IActionResult> UnlikeMarketplace(Guid id)
        {
            if (!TryGetUserId(out var userId))
                return ToActionResult(Result<EngagementToggleResultDto>.Failure(ErrorCodes.Unauthorized, "Sign in to manage likes."));
            return ToActionResult(await _engagement.UnlikeMarketplaceListingAsync(userId, id));
        }

        [HttpGet("api/me/liked-marketplace-listings")]
        public async Task<IActionResult> MyLikedMarketplace([FromQuery] int page = 1, [FromQuery] int pageSize = 30)
        {
            if (!TryGetUserId(out var userId))
                return ToActionResult(Result<System.Collections.Generic.List<MarketplaceListingDto>>.Failure(ErrorCodes.Unauthorized, "Sign in to view your liked items."));
            return ToActionResult(await _engagement.GetMyLikedMarketplaceListingsAsync(userId, page, pageSize));
        }

        // ─────────────────────────────────────────────────────────────────
        // Shops — POST/DELETE /api/shops/{id}/follow + GET /api/me/followed-shops
        //
        // Deliberately not under ShopsController so all engagement routes
        // sit in one file (easier to audit for auth + uniform response
        // shape).
        // ─────────────────────────────────────────────────────────────────

        [HttpPost("api/shops/{shopId:guid}/follow")]
        public async Task<IActionResult> FollowShop(Guid shopId)
        {
            if (!TryGetUserId(out var userId))
                return ToActionResult(Result<EngagementToggleResultDto>.Failure(ErrorCodes.Unauthorized, "Sign in to follow shops."));
            return ToActionResult(await _engagement.FollowShopAsync(userId, shopId));
        }

        [HttpDelete("api/shops/{shopId:guid}/follow")]
        public async Task<IActionResult> UnfollowShop(Guid shopId)
        {
            if (!TryGetUserId(out var userId))
                return ToActionResult(Result<EngagementToggleResultDto>.Failure(ErrorCodes.Unauthorized, "Sign in to manage follows."));
            return ToActionResult(await _engagement.UnfollowShopAsync(userId, shopId));
        }

        [HttpGet("api/me/followed-shops")]
        public async Task<IActionResult> MyFollowedShops([FromQuery] int page = 1, [FromQuery] int pageSize = 30)
        {
            if (!TryGetUserId(out var userId))
                return ToActionResult(Result<System.Collections.Generic.List<FollowedShopDto>>.Failure(ErrorCodes.Unauthorized, "Sign in to view your followed shops."));
            return ToActionResult(await _engagement.GetMyFollowedShopsAsync(userId, page, pageSize));
        }

        // ─────────────────────────────────────────────────────────────────
        // Physical stores — POST/DELETE /api/stores/{merchantId}/save + GET /api/me/saved-stores
        //
        // The path is /stores/ even though the FK is to Merchant, because
        // the buyer-facing surface for these rows is the "Stores" tab
        // (only merchants with Type==PhysicalStore are returned).
        // ─────────────────────────────────────────────────────────────────

        [HttpPost("api/stores/{storeId:guid}/save")]
        public async Task<IActionResult> SaveStore(Guid storeId)
        {
            if (!TryGetUserId(out var userId))
                return ToActionResult(Result<EngagementToggleResultDto>.Failure(ErrorCodes.Unauthorized, "Sign in to save stores."));
            return ToActionResult(await _engagement.SaveStoreAsync(userId, storeId));
        }

        [HttpDelete("api/stores/{storeId:guid}/save")]
        public async Task<IActionResult> UnsaveStore(Guid storeId)
        {
            if (!TryGetUserId(out var userId))
                return ToActionResult(Result<EngagementToggleResultDto>.Failure(ErrorCodes.Unauthorized, "Sign in to manage saved stores."));
            return ToActionResult(await _engagement.UnsaveStoreAsync(userId, storeId));
        }

        [HttpGet("api/me/saved-stores")]
        public async Task<IActionResult> MySavedStores([FromQuery] int page = 1, [FromQuery] int pageSize = 30)
        {
            if (!TryGetUserId(out var userId))
                return ToActionResult(Result<System.Collections.Generic.List<SavedStoreDto>>.Failure(ErrorCodes.Unauthorized, "Sign in to view your saved stores."));
            return ToActionResult(await _engagement.GetMySavedStoresAsync(userId, page, pageSize));
        }
    }
}
