using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ZansiHustle.Application.Persistence.SellerCategories;
using ZansiHustle.Application.SellerCategories.Dtos;
using ZansiHustle.Domain.SellerCategories;
using ZansiHustle.Shared.Results;

namespace ZansiHustle.Application.SellerCategories
{
    public class SellerCategoryService : ISellerCategoryService
    {
        private readonly ISellerCategoryRepository _repository;

        public SellerCategoryService(ISellerCategoryRepository repository)
        {
            _repository = repository;
        }

        public async Task<Result<List<SellerCategoryDto>>> GetAllAsync(bool activeOnly = false)
        {
            var items = await _repository.GetAllAsync(activeOnly);
            return Result<List<SellerCategoryDto>>.Success(Map(items), "Seller categories retrieved successfully.");
        }

        public async Task<Result<SellerCategoryDto>> GetByIdAsync(Guid id)
        {
            var item = await _repository.GetByIdAsync(id);
            if (item == null)
            {
                return Result<SellerCategoryDto>.Failure("NOT_FOUND", "Seller category was not found.");
            }

            return Result<SellerCategoryDto>.Success(Map(item), "Seller category retrieved successfully.");
        }

        public async Task<Result<SellerCategoryDto>> CreateAsync(CreateSellerCategoryRequestDto request)
        {
            if (string.IsNullOrWhiteSpace(request.Name))
            {
                return Result<SellerCategoryDto>.Failure("BAD_REQUEST", "Category name is required.");
            }

            if (await _repository.ExistsByNameAsync(request.Name))
            {
                return Result<SellerCategoryDto>.Failure("CONFLICT", "A seller category with the same name already exists.");
            }

            var category = new SellerCategory
            {
                Name = request.Name.Trim(),
                Slug = ToSlug(request.Name),
                Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
                SortOrder = request.SortOrder,
                IsActive = request.IsActive,
                CreatedAtUtc = DateTime.UtcNow
            };

            foreach (var sub in request.Subcategories ?? new List<CreateSellerSubcategoryRequestDto>())
            {
                if (string.IsNullOrWhiteSpace(sub.Name))
                    continue;

                category.Subcategories.Add(new SellerSubcategory
                {
                    SellerCategoryId = category.Id,
                    Name = sub.Name.Trim(),
                    Slug = ToSlug(sub.Name),
                    Description = string.IsNullOrWhiteSpace(sub.Description) ? null : sub.Description.Trim(),
                    SortOrder = sub.SortOrder,
                    IsActive = sub.IsActive,
                    CreatedAtUtc = DateTime.UtcNow
                });
            }

            await _repository.AddAsync(category);

            if (!await _repository.SaveChangesAsync())
            {
                return Result<SellerCategoryDto>.Failure("BAD_REQUEST", "Unable to create seller category.");
            }

            var created = await _repository.GetByIdAsync(category.Id) ?? category;
            return Result<SellerCategoryDto>.Success(Map(created), "Seller category created successfully.");
        }

        public async Task<Result<SellerCategoryDto>> UpdateAsync(Guid id, UpdateSellerCategoryRequestDto request)
        {
            var category = await _repository.GetByIdAsync(id);
            if (category == null)
            {
                return Result<SellerCategoryDto>.Failure("NOT_FOUND", "Seller category was not found.");
            }

            if (string.IsNullOrWhiteSpace(request.Name))
            {
                return Result<SellerCategoryDto>.Failure("BAD_REQUEST", "Category name is required.");
            }

            if (await _repository.ExistsByNameAsync(request.Name, id))
            {
                return Result<SellerCategoryDto>.Failure("CONFLICT", "A seller category with the same name already exists.");
            }

            category.Name = request.Name.Trim();
            category.Slug = ToSlug(request.Name);
            category.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();
            category.SortOrder = request.SortOrder;
            category.IsActive = request.IsActive;
            category.UpdatedAtUtc = DateTime.UtcNow;

            _repository.Update(category);

            if (!await _repository.SaveChangesAsync())
            {
                return Result<SellerCategoryDto>.Failure("BAD_REQUEST", "Unable to update seller category.");
            }

            var updated = await _repository.GetByIdAsync(category.Id) ?? category;
            return Result<SellerCategoryDto>.Success(Map(updated), "Seller category updated successfully.");
        }

        public async Task<Result> DeleteAsync(Guid id)
        {
            var category = await _repository.GetByIdAsync(id);
            if (category == null)
            {
                return Result.Failure("NOT_FOUND", "Seller category was not found.");
            }

            _repository.Delete(category);

            if (!await _repository.SaveChangesAsync())
            {
                return Result.Failure("BAD_REQUEST", "Unable to delete seller category.");
            }

            return Result.Success("Seller category deleted successfully.");
        }

