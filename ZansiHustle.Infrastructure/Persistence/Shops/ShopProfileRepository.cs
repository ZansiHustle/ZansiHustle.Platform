using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ZansiHustle.Application.Common.Paging;
using ZansiHustle.Application.Persistence.Shops;
using ZansiHustle.Domain.Shops;
using ZansiHustle.Infrastructure.Data;
using ZansiHustle.Shared.Enums.Shops;

namespace ZansiHustle.Infrastructure.Persistence.Shops
{
    public class ShopProfileRepository : IShopProfileRepository
    {
        private readonly AppDbContext _context;

        public ShopProfileRepository(AppDbContext context)
        {
            _context = context;
        }

        public Task<ShopProfile?> GetByIdAsync(Guid id)
            => _context.ShopProfiles.FirstOrDefaultAsync(s => s.Id == id);

        public Task<ShopProfile?> GetBySlugAsync(string slug)
            => _context.ShopProfiles.FirstOrDefaultAsync(s => s.Slug == slug);

        public Task<ShopProfile?> GetMineAsync(Guid ownerUserId)
        {
            // Resolves through Merchant.OwnerUserId so the shop is
            // discoverable from a JWT user id without a denormalised
            // OwnerUserId column on the shop row itself.
            return (
                from shop in _context.ShopProfiles
                join merchant in _context.Merchants on shop.MerchantId equals merchant.Id
                where merchant.OwnerUserId == ownerUserId
                      && shop.Status != ShopProfileStatus.Suspended
                orderby shop.CreatedAtUtc descending
                select shop
            ).FirstOrDefaultAsync();
        }

        public async Task<List<ShopProfile>> GetAllMineAsync(Guid ownerUserId)
        {
            // Same ownership join + status filter as GetMineAsync, but returns
            // every matching shop (Admin/SuperAdmin can own several). Newest
            // first so the most recently created shop heads the My Shops list.
            return await (
                from shop in _context.ShopProfiles
                join merchant in _context.Merchants on shop.MerchantId equals merchant.Id
                where merchant.OwnerUserId == ownerUserId
                      && shop.Status != ShopProfileStatus.Suspended
                orderby shop.CreatedAtUtc descending
                select shop
            ).ToListAsync();
        }

        public Task<int> CountMineAsync(Guid ownerUserId)
        {
            return (
                from shop in _context.ShopProfiles
                join merchant in _context.Merchants on shop.MerchantId equals merchant.Id
                where merchant.OwnerUserId == ownerUserId
                      && shop.Status != ShopProfileStatus.Suspended
                select shop.Id
            ).CountAsync();
        }

        public Task<ShopProfile?> GetActiveByMerchantAsync(Guid merchantId)
        {
            return _context.ShopProfiles.FirstOrDefaultAsync(s =>
                s.MerchantId == merchantId
                && s.Status != ShopProfileStatus.Suspended);
        }

        public Task<bool> SlugExistsAsync(string slug, Guid? excludingId = null)
        {
            return _context.ShopProfiles.AnyAsync(s =>
                s.Slug == slug
                && (excludingId == null || s.Id != excludingId.Value));
        }

        public async Task<PagedResult<ShopProfile>> SearchPublicAsync(int page, int pageSize, string? q)
        {
            var query = _context.ShopProfiles
                .AsNoTracking()
                // Public discovery: lifecycle-Active AND buyer-visible. A
                // seller-paused (or under-review/blocked) shop drops out of
                // search/list; default Visible leaves existing shops unaffected.
                .Where(s => s.Status == ShopProfileStatus.Active
                    && s.VisibilityStatus == ShopVisibilityStatus.Visible);

            if (!string.IsNullOrWhiteSpace(q))
            {
                var needle = q.Trim();
                query = query.Where(s =>
                    EF.Functions.Like(s.Name, $"%{needle}%")
                    || (s.City != null && EF.Functions.Like(s.City, $"%{needle}%")));
            }

            var total = await query.CountAsync();
            var items = await query
                .OrderByDescending(s => s.CreatedAtUtc)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new PagedResult<ShopProfile>
            {
                Items = items,
                Total = total,
                Page = page,
                PageSize = pageSize,
            };
        }

        public async Task<PagedResult<ShopProfile>> SearchAdminAsync(int page, int pageSize, ShopProfileStatus? status, string? q)
        {
            var query = _context.ShopProfiles.AsNoTracking();

            if (status.HasValue)
                query = query.Where(s => s.Status == status.Value);

            if (!string.IsNullOrWhiteSpace(q))
            {
                var needle = q.Trim();
                query = query.Where(s =>
                    EF.Functions.Like(s.Name, $"%{needle}%")
                    || EF.Functions.Like(s.Slug, $"%{needle}%"));
            }

            var total = await query.CountAsync();
            var items = await query
                .OrderByDescending(s => s.CreatedAtUtc)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new PagedResult<ShopProfile>
            {
                Items = items,
                Total = total,
                Page = page,
                PageSize = pageSize,
            };
        }

        public async Task AddAsync(ShopProfile shop)
            => await _context.ShopProfiles.AddAsync(shop);

        public void Update(ShopProfile shop)
            => _context.ShopProfiles.Update(shop);

        public async Task<bool> SaveChangesAsync()
            => await _context.SaveChangesAsync() > 0;
    }
}
