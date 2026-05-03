using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ZansiHustle.Application.Marketplace.Dtos;
using ZansiHustle.Application.Persistence.Marketplace;
using ZansiHustle.Domain.Marketplace;
using ZansiHustle.Infrastructure.Data;

namespace ZansiHustle.Infrastructure.Persistence.Marketplace
{
    /// <inheritdoc />
    public class MarketplaceListingRepository : IMarketplaceListingRepository
    {
        private readonly AppDbContext _context;

        public MarketplaceListingRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<(List<MarketplaceListing> Items, int Total)> SearchAsync(MarketplaceListingFilterRequestDto filter)
        {
            // Owner navigation is always included — the buyer-side card
            // needs SellerName + SellerVerified, and an in-memory join
            // here is cheaper than an N+1 over UserManager later.
            IQueryable<MarketplaceListing> query = _context.MarketplaceListings
                .AsNoTracking()
                .Include(x => x.Owner)
                .Include(x => x.Images);

            if (filter.Status.HasValue)
                query = query.Where(x => x.Status == filter.Status.Value);

            if (filter.Condition.HasValue)
                query = query.Where(x => x.Condition == filter.Condition.Value);

            if (!string.IsNullOrWhiteSpace(filter.Category))
                query = query.Where(x => x.Category == filter.Category);

            if (!string.IsNullOrWhiteSpace(filter.Province))
                query = query.Where(x => x.Province == filter.Province);

            if (filter.MinPrice.HasValue)
                query = query.Where(x => x.Price >= filter.MinPrice.Value);

            if (filter.MaxPrice.HasValue)
                query = query.Where(x => x.Price <= filter.MaxPrice.Value);

            if (!string.IsNullOrWhiteSpace(filter.Q))
            {
                var q = filter.Q.Trim();
                query = query.Where(x =>
                    EF.Functions.Like(x.Title, $"%{q}%") ||
                    EF.Functions.Like(x.Description, $"%{q}%"));
            }

            query = ApplySort(query, filter.Sort);

            var total = await query.CountAsync();

            var page = filter.Page <= 0 ? 1 : filter.Page;
            var pageSize = filter.PageSize <= 0
                ? 20
                : (filter.PageSize > 100 ? 100 : filter.PageSize);

            var items = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, total);
        }

        public async Task<MarketplaceListing?> GetByIdAsync(Guid id)
        {
            return await _context.MarketplaceListings
                .Include(x => x.Owner)
                .Include(x => x.Images)
                .FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<List<MarketplaceListing>> GetByOwnerAsync(Guid ownerUserId)
        {
            return await _context.MarketplaceListings
                .AsNoTracking()
                .Include(x => x.Owner)
                .Include(x => x.Images)
                .Where(x => x.OwnerUserId == ownerUserId)
                .OrderByDescending(x => x.CreatedAtUtc)
                .ToListAsync();
        }

        public async Task AddAsync(MarketplaceListing listing)
        {
            ArgumentNullException.ThrowIfNull(listing);
            await _context.MarketplaceListings.AddAsync(listing);
        }

        public void Update(MarketplaceListing listing)
        {
            ArgumentNullException.ThrowIfNull(listing);
            _context.MarketplaceListings.Update(listing);
        }

        public void Delete(MarketplaceListing listing)
        {
            ArgumentNullException.ThrowIfNull(listing);
            _context.MarketplaceListings.Remove(listing);
        }

        public async Task AddImageAsync(MarketplaceListingImage image)
        {
            ArgumentNullException.ThrowIfNull(image);
            await _context.MarketplaceListingImages.AddAsync(image);
        }

        public async Task<bool> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync() > 0;
        }

        private static IQueryable<MarketplaceListing> ApplySort(IQueryable<MarketplaceListing> query, string? sort)
        {
            return (sort ?? "newest").ToLowerInvariant() switch
            {
                "price_asc"  => query.OrderBy(x => x.Price).ThenByDescending(x => x.CreatedAtUtc),
                "price_desc" => query.OrderByDescending(x => x.Price).ThenByDescending(x => x.CreatedAtUtc),
                "featured"   => query.OrderByDescending(x => x.IsFeatured)
                                     .ThenByDescending(x => x.IsBoosted)
                                     .ThenByDescending(x => x.CreatedAtUtc),
                _            => query.OrderByDescending(x => x.IsFeatured)
                                     .ThenByDescending(x => x.IsBoosted)
                                     .ThenByDescending(x => x.CreatedAtUtc),
            };
        }
    }
}