        public async Task<Result<SellerSubcategoryDto>> CreateSubcategoryAsync(CreateSellerSubcategoryRequestDto request)
        {
            if (request.SellerCategoryId == Guid.Empty)
            {
                return Result<SellerSubcategoryDto>.Failure("BAD_REQUEST", "Seller category is required.");
            }

            if (string.IsNullOrWhiteSpace(request.Name))
            {
                return Result<SellerSubcategoryDto>.Failure("BAD_REQUEST", "Subcategory name is required.");
            }

            var parent = await _repository.GetByIdAsync(request.SellerCategoryId);
            if (parent == null)
            {
                return Result<SellerSubcategoryDto>.Failure("NOT_FOUND", "Parent seller category was not found.");
            }

            if (await _repository.SubcategoryExistsByNameAsync(request.SellerCategoryId, request.Name))
            {
                return Result<SellerSubcategoryDto>.Failure("CONFLICT", "A subcategory with the same name already exists under this category.");
            }

            var sub = new SellerSubcategory
            {
                SellerCategoryId = request.SellerCategoryId,
                Name = request.Name.Trim(),
                Slug = ToSlug(request.Name),
                Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
                SortOrder = request.SortOrder,
                IsActive = request.IsActive,
                CreatedAtUtc = DateTime.UtcNow
            };

            await _repository.AddSubcategoryAsync(sub);

            if (!await _repository.SaveChangesAsync())
            {
                return Result<SellerSubcategoryDto>.Failure("BAD_REQUEST", "Unable to create seller subcategory.");
            }

            var created = await _repository.GetSubcategoryByIdAsync(sub.Id) ?? sub;
            return Result<SellerSubcategoryDto>.Success(Map(created), "Seller subcategory created successfully.");
        }

        public async Task<Result<SellerSubcategoryDto>> UpdateSubcategoryAsync(Guid id, UpdateSellerSubcategoryRequestDto request)
        {
            var sub = await _repository.GetSubcategoryByIdAsync(id);
            if (sub == null)
            {
                return Result<SellerSubcategoryDto>.Failure("NOT_FOUND", "Seller subcategory was not found.");
            }

            if (string.IsNullOrWhiteSpace(request.Name))
            {
                return Result<SellerSubcategoryDto>.Failure("BAD_REQUEST", "Subcategory name is required.");
            }

            if (await _repository.SubcategoryExistsByNameAsync(sub.SellerCategoryId, request.Name, id))
            {
                return Result<SellerSubcategoryDto>.Failure("CONFLICT", "A subcategory with the same name already exists under this category.");
            }

            sub.Name = request.Name.Trim();
            sub.Slug = ToSlug(request.Name);
            sub.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();
            sub.SortOrder = request.SortOrder;
            sub.IsActive = request.IsActive;
            sub.UpdatedAtUtc = DateTime.UtcNow;

            _repository.UpdateSubcategory(sub);

            if (!await _repository.SaveChangesAsync())
            {
                return Result<SellerSubcategoryDto>.Failure("BAD_REQUEST", "Unable to update seller subcategory.");
            }

            var updated = await _repository.GetSubcategoryByIdAsync(sub.Id) ?? sub;
            return Result<SellerSubcategoryDto>.Success(Map(updated), "Seller subcategory updated successfully.");
        }

        public async Task<Result> DeleteSubcategoryAsync(Guid id)
        {
            var sub = await _repository.GetSubcategoryByIdAsync(id);
            if (sub == null)
            {
                return Result.Failure("NOT_FOUND", "Seller subcategory was not found.");
            }

            _repository.DeleteSubcategory(sub);

            if (!await _repository.SaveChangesAsync())
            {
                return Result.Failure("BAD_REQUEST", "Unable to delete seller subcategory.");
            }

            return Result.Success("Seller subcategory deleted successfully.");
        }

        private static List<SellerCategoryDto> Map(List<SellerCategory> items)
        {
            return items
                .OrderBy(x => x.SortOrder)
                .ThenBy(x => x.Name)
                .Select(Map)
                .ToList();
        }

        private static SellerCategoryDto Map(SellerCategory item)
        {
            return new SellerCategoryDto
            {
                Id = item.Id,
                Name = item.Name,
                Slug = item.Slug,
                Description = item.Description,
                SortOrder = item.SortOrder,
                IsActive = item.IsActive,
                Subcategories = item.Subcategories
                    .OrderBy(x => x.SortOrder)
                    .ThenBy(x => x.Name)
                    .Select(Map)
                    .ToList()
            };
        }

        private static SellerSubcategoryDto Map(SellerSubcategory item)
        {
            return new SellerSubcategoryDto
            {
                Id = item.Id,
                SellerCategoryId = item.SellerCategoryId,
                Name = item.Name,
                Slug = item.Slug,
                Description = item.Description,
                SortOrder = item.SortOrder,
                IsActive = item.IsActive
            };
        }

        private static string ToSlug(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;

            return value.Trim()
                .ToLowerInvariant()
                .Replace("&", "and")
                .Replace("/", "-")
                .Replace("(", string.Empty)
                .Replace(")", string.Empty)
                .Replace(".", string.Empty)
                .Replace(",", string.Empty)
                .Replace("'", string.Empty)
                .Replace("  ", " ")
                .Replace(" ", "-");
        }
    }
}