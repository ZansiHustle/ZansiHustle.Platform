using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ZansiHustle.Application.Engagement.Dtos;
using ZansiHustle.Application.Listings.Dtos;
using ZansiHustle.Application.Marketplace.Dtos;
using ZansiHustle.Shared.Results;

namespace ZansiHustle.Application.Engagement
{
    /// <summary>
    /// Buyer-engagement service — handles the four "save-style" actions
    /// in one place (like normal listing, like marketplace listing,
    /// follow shop, save physical store). Every mutation is idempotent;
    /// the controller / client can replay a request without producing
    /// duplicate rows or count drift.
    ///
    /// Ownership rule: the owner of a target cannot like / follow /
    /// save their own row. Returns <c>FORBIDDEN</c> rather than a
    /// silent no-op so the client UI can keep the heart button hidden
    /// for owners and flag any leaked attempt.
    /// </summary>
    public interface IEngagementService
    {
        // ── Listings (Products / Services owned by Merchants) ──
        Task<Result<EngagementToggleResultDto>> LikeListingAsync(Guid userId, Guid listingId, CancellationToken ct = default);
        Task<Result<EngagementToggleResultDto>> UnlikeListingAsync(Guid userId, Guid listingId, CancellationToken ct = default);
        Task<Result<List<ListingListItemDto>>> GetMyLikedListingsAsync(Guid userId, int page, int pageSize, CancellationToken ct = default);

        // ── Marketplace (casual user-owned listings) ──
        Task<Result<EngagementToggleResultDto>> LikeMarketplaceListingAsync(Guid userId, Guid marketplaceListingId, CancellationToken ct = default);
        Task<Result<EngagementToggleResultDto>> UnlikeMarketplaceListingAsync(Guid userId, Guid marketplaceListingId, CancellationToken ct = default);
        Task<Result<List<MarketplaceListingDto>>> GetMyLikedMarketplaceListingsAsync(Guid userId, int page, int pageSize, CancellationToken ct = default);

        // ── Shops ──
        Task<Result<EngagementToggleResultDto>> FollowShopAsync(Guid userId, Guid shopProfileId, CancellationToken ct = default);
        Task<Result<EngagementToggleResultDto>> UnfollowShopAsync(Guid userId, Guid shopProfileId, CancellationToken ct = default);
        Task<Result<List<FollowedShopDto>>> GetMyFollowedShopsAsync(Guid userId, int page, int pageSize, CancellationToken ct = default);

        // ── Physical stores (Merchant where Type == PhysicalStore) ──
        Task<Result<EngagementToggleResultDto>> SaveStoreAsync(Guid userId, Guid merchantId, CancellationToken ct = default);
        Task<Result<EngagementToggleResultDto>> UnsaveStoreAsync(Guid userId, Guid merchantId, CancellationToken ct = default);
        Task<Result<List<SavedStoreDto>>> GetMySavedStoresAsync(Guid userId, int page, int pageSize, CancellationToken ct = default);
    }
}
