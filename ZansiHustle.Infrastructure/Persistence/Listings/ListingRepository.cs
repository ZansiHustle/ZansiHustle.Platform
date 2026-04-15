using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ZansiHustle.Application.Listings.Dtos;
using ZansiHustle.Application.Persistence.Listings;
using ZansiHustle.Domain.Listings;
using ZansiHustle.Infrastructure.Data;

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
                .Include(x => x.SellerCategory)
                .Include(x => x.SellerSubcategory);

            if (filter.Type.HasValue)
                query = query.Where(x => x.Type == filter.Type.Value);

            if (filter.Status.HasValue)
                query = query.Where(x => x.Status == filter.Status.Value);

            if (filter.MerchantId.HasValue)
                query = query.Where(x => x.MerchantId == filter.MerchantId.Value);

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
                .Include(x => x.SellerCategory)
                .Include(x => x.SellerSubcategory)
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
                .Include(x => x.SellerCategory)
                .Include(x => x.SellerSubcategory)
                .FirstOrDefaultAsync(x => x.Slug == normalized);
        }

        /// <inheritdoc />
        public async Task<List<Listing>> GetByMerchantAsync(Guid merchantId)
        {
            return await _context.Listings
                .AsNoTracking()
                .Include(x => x.Merchant)
                .Include(x => x.SellerCategory)
                .Include(x => x.SellerSubcategory)
                .Where(x => x.MerchantId == merchantId)
                .OrderByDescending(x => x.CreatedAtUtc)
                .ToListAsync();
        }

        /// <inheritdoc />
        public async Task<List<Listing>> GetByOwnerAsync(Guid ownerUserId)
        {
            return await _context.Listings
                .AsNoTracking()
                .Include(x => x.Merchant)
                .Include(x => x.SellerCategory)
                .Include(x => x.SellerSubcategory)
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

            _context.Listings.Update(listing);
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
