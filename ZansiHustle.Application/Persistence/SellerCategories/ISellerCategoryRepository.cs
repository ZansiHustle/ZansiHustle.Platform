using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ZansiHustle.Domain.SellerCategories;

namespace ZansiHustle.Application.Persistence.SellerCategories
{
    /// <summary>
    /// Repository contract for seller categories and subcategories.
    /// </summary>
    public interface ISellerCategoryRepository
    {
        Task<List<SellerCategory>> GetAllAsync(bool activeOnly = false);
        Task<SellerCategory?> GetByIdAsync(Guid id);
        Task<SellerCategory?> GetBySlugAsync(string slug);
        Task<bool> ExistsByNameAsync(string name, Guid? excludeId = null);

        Task AddAsync(SellerCategory category);
        void Update(SellerCategory category);
        void Delete(SellerCategory category);

        Task<SellerSubcategory?> GetSubcategoryByIdAsync(Guid id);
        Task<List<SellerSubcategory>> GetSubcategoriesByCategoryIdAsync(Guid sellerCategoryId, bool activeOnly = false);
        Task<bool> SubcategoryExistsByNameAsync(Guid sellerCategoryId, string name, Guid? excludeId = null);

        Task AddSubcategoryAsync(SellerSubcategory subcategory);
        void UpdateSubcategory(SellerSubcategory subcategory);
        void DeleteSubcategory(SellerSubcategory subcategory);

        Task<bool> SaveChangesAsync();
    }
}