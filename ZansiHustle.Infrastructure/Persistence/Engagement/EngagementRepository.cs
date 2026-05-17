using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ZansiHustle.Application.Persistence.Engagement;
using ZansiHustle.Domain.Engagement;
using ZansiHustle.Domain.Listings;
using ZansiHustle.Domain.Marketplace;
using ZansiHustle.Domain.Merchants;
using ZansiHustle.Domain.Shops;
using ZansiHustle.Infrastructure.Data;
using ZansiHustle.Shared.Enums.Listings;
using ZansiHustle.Shared.Enums.Marketplace;
using ZansiHustle.Shared.Enums.Shops;

namespace ZansiHustle.Infrastructure.Persistence.Engagement
{
    /// <summary>
    /// EF Core implementation of <see cref="IEngagementRepository"/>.
    /// Each mutation runs inside an explicit transaction so the join-
    /// table row and the parent's denormalised count change atomically;
    /// a crash mid-write leaves both consistent.
    /// </summary>
    public sealed class EngagementRepository : IEngagementRepository
    {
        private readonly AppDbContext _db;

        public EngagementRepository(AppDbContext db)
        {
            _db = db;
        }

        // ─────────────────────────────────────────────────────────────────
        // ListingLikes
        // ─────────────────────────────────────────────────────────────────

        public async Task<bool> AddListingLikeAsync(Guid userId, Guid listingId, CancellationToken ct = default)
        {
            // Pre-check + insert + count++ in one transaction. We do the
            // pre-check rather than relying on a unique-index violation
            // because catching DbUpdateException to mean "already liked"
            // is muddier than an explicit "exists?" lookup. The unique
            // index is still the hard guarantee against any race
            // between two concurrent POSTs from the same client — the
            // second one will throw, which the SERVICE swallows and
            // surfaces as "already liked" → idempotent success.
            await using var tx = await _db.Database.BeginTransactionAsync(ct);

            var exists = await _db.ListingLikes
                .AnyAsync(l => l.UserId == userId && l.ListingId == listingId, ct);
            if (exists)
            {
                await tx.CommitAsync(ct);
                return false;
            }

            _db.ListingLikes.Add(new ListingLike
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                ListingId = listingId,
                CreatedAtUtc = DateTime.UtcNow,
            });

            await _db.Listings
                .Where(x => x.Id == listingId)
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.LikeCount, x => x.LikeCount + 1), ct);

            await _db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
            return true;
        }

        public async Task<bool> RemoveListingLikeAsync(Guid userId, Guid listingId, CancellationToken ct = default)
        {
            await using var tx = await _db.Database.BeginTransactionAsync(ct);

            var deleted = await _db.ListingLikes
                .Where(l => l.UserId == userId && l.ListingId == listingId)
                .ExecuteDeleteAsync(ct);

            if (deleted == 0)
            {
                await tx.CommitAsync(ct);
                return false;
            }

            // Floor at zero — `LikeCount - 1` can never go negative when
            // the row genuinely existed, but the `Math.Max` guard means
            // a corrupt-data scenario (count drifted) can't bottom out
            // into negative territory and trip downstream UI.
            await _db.Listings
                .Where(x => x.Id == listingId)
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.LikeCount,
                    x => x.LikeCount > 0 ? x.LikeCount - 1 : 0), ct);

            await tx.CommitAsync(ct);
            return true;
        }

        public Task<bool> IsListingLikedAsync(Guid userId, Guid listingId, CancellationToken ct = default)
            => _db.ListingLikes.AnyAsync(l => l.UserId == userId && l.ListingId == listingId, ct);

        public async Task<HashSet<Guid>> WhichListingsLikedAsync(Guid userId, IReadOnlyCollection<Guid> listingIds, CancellationToken ct = default)
        {
            if (listingIds.Count == 0) return new HashSet<Guid>();
            var hits = await _db.ListingLikes
                .Where(l => l.UserId == userId && listingIds.Contains(l.ListingId))
                .Select(l => l.ListingId)
                .ToListAsync(ct);
            return hits.ToHashSet();
        }

        public async Task<List<Listing>> GetMyLikedListingsAsync(Guid userId, int page, int pageSize, CancellationToken ct = default)
        {
            // Join the like rows to the listings and surface only the
            // ones still publicly visible — the user can have an old
            // like row pointing at a now-Deleted/Suspended listing; the
            // saved screen should hide those rather than render broken
            // cards. (We still leave the like row in place so when the
            // seller re-activates the listing it reappears.)
            var query =
                from like in _db.ListingLikes.AsNoTracking()
                join listing in _db.Listings.AsNoTracking()
                    on like.ListingId equals listing.Id
                where like.UserId == userId
                      && listing.Status == ListingStatus.Active
                orderby like.CreatedAtUtc descending
                select listing;

            return await query
                .Skip(Math.Max(0, (page - 1) * pageSize))
                .Take(Math.Clamp(pageSize, 1, 100))
                .Include(l => l.Merchant)
                .Include(l => l.ShopProfile)
                .ToListAsync(ct);
        }

        // ─────────────────────────────────────────────────────────────────
        // MarketplaceListingLikes
        // ─────────────────────────────────────────────────────────────────

        public async Task<bool> AddMarketplaceLikeAsync(Guid userId, Guid marketplaceListingId, CancellationToken ct = default)
        {
            await using var tx = await _db.Database.BeginTransactionAsync(ct);

            var exists = await _db.MarketplaceListingLikes
                .AnyAsync(l => l.UserId == userId && l.MarketplaceListingId == marketplaceListingId, ct);
            if (exists)
            {
                await tx.CommitAsync(ct);
                return false;
            }

            _db.MarketplaceListingLikes.Add(new MarketplaceListingLike
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                MarketplaceListingId = marketplaceListingId,
                CreatedAtUtc = DateTime.UtcNow,
            });

            await _db.MarketplaceListings
                .Where(x => x.Id == marketplaceListingId)
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.LikeCount, x => x.LikeCount + 1), ct);

            await _db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
            return true;
        }

        public async Task<bool> RemoveMarketplaceLikeAsync(Guid userId, Guid marketplaceListingId, CancellationToken ct = default)
        {
            await using var tx = await _db.Database.BeginTransactionAsync(ct);

            var deleted = await _db.MarketplaceListingLikes
                .Where(l => l.UserId == userId && l.MarketplaceListingId == marketplaceListingId)
                .ExecuteDeleteAsync(ct);

            if (deleted == 0)
            {
                await tx.CommitAsync(ct);
                return false;
            }

            await _db.MarketplaceListings
                .Where(x => x.Id == marketplaceListingId)
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.LikeCount,
                    x => x.LikeCount > 0 ? x.LikeCount - 1 : 0), ct);

            await tx.CommitAsync(ct);
            return true;
        }

        public Task<bool> IsMarketplaceLikedAsync(Guid userId, Guid marketplaceListingId, CancellationToken ct = default)
            => _db.MarketplaceListingLikes.AnyAsync(l => l.UserId == userId && l.MarketplaceListingId == marketplaceListingId, ct);

        public async Task<HashSet<Guid>> WhichMarketplaceLikedAsync(Guid userId, IReadOnlyCollection<Guid> ids, CancellationToken ct = default)
        {
            if (ids.Count == 0) return new HashSet<Guid>();
            var hits = await _db.MarketplaceListingLikes
                .Where(l => l.UserId == userId && ids.Contains(l.MarketplaceListingId))
                .Select(l => l.MarketplaceListingId)
                .ToListAsync(ct);
            return hits.ToHashSet();
        }

        public async Task<List<MarketplaceListing>> GetMyLikedMarketplaceAsync(Guid userId, int page, int pageSize, CancellationToken ct = default)
        {
            var query =
                from like in _db.MarketplaceListingLikes.AsNoTracking()
                join listing in _db.MarketplaceListings.AsNoTracking()
                    on like.MarketplaceListingId equals listing.Id
                where like.UserId == userId
                      && listing.Status == MarketplaceListingStatus.Active
                orderby like.CreatedAtUtc descending
                select listing;

            return await query
                .Skip(Math.Max(0, (page - 1) * pageSize))
                .Take(Math.Clamp(pageSize, 1, 100))
                .Include(l => l.Images)
                .ToListAsync(ct);
        }

        // ─────────────────────────────────────────────────────────────────
        // ShopFollows
        // ─────────────────────────────────────────────────────────────────

        public async Task<bool> AddShopFollowAsync(Guid userId, Guid shopProfileId, CancellationToken ct = default)
        {
            await using var tx = await _db.Database.BeginTransactionAsync(ct);

            var exists = await _db.ShopFollows
                .AnyAsync(f => f.UserId == userId && f.ShopProfileId == shopProfileId, ct);
            if (exists)
            {
                await tx.CommitAsync(ct);
                return false;
            }

            _db.ShopFollows.Add(new ShopFollow
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                ShopProfileId = shopProfileId,
                CreatedAtUtc = DateTime.UtcNow,
            });

            await _db.ShopProfiles
                .Where(x => x.Id == shopProfileId)
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.FollowersCount, x => x.FollowersCount + 1), ct);

            await _db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
            return true;
        }

        public async Task<bool> RemoveShopFollowAsync(Guid userId, Guid shopProfileId, CancellationToken ct = default)
        {
            await using var tx = await _db.Database.BeginTransactionAsync(ct);

            var deleted = await _db.ShopFollows
                .Where(f => f.UserId == userId && f.ShopProfileId == shopProfileId)
                .ExecuteDeleteAsync(ct);

            if (deleted == 0)
            {
                await tx.CommitAsync(ct);
                return false;
            }

            await _db.ShopProfiles
                .Where(x => x.Id == shopProfileId)
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.FollowersCount,
                    x => x.FollowersCount > 0 ? x.FollowersCount - 1 : 0), ct);

            await tx.CommitAsync(ct);
            return true;
        }

        public Task<bool> IsShopFollowedAsync(Guid userId, Guid shopProfileId, CancellationToken ct = default)
            => _db.ShopFollows.AnyAsync(f => f.UserId == userId && f.ShopProfileId == shopProfileId, ct);

        public async Task<HashSet<Guid>> WhichShopsFollowedAsync(Guid userId, IReadOnlyCollection<Guid> shopProfileIds, CancellationToken ct = default)
        {
            if (shopProfileIds.Count == 0) return new HashSet<Guid>();
            var hits = await _db.ShopFollows
                .Where(f => f.UserId == userId && shopProfileIds.Contains(f.ShopProfileId))
                .Select(f => f.ShopProfileId)
                .ToListAsync(ct);
            return hits.ToHashSet();
        }

        public async Task<List<ShopProfile>> GetMyFollowedShopsAsync(Guid userId, int page, int pageSize, CancellationToken ct = default)
        {
            // Only show Active shops in the followed list — same
            // reasoning as the listing path: a suspended shop's row
            // stays in the follow table so re-activation restores it.
            var query =
                from follow in _db.ShopFollows.AsNoTracking()
                join shop in _db.ShopProfiles.AsNoTracking()
                    on follow.ShopProfileId equals shop.Id
                where follow.UserId == userId
                      && shop.Status == ShopProfileStatus.Active
                orderby follow.CreatedAtUtc descending
                select shop;

            return await query
                .Skip(Math.Max(0, (page - 1) * pageSize))
                .Take(Math.Clamp(pageSize, 1, 100))
                .ToListAsync(ct);
        }

        // ─────────────────────────────────────────────────────────────────
        // StoreSaves
        // ─────────────────────────────────────────────────────────────────

        public async Task<bool> AddStoreSaveAsync(Guid userId, Guid merchantId, CancellationToken ct = default)
        {
            await using var tx = await _db.Database.BeginTransactionAsync(ct);

            var exists = await _db.StoreSaves
                .AnyAsync(s => s.UserId == userId && s.MerchantId == merchantId, ct);
            if (exists)
            {
                await tx.CommitAsync(ct);
                return false;
            }

            _db.StoreSaves.Add(new StoreSave
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                MerchantId = merchantId,
                CreatedAtUtc = DateTime.UtcNow,
            });

            await _db.Merchants
                .Where(x => x.Id == merchantId)
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.SavesCount, x => x.SavesCount + 1), ct);

            await _db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
            return true;
        }

        public async Task<bool> RemoveStoreSaveAsync(Guid userId, Guid merchantId, CancellationToken ct = default)
        {
            await using var tx = await _db.Database.BeginTransactionAsync(ct);

            var deleted = await _db.StoreSaves
                .Where(s => s.UserId == userId && s.MerchantId == merchantId)
                .ExecuteDeleteAsync(ct);

            if (deleted == 0)
            {
                await tx.CommitAsync(ct);
                return false;
            }

            await _db.Merchants
                .Where(x => x.Id == merchantId)
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.SavesCount,
                    x => x.SavesCount > 0 ? x.SavesCount - 1 : 0), ct);

            await tx.CommitAsync(ct);
            return true;
        }

        public Task<bool> IsStoreSavedAsync(Guid userId, Guid merchantId, CancellationToken ct = default)
            => _db.StoreSaves.AnyAsync(s => s.UserId == userId && s.MerchantId == merchantId, ct);

        public async Task<HashSet<Guid>> WhichStoresSavedAsync(Guid userId, IReadOnlyCollection<Guid> merchantIds, CancellationToken ct = default)
        {
            if (merchantIds.Count == 0) return new HashSet<Guid>();
            var hits = await _db.StoreSaves
                .Where(s => s.UserId == userId && merchantIds.Contains(s.MerchantId))
                .Select(s => s.MerchantId)
                .ToListAsync(ct);
            return hits.ToHashSet();
        }

        public async Task<List<Merchant>> GetMySavedStoresAsync(Guid userId, int page, int pageSize, CancellationToken ct = default)
        {
            var query =
                from save in _db.StoreSaves.AsNoTracking()
                join merchant in _db.Merchants.AsNoTracking()
                    on save.MerchantId equals merchant.Id
                where save.UserId == userId
                orderby save.CreatedAtUtc descending
                select merchant;

            return await query
                .Skip(Math.Max(0, (page - 1) * pageSize))
                .Take(Math.Clamp(pageSize, 1, 100))
                .ToListAsync(ct);
        }
    }
}
