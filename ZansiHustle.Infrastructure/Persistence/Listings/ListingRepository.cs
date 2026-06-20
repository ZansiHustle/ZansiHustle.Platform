using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ZansiHustle.Application.Listings.Dtos;
using ZansiHustle.Application.Persistence.Listings;
using ZansiHustle.Domain.Listings;
using ZansiHustle.Infrastructure.Data;
using ZansiHustle.Shared.Enums.Listings;
using ZansiHustle.Shared.Enums.Merchants;
using ZansiHustle.Shared.Enums.Shops;

namespace ZansiHustle.Infrastructure.Persistence.Listings
{
    public class ListingRepository : IListingRepository
    {
        private readonly AppDbContext _context;

        public ListingRepository(AppDbContext context)
        {
            _context = context;
        }

        /// <inheritdoc />
        public async Task<(List<Listing> Items, int Total)> SearchAsync(ListingFilterRequestDto filter)
        {
            IQueryable<Listing> query = _context.Listings
                .AsNoTracking()
                .Include(x => x.Merchant)
                .Include(x => x.ShopProfile)
                .Include(x => x.SellerCategory)
                .Include(x => x.SellerSubcategory)
                .Include(x => x.Variants);

            // Hard contract for the public listing search:
            //
            //   1. Status MUST be Active. Earlier the filter was
            //      optional; the mobile client always passed Active
            //      but a forgotten parameter would expose Drafts /
            //      Archived. We now enforce server-side regardless of
            //      the caller-supplied filter.
            //   2. AvailabilityMode MUST NOT be InStoreOnly. Physical-
            //      store catalog items belong on the merchant's Store
            //      profile, never in Home / Explore / global search.
            //      A caller-supplied filter can narrow further (e.g.
            //      "OnlineOnly only") but cannot loosen this gate.
            query = query
                .Where(x => x.Status == ListingStatus.Active)
                .Where(x => x.AvailabilityMode != AvailabilityMode.InStoreOnly);

            //   3. Buyer-facing visibility gate. The two pauses are INDEPENDENT:
            //      • SellerAccount listings are hidden when the owning merchant
            //        is seller-paused (Merchant.SellerVisibility != Visible).
            //      • ShopProfile listings are hidden when their shop is paused
            //        (ShopProfile.VisibilityStatus != Visible) — a seller pause
            //        does NOT hide shop items, and vice-versa.
            //      Defaults are Visible, so existing rows are unaffected. Owner/
            //      admin management reads (GetByOwner/GetByMerchant) never apply
            //      this gate, so the seller still sees everything in Seller Centre.
            query = query.Where(x =>
                (x.ListingSource != ListingSource.ShopProfile
                    && x.Merchant != null
                    && x.Merchant.SellerVisibility == SellerVisibilityStatus.Visible)
                || (x.ListingSource == ListingSource.ShopProfile
                    && x.ShopProfile != null
                    && x.ShopProfile.VisibilityStatus == ShopVisibilityStatus.Visible));

            if (filter.Type.HasValue)
                query = query.Where(x => x.Type == filter.Type.Value);

            // filter.Status no longer relaxes the Active hard-filter
            // above — we keep the parameter on the DTO for API
            // back-compat, but it can only narrow within Active. If
            // a caller passes `status=Draft` we return zero rows
            // (intentional — buyer feeds never see drafts).
            if (filter.Status.HasValue && filter.Status.Value != ListingStatus.Active)
                query = query.Where(_ => false);

            if (filter.AvailabilityMode.HasValue &&
                filter.AvailabilityMode.Value != AvailabilityMode.InStoreOnly)
            {
                query = query.Where(x => x.AvailabilityMode == filter.AvailabilityMode.Value);
            }

            if (filter.MerchantId.HasValue)
                query = query.Where(x => x.MerchantId == filter.MerchantId.Value);

            if (filter.ShopProfileId.HasValue)
            {
                // Filter by explicit shop association — the listing
                // MUST be tagged ShopProfile source AND linked to this
                // exact ShopProfileId. Skipping the source check would
                // re-introduce the original bug: any matching FK row
                // would surface even if the listing wasn't supposed to
                // appear on the shop.
                query = query
                    .Where(x => x.ListingSource == ListingSource.ShopProfile
                        && x.ShopProfileId == filter.ShopProfileId.Value);
            }

            if (filter.ListingSource.HasValue)
                query = query.Where(x => x.ListingSource == filter.ListingSource.Value);

            if (filter.SellerCategoryId.HasValue)
                query = query.Where(x => x.SellerCategoryId == filter.SellerCategoryId.Value);

            if (filter.SellerSubcategoryId.HasValue)
                query = query.Where(x => x.SellerSubcategoryId == filter.SellerSubcategoryId.Value);

            if (!string.IsNullOrWhiteSpace(filter.Province))
                query = query.Where(x => x.Province == filter.Province);

            if (!string.IsNullOrWhiteSpace(filter.City))
                query = query.Where(x => x.City == filter.City);

            if (filter.MinPrice.HasValue)
                query = query.Where(x => x.Price >= filter.MinPrice.Value);

            if (filter.MaxPrice.HasValue)
                query = query.Where(x => x.Price <= filter.MaxPrice.Value);

            if (filter.FeaturedOnly == true)
                query = query.Where(x => x.IsFeatured);

            if (!string.IsNullOrWhiteSpace(filter.Q))
            {
                var q = filter.Q.Trim();
                query = query.Where(x =>
                    EF.Functions.Like(x.Title, $"%{q}%") ||
                    (x.Description != null && EF.Functions.Like(x.Description, $"%{q}%")));
            }

            query = ApplySort(query, filter.Sort);

            var total = await query.CountAsync();

            var page = filter.Page <= 0 ? 1 : filter.Page;
            var pageSize = filter.PageSize <= 0 ? 20 : (filter.PageSize > 100 ? 100 : filter.PageSize);

            var items = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, total);
        }

