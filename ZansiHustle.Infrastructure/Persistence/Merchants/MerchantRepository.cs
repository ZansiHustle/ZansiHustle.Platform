using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ZansiHustle.Application.Common.Geo;
using ZansiHustle.Application.Merchants.Dtos;
using ZansiHustle.Application.Persistence.Merchants;
using ZansiHustle.Domain.Merchants;
using ZansiHustle.Infrastructure.Data;
using ZansiHustle.Shared.Enums.Merchants;

namespace ZansiHustle.Infrastructure.Persistence.Merchants
{
    /// <summary>
    /// Repository implementation for merchant persistence operations.
    /// </summary>
    public class MerchantRepository : IMerchantRepository
    {
        private readonly AppDbContext _context;

        public MerchantRepository(AppDbContext context)
        {
            _context = context;
        }

        /// <inheritdoc />
        public async Task<List<Merchant>> GetAllAsync()
        {
            return await _context.Merchants
                .AsNoTracking()
                .Include(x => x.SellerCategory)
                .Include(x => x.SellerSubcategory)
                .OrderByDescending(x => x.CreatedAtUtc)
                .ToListAsync();
        }

        /// <inheritdoc />
        public async Task<List<Merchant>> GetByOwnerAsync(Guid ownerUserId)
        {
            return await _context.Merchants
                .AsNoTracking()
                .Include(x => x.SellerCategory)
                .Include(x => x.SellerSubcategory)
                .Where(x => x.OwnerUserId == ownerUserId)
                .OrderByDescending(x => x.CreatedAtUtc)
                .ToListAsync();
        }

        /// <inheritdoc />
        public async Task<Merchant?> GetByIdAsync(Guid id)
        {
            return await _context.Merchants
                .Include(x => x.SellerCategory)
                .Include(x => x.SellerSubcategory)
                .FirstOrDefaultAsync(x => x.Id == id);
        }

        /// <inheritdoc />
        public async Task<Merchant?> GetByCodeAsync(string code)
        {
            if (string.IsNullOrWhiteSpace(code))
                return null;

            var normalizedCode = code.Trim();

            return await _context.Merchants
                .Include(x => x.SellerCategory)
                .Include(x => x.SellerSubcategory)
                .FirstOrDefaultAsync(x => x.Code == normalizedCode);
        }

        /// <inheritdoc />
        public async Task<Merchant?> GetBySlugAsync(string slug)
        {
            if (string.IsNullOrWhiteSpace(slug))
                return null;

            var normalized = slug.Trim().ToLowerInvariant();

            return await _context.Merchants
                .Include(x => x.SellerCategory)
                .Include(x => x.SellerSubcategory)
                .FirstOrDefaultAsync(x => x.Slug == normalized);
        }

        /// <inheritdoc />
        public async Task<bool> ExistsByCodeAsync(string code)
        {
            if (string.IsNullOrWhiteSpace(code))
                return false;

            var normalizedCode = code.Trim();

            return await _context.Merchants.AnyAsync(x => x.Code == normalizedCode);
        }

        /// <inheritdoc />
        public async Task<bool> ExistsBySlugAsync(string slug)
        {
            if (string.IsNullOrWhiteSpace(slug))
                return false;

            var normalized = slug.Trim().ToLowerInvariant();

            return await _context.Merchants.AnyAsync(x => x.Slug == normalized);
        }

        /// <inheritdoc />
        public async Task AddAsync(Merchant merchant)
        {
            ArgumentNullException.ThrowIfNull(merchant);

            await _context.Merchants.AddAsync(merchant);
        }

        /// <inheritdoc />
        public void Update(Merchant merchant)
        {
            ArgumentNullException.ThrowIfNull(merchant);

            _context.Merchants.Update(merchant);
        }

        /// <inheritdoc />
        public void Delete(Merchant merchant)
        {
            ArgumentNullException.ThrowIfNull(merchant);

            _context.Merchants.Remove(merchant);
        }

        /// <inheritdoc />
        public async Task<bool> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync() > 0;
        }

        // ─── Public discovery ────────────────────────────────────────
        //
        // Defaults applied here (NOT on the DTO) so a future caller
        // passing 0 / out-of-range values still gets sane behaviour.
        private const int DefaultPageSize = 20;
        private const int MaxPageSize = 100;
        private const decimal DefaultRadiusKm = 25m;
        // Approx kilometres per degree of latitude. Constant within ~0.5%
        // anywhere on Earth. Longitude varies with latitude (cos factor)
        // and is computed inline below.
        private const double KmPerDegreeLat = 111.0;

        /// <inheritdoc />
        public async Task<(List<Merchant> Items, int Total)> SearchPublicAsync(
            MerchantPublicFilterRequestDto filter)
        {
            ArgumentNullException.ThrowIfNull(filter);

            // ── SQL-side filters ─────────────────────────────────────
            //
            // Status==Active is non-negotiable on the public route.
            // Pending / Inactive / Suspended merchants must NEVER reach
            // an unauthenticated caller. Enforcing it here (rather than
            // only in the service) means a future consumer that forgets
            // the filter can't accidentally leak them.
            IQueryable<Merchant> query = _context.Merchants
                .AsNoTracking()
                .Include(x => x.SellerCategory)
                .Where(x => x.Status == MerchantStatus.Active);

            if (!string.IsNullOrWhiteSpace(filter.Province))
            {
                var province = filter.Province.Trim();
                query = query.Where(x => x.Province == province);
            }

            if (!string.IsNullOrWhiteSpace(filter.City))
            {
                var city = filter.City.Trim();
                query = query.Where(x => x.City == city);
            }

            if (!string.IsNullOrWhiteSpace(filter.Category))
            {
                var category = filter.Category.Trim();
                // Category match runs against the navigation's Name —
                // SellerCategoryId is opaque to public callers.
                query = query.Where(x =>
                    x.SellerCategory != null &&
                    x.SellerCategory.Name == category);
            }

            if (!string.IsNullOrWhiteSpace(filter.Q))
            {
                // Same LIKE-on-name/desc/city/suburb/category pattern as
                // marketplace-listings search. Cheap on its own — the
                // covering indexes on Name / City exist; Description has
                // no index but the row count keeps it acceptable.
                var q = filter.Q.Trim();
                var like = $"%{q}%";
                query = query.Where(x =>
                    EF.Functions.Like(x.Name, like) ||
                    (x.Description != null && EF.Functions.Like(x.Description, like)) ||
                    (x.City != null && EF.Functions.Like(x.City, like)) ||
                    (x.Suburb != null && EF.Functions.Like(x.Suburb, like)) ||
                    (x.SellerCategory != null && EF.Functions.Like(x.SellerCategory.Name, like)));
            }

            // ── Geo branch ───────────────────────────────────────────
            var hasCoords = filter.Lat.HasValue && filter.Lng.HasValue;
            if (hasCoords)
            {
                var userLat = (double)filter.Lat!.Value;
                var userLng = (double)filter.Lng!.Value;
                var radiusKm = filter.RadiusKm.HasValue && filter.RadiusKm.Value > 0
                    ? (double)filter.RadiusKm.Value
                    : (double)DefaultRadiusKm;

                // Coarse bbox prefilter — runs in SQL using the
                // composite (Latitude, Longitude) index. We inflate the
                // longitude delta with a cosine factor that depends on
                // latitude so the bbox stays correct near the poles.
                // Far from the poles (South Africa lives between
                // ~-22° and ~-35°) this is a tight rectangle.
                var latDelta = radiusKm / KmPerDegreeLat;
                var cos = System.Math.Cos(userLat * System.Math.PI / 180.0);
                // Guard against cos approaching 0 near the poles — would
                // explode the longitude window. Clamp the divisor.
                var lngDelta = radiusKm / (KmPerDegreeLat * System.Math.Max(0.01, System.Math.Abs(cos)));

                var minLat = (decimal)(userLat - latDelta);
                var maxLat = (decimal)(userLat + latDelta);
                var minLng = (decimal)(userLng - lngDelta);
                var maxLng = (decimal)(userLng + lngDelta);

                query = query.Where(x =>
                    x.Latitude.HasValue && x.Longitude.HasValue &&
                    x.Latitude >= minLat && x.Latitude <= maxLat &&
                    x.Longitude >= minLng && x.Longitude <= maxLng);

                // Materialise the bbox-bounded set, then refine with
                // exact Haversine and sort/paginate in memory. The
                // bbox keeps the candidate set small even on a busy
                // table so the in-memory pass stays cheap.
                var candidates = await query.ToListAsync();

                var withDistance = new List<(Merchant Merchant, double DistanceKm)>(candidates.Count);
                foreach (var m in candidates)
                {
                    var d = Haversine.DistanceKm(
                        userLat, userLng,
                        (double)m.Latitude!.Value, (double)m.Longitude!.Value);
                    if (d <= radiusKm)
                        withDistance.Add((m, d));
                }

                // Cache distance back onto a sidecar dictionary the
                // service layer reads via the wrapper return — but the
                // existing repository contract returns plain entities,
                // so we instead stash distance into Merchant.* with a
                // throwaway field? No — keep the contract simple: sort
                // here and return entities. The service recomputes
                // distance for the DTO using the same Haversine call.
                // (Recomputation is cheap; ≤ pageSize entries per call.)
                var sort = NormaliseSort(filter.Sort, hasCoords: true);
                IEnumerable<(Merchant Merchant, double DistanceKm)> ordered = sort switch
                {
                    "rating"  => withDistance
                                   .OrderByDescending(x => x.Merchant.KycStatus == MerchantKycStatus.Verified)
                                   .ThenByDescending(x => x.Merchant.Rating ?? 0m)
                                   .ThenByDescending(x => x.Merchant.ReviewCount),
                    "newest"  => withDistance.OrderByDescending(x => x.Merchant.CreatedAtUtc),
                    _ /*nearest*/ => withDistance.OrderBy(x => x.DistanceKm),
                };

                var total = withDistance.Count;
                var (page, pageSize) = ClampPaging(filter.Page, filter.PageSize);
                var items = ordered
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(x => x.Merchant)
                    .ToList();

                return (items, total);
            }

            // ── Non-geo branch — plain SQL paging ────────────────────
            {
                var sort = NormaliseSort(filter.Sort, hasCoords: false);
                query = sort switch
                {
                    "newest" => query.OrderByDescending(x => x.CreatedAtUtc),
                    _ /*rating*/ => query
                        .OrderByDescending(x => x.KycStatus == MerchantKycStatus.Verified)
                        .ThenByDescending(x => x.Rating ?? 0m)
                        .ThenByDescending(x => x.ReviewCount)
                        .ThenByDescending(x => x.CreatedAtUtc),
                };

                var total = await query.CountAsync();
                var (page, pageSize) = ClampPaging(filter.Page, filter.PageSize);

                var items = await query
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                return (items, total);
            }
        }

        /// <inheritdoc />
        public async Task<Merchant?> GetPublicByIdAsync(Guid id)
        {
            // Status filter mirrors SearchPublicAsync — non-Active
            // merchants resolve to null, which the controller maps to
            // a 404 indistinguishable from "no such id". Hides the
            // existence of Pending/Suspended merchants.
            return await _context.Merchants
                .AsNoTracking()
                .Include(x => x.SellerCategory)
                .Where(x => x.Status == MerchantStatus.Active)
                .FirstOrDefaultAsync(x => x.Id == id);
        }

        // ─── Helpers ─────────────────────────────────────────────────

        private static string NormaliseSort(string? raw, bool hasCoords)
        {
            var s = (raw ?? string.Empty).Trim().ToLowerInvariant();
            return s switch
            {
                "nearest" => hasCoords ? "nearest" : "rating",
                "rating"  => "rating",
                "newest"  => "newest",
                _ => hasCoords ? "nearest" : "rating",
            };
        }

        private static (int Page, int PageSize) ClampPaging(int page, int pageSize)
        {
            var p = page <= 0 ? 1 : page;
            var ps = pageSize <= 0 ? DefaultPageSize : pageSize;
            if (ps > MaxPageSize) ps = MaxPageSize;
            return (p, ps);
        }

    }
}
