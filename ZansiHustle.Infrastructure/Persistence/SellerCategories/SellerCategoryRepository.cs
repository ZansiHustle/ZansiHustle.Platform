using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ZansiHustle.Application.Persistence.SellerCategories;
using ZansiHustle.Domain.SellerCategories;
using ZansiHustle.Infrastructure.Data;

namespace ZansiHustle.Infrastructure.Persistence.SellerCategories
{
    public class SellerCategoryRepository : ISellerCategoryRepository
    {
        private readonly AppDbContext _context;

        public SellerCategoryRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<SellerCategory>> GetAllAsync(bool activeOnly = false)
        {
            var query = _context.SellerCategories
                .Include(x => x.Subcategories)
                .AsNoTracking()
                .AsQueryable();

            if (activeOnly)
            {
                query = query.Where(x => x.IsActive);
            }

            return await query
                .OrderBy(x => x.SortOrder)
                .ThenBy(x => x.Name)
                .ToListAsync();
        }

        public async Task<SellerCategory?> GetByIdAsync(Guid id)
        {
            return await _context.SellerCategories
                .Include(x => x.Subcategories)
                .FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<SellerCategory?> GetBySlugAsync(string slug)
        {
            if (string.IsNullOrWhiteSpace(slug))
                return null;

            var normalized = slug.Trim().ToLowerInvariant();

            return await _context.SellerCategories
                .Include(x => x.Subcategories)
                .FirstOrDefaultAsync(x => x.Slug.ToLower() == normalized);
        }

        public async Task<bool> ExistsByNameAsync(string name, Guid? excludeId = null)
        {
            if (string.IsNullOrWhiteSpace(name))
                return false;

            var normalized = name.Trim().ToLowerInvariant();

            return await _context.SellerCategories.AnyAsync(x =>
                x.Name.ToLower() == normalized &&
                (!excludeId.HasValue || x.Id != excludeId.Value));
        }

        public async Task AddAsync(SellerCategory category)
        {
            ArgumentNullException.ThrowIfNull(category);
            await _context.SellerCategories.AddAsync(category);
        }

        public void Update(SellerCategory category)
        {
            ArgumentNullException.ThrowIfNull(category);
            _context.SellerCategories.Update(category);
        }

        public void Delete(SellerCategory category)
        {
            ArgumentNullException.ThrowIfNull(category);
            _context.SellerCategories.Remove(category);
        }

        public async Task<SellerSubcategory?> GetSubcategoryByIdAsync(Guid id)
        {
            return await _context.SellerSubcategories
                .Include(x => x.SellerCategory)
                .FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<List<SellerSubcategory>> GetSubcategoriesByCategoryIdAsync(Guid sellerCategoryId, bool activeOnly = false)
        {
            var query = _context.SellerSubcategories
                .AsNoTracking()
                .Where(x => x.SellerCategoryId == sellerCategoryId);

            if (activeOnly)
            {
                query = query.Where(x => x.IsActive);
            }

            return await query
                .OrderBy(x => x.SortOrder)
                .ThenBy(x => x.Name)
                .ToListAsync();
        }

        public async Task<bool> SubcategoryExistsByNameAsync(Guid sellerCategoryId, string name, Guid? excludeId = null)
        {
            if (string.IsNullOrWhiteSpace(name))
                return false;

            var normalized = name.Trim().ToLowerInvariant();

            return await _context.SellerSubcategories.AnyAsync(x =>
                x.SellerCategoryId == sellerCategoryId &&
                x.Name.ToLower() == normalized &&
                (!excludeId.HasValue || x.Id != excludeId.Value));
        }

        public async Task AddSubcategoryAsync(SellerSubcategory subcategory)
        {
            ArgumentNullException.ThrowIfNull(subcategory);
            await _context.SellerSubcategories.AddAsync(subcategory);
        }

        public void UpdateSubcategory(SellerSubcategory subcategory)
        {
            ArgumentNullException.ThrowIfNull(subcategory);
            _context.SellerSubcategories.Update(subcategory);
        }

        public void DeleteSubcategory(SellerSubcategory subcategory)
        {
            ArgumentNullException.ThrowIfNull(subcategory);
            _context.SellerSubcategories.Remove(subcategory);
        }

        public async Task<bool> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync() > 0;
        }
    }
}