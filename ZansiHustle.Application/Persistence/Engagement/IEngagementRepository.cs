using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ZansiHustle.Domain.Listings;
using ZansiHustle.Domain.Marketplace;
using ZansiHustle.Domain.Merchants;
using ZansiHustle.Domain.Shops;

namespace ZansiHustle.Application.Persistence.Engagement
{
    /// <summary>
    /// Persistence contract for all four engagement join tables —
    /// <c>ListingLikes</c>, <c>MarketplaceListingLikes</c>,
    /// <c>ShopFollows</c>, and <c>StoreSaves</c>.
    ///
    /// Each <c>Add*</c> / <c>Remove*</c> method is idempotent: a
    /// second call with the same (userId, targetId) is a no-op and
    /// returns <c>false</c>. The repository handles the parent-entity
    /// count update transactionally with the row insert/delete so the
    /// denormalised count never drifts from the source-of-truth join
    /// table.
    ///
    /// <c>Which*</c> methods batch-resolve "did this user engage with
    /// these targets?" so list endpoints can hydrate
    /// <c>IsLikedByMe</c> / <c>IsFollowedByMe</c> / <c>IsSavedByMe</c>
    /// in a single query rather than N round-trips.
    /// </summary>
    public interface IEngagementRepository
    {
        // ── Listing likes ──────────────────────────────────────────
        /// <returns><c>true</c> if a new row was inserted; <c>false</c> when it already existed (idempotent no-op).</returns>
        Task<bool> AddListingLikeAsync(Guid userId, Guid listingId, CancellationToken ct = default);
        /// <returns><c>true</c> if a row was deleted; <c>false</c> when none existed (idempotent no-op).</returns>
        Task<bool> RemoveListingLikeAsync(Guid userId, Guid listingId, CancellationToken ct = default);
        Task<bool> IsListingLikedAsync(Guid userId, Guid listingId, CancellationToken ct = default);
        Task<HashSet<Guid>> WhichListingsLikedAsync(Guid userId, IReadOnlyCollection<Guid> listingIds, CancellationToken ct = default);
        Task<List<Listing>> GetMyLikedListingsAsync(Guid userId, int page, int pageSize, CancellationToken ct = default);

        // ── Marketplace listing likes ──────────────────────────────
        Task<bool> AddMarketplaceLikeAsync(Guid userId, Guid marketplaceListingId, CancellationToken ct = default);
        Task<bool> RemoveMarketplaceLikeAsync(Guid userId, Guid marketplaceListingId, CancellationToken ct = default);
        Task<bool> IsMarketplaceLikedAsync(Guid userId, Guid marketplaceListingId, CancellationToken ct = default);
        Task<HashSet<Guid>> WhichMarketplaceLikedAsync(Guid userId, IReadOnlyCollection<Guid> ids, CancellationToken ct = default);
        Task<List<MarketplaceListing>> GetMyLikedMarketplaceAsync(Guid userId, int page, int pageSize, CancellationToken ct = default);

        // ── Shop follows ───────────────────────────────────────────
        Task<bool> AddShopFollowAsync(Guid userId, Guid shopProfileId, CancellationToken ct = default);
        Task<bool> RemoveShopFollowAsync(Guid userId, Guid shopProfileId, CancellationToken ct = default);
        Task<bool> IsShopFollowedAsync(Guid userId, Guid shopProfileId, CancellationToken ct = default);
        Task<HashSet<Guid>> WhichShopsFollowedAsync(Guid userId, IReadOnlyCollection<Guid> shopProfileIds, CancellationToken ct = default);
        Task<List<ShopProfile>> GetMyFollowedShopsAsync(Guid userId, int page, int pageSize, CancellationToken ct = default);

        // ── Store saves ────────────────────────────────────────────
        Task<bool> AddStoreSaveAsync(Guid userId, Guid merchantId, CancellationToken ct = default);
        Task<bool> RemoveStoreSaveAsync(Guid userId, Guid merchantId, CancellationToken ct = default);
        Task<bool> IsStoreSavedAsync(Guid userId, Guid merchantId, CancellationToken ct = default);
        Task<HashSet<Guid>> WhichStoresSavedAsync(Guid userId, IReadOnlyCollection<Guid> merchantIds, CancellationToken ct = default);
        Task<List<Merchant>> GetMySavedStoresAsync(Guid userId, int page, int pageSize, CancellationToken ct = default);
    }
}