        /// <inheritdoc />
        public async Task<Listing?> GetByIdAsync(Guid id)
        {
            return await _context.Listings
                .Include(x => x.Merchant)
                .Include(x => x.ShopProfile)
                .Include(x => x.SellerCategory)
                .Include(x => x.SellerSubcategory)
                .Include(x => x.Variants)
                .FirstOrDefaultAsync(x => x.Id == id);
        }

        /// <inheritdoc />
        public async Task<Listing?> GetBySlugAsync(string slug)
        {
            if (string.IsNullOrWhiteSpace(slug))
                return null;

            var normalized = slug.Trim().ToLowerInvariant();

            return await _context.Listings
                .Include(x => x.Merchant)
                .Include(x => x.ShopProfile)
                .Include(x => x.SellerCategory)
                .Include(x => x.SellerSubcategory)
                .Include(x => x.Variants)
                .FirstOrDefaultAsync(x => x.Slug == normalized);
        }

        /// <inheritdoc />
        public async Task<List<Listing>> GetByMerchantAsync(Guid merchantId)
        {
            return await _context.Listings
                .AsNoTracking()
                .Include(x => x.Merchant)
                .Include(x => x.ShopProfile)
                .Include(x => x.SellerCategory)
                .Include(x => x.SellerSubcategory)
                .Include(x => x.Variants)
                .Where(x => x.MerchantId == merchantId)
                .OrderByDescending(x => x.CreatedAtUtc)
                .ToListAsync();
        }

        /// <inheritdoc />
        public async Task<List<Listing>> GetByShopProfileAsync(Guid shopProfileId)
        {
            return await _context.Listings
                .AsNoTracking()
                .Include(x => x.Merchant)
                .Include(x => x.ShopProfile)
                .Include(x => x.SellerCategory)
                .Include(x => x.SellerSubcategory)
                .Include(x => x.Variants)
                .Where(x => x.ListingSource == ListingSource.ShopProfile
                    && x.ShopProfileId == shopProfileId
                    // Hide the whole catalog when the shop is paused/under-review/
                    // blocked. Buyers get an empty catalog; the owner manages items
                    // via Seller Centre (GetByOwner — never this public read).
                    && x.ShopProfile != null
                    && x.ShopProfile.VisibilityStatus == ShopVisibilityStatus.Visible)
                .OrderByDescending(x => x.CreatedAtUtc)
                .ToListAsync();
        }

        /// <inheritdoc />
        public async Task<List<Listing>> GetByOwnerAsync(Guid ownerUserId)
        {
            return await _context.Listings
                .AsNoTracking()
                .Include(x => x.Merchant)
                .Include(x => x.ShopProfile)
                .Include(x => x.SellerCategory)
                .Include(x => x.SellerSubcategory)
                .Include(x => x.Variants)
                .Where(x => x.Merchant != null && x.Merchant.OwnerUserId == ownerUserId)
                .OrderByDescending(x => x.CreatedAtUtc)
                .ToListAsync();
        }

        /// <inheritdoc />
        public async Task<bool> ExistsBySlugAsync(string slug)
        {
            if (string.IsNullOrWhiteSpace(slug))
                return false;

            var normalized = slug.Trim().ToLowerInvariant();

            return await _context.Listings.AnyAsync(x => x.Slug == normalized);
        }

        /// <inheritdoc />
        public async Task AddAsync(Listing listing)
        {
            ArgumentNullException.ThrowIfNull(listing);

            await _context.Listings.AddAsync(listing);
        }

        /// <inheritdoc />
        public void Update(Listing listing)
        {
            ArgumentNullException.ThrowIfNull(listing);

            // CRITICAL: only attach via DbSet.Update when the entity is
            // detached. Calling `_context.Listings.Update(trackedListing)`
            // runs a graph traversal that *re-marks every reachable
            // entity as Modified, including new child rows we just
            // added via `listing.Variants.Add(new ListingVariant {...})`
            // in ApplyVariantDiff. EF then tries to UPDATE the freshly-
            // added variant by its PK, the row doesn't exist yet, and
            // SaveChanges throws DbUpdateConcurrencyException ("1 row
            // expected, 0 affected"). The service flow always loads
            // via the same context (tracked), so this branch is a
            // no-op for our hot path — the change tracker has already
            // captured the scalar edits + the variant diff.
            var entry = _context.Entry(listing);
            if (entry.State == EntityState.Detached)
            {
                _context.Listings.Update(listing);
            }
        }

        /// <inheritdoc />
        public void Delete(Listing listing)
        {
            ArgumentNullException.ThrowIfNull(listing);

            _context.Listings.Remove(listing);
        }

        /// <inheritdoc />
        public async Task<bool> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync() > 0;
        }

        /// <inheritdoc />
        public async Task<bool> SaveListingAndReplaceVariantsAsync(
            Listing listing,
            IReadOnlyList<ListingVariant>? newVariantsOrNull)
        {
            ArgumentNullException.ThrowIfNull(listing);

            // Fast path: caller doesn't want to touch variants.
            if (newVariantsOrNull is null)
            {
                return await _context.SaveChangesAsync() > 0;
            }

            // Atomic variant replace. ExecuteDeleteAsync runs immediately
            // against the DB (bypasses the change tracker) so we must
            // wrap it together with the deferred SaveChangesAsync in an
            // explicit transaction — otherwise a failure between the
            // delete and the inserts would leave the listing with no
            // variants.
            await using var tx = await _context.Database.BeginTransactionAsync();
            try
            {
                // Detach any variants EF picked up via `.Include(Variants)`
                // earlier in the request. We're about to delete those
                // rows out-of-band; leaving them tracked would let EF
                // try to UPDATE / DELETE rows that no longer exist when
                // SaveChanges fires for the scalar listing changes.
                foreach (var v in listing.Variants.ToList())
                {
                    _context.Entry(v).State = EntityState.Detached;
                }
                listing.Variants.Clear();

                // Single SQL DELETE for every variant of this listing —
                // safer than tracker-based removal because there's no
                // row-by-row Modified/Deleted state for EF to confuse.
                await _context.ListingVariants
                    .Where(v => v.ListingId == listing.Id)
                    .ExecuteDeleteAsync();

                // Stage new variants for INSERT. Each must carry a
                // pre-assigned Id (set by the service in BuildVariants…)
                // because we don't rely on store-generated PK here.
                if (newVariantsOrNull.Count > 0)
                {
                    await _context.ListingVariants.AddRangeAsync(newVariantsOrNull);
                }

                // SaveChanges commits: listing UPDATE + new variant
                // INSERTs in this transaction. Affected rows always >= 1
                // because the listing's UpdatedAtUtc bump is a tracked
                // scalar change.
                var affected = await _context.SaveChangesAsync();

                await tx.CommitAsync();

                // Drop the tracker so the next reload (for the
                // response DTO) starts clean and doesn't see the now-
                // stale references to the detached old variants.
                _context.ChangeTracker.Clear();

                return affected > 0;
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }

        private static IQueryable<Listing> ApplySort(IQueryable<Listing> query, string? sort)
        {
            return (sort ?? "newest").ToLowerInvariant() switch
            {
                "price_asc" => query.OrderBy(x => x.Price).ThenByDescending(x => x.CreatedAtUtc),
                "price_desc" => query.OrderByDescending(x => x.Price).ThenByDescending(x => x.CreatedAtUtc),
                "rating" => query.OrderByDescending(x => x.Rating ?? 0m).ThenByDescending(x => x.CreatedAtUtc),
                "featured" => query.OrderByDescending(x => x.IsFeatured)
                                   .ThenByDescending(x => x.IsBoosted)
                                   .ThenByDescending(x => x.CreatedAtUtc),
                _ => query.OrderByDescending(x => x.IsFeatured)
                          .ThenByDescending(x => x.CreatedAtUtc)
            };
        }
    }
}
