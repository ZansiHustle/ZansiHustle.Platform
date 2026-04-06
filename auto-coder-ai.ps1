param(
    [string]$Root = ".",
    [switch]$PatchAppDbContext,
    [switch]$PatchServiceExtensions
)

$ErrorActionPreference = "Stop"

function Ensure-Dir {
    param([string]$Path)
    if (-not (Test-Path $Path)) {
        New-Item -ItemType Directory -Path $Path -Force | Out-Null
    }
}

function Write-Utf8File {
    param(
        [string]$Path,
        [string]$Content
    )

    $dir = Split-Path $Path -Parent
    Ensure-Dir $dir
    [System.IO.File]::WriteAllText($Path, $Content, [System.Text.UTF8Encoding]::new($false))
    Write-Host "Created: $Path" -ForegroundColor Green
}

$rootPath = (Resolve-Path $Root).Path

$domainPath = Join-Path $rootPath "ZansiHustle.Domain\SellerCategories"
$appSellerCategoriesPath = Join-Path $rootPath "ZansiHustle.Application\SellerCategories"
$appDtosPath = Join-Path $appSellerCategoriesPath "Dtos"
$appPersistencePath = Join-Path $rootPath "ZansiHustle.Application\Persistence\SellerCategories"
$infraRepoPath = Join-Path $rootPath "ZansiHustle.Infrastructure\Persistence\SellerCategories"
$infraConfigPath = Join-Path $rootPath "ZansiHustle.Infrastructure\Data\Configurations\SellerCategories"
$infraSeedPath = Join-Path $rootPath "ZansiHustle.Infrastructure\Data\Seed"
$apiControllersPath = Join-Path $rootPath "ZansiHustle.API\Controllers"

# =========================
# Domain
# =========================

Write-Utf8File (Join-Path $domainPath "SellerCategory.cs") @'
using System;
using System.Collections.Generic;

namespace ZansiHustle.Domain.SellerCategories
{
    /// <summary>
    /// Top-level seller category used for structured marketplace/service discovery.
    /// </summary>
    public class SellerCategory
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>
        /// Display name of the category.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// URL-friendly slug for frontend usage and analytics.
        /// </summary>
        public string Slug { get; set; } = string.Empty;

        /// <summary>
        /// Optional description shown in admin or future app experiences.
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Controls display ordering.
        /// </summary>
        public int SortOrder { get; set; }

        /// <summary>
        /// Allows soft disabling without deleting history.
        /// </summary>
        public bool IsActive { get; set; } = true;

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAtUtc { get; set; }

        public ICollection<SellerSubcategory> Subcategories { get; set; } = new List<SellerSubcategory>();
    }
}
'@

Write-Utf8File (Join-Path $domainPath "SellerSubcategory.cs") @'
using System;

namespace ZansiHustle.Domain.SellerCategories
{
    /// <summary>
    /// Child subcategory mapped to a top-level seller category.
    /// </summary>
    public class SellerSubcategory
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid SellerCategoryId { get; set; }

        /// <summary>
        /// Display name of the subcategory.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// URL-friendly slug for frontend usage and analytics.
        /// </summary>
        public string Slug { get; set; } = string.Empty;

        /// <summary>
        /// Optional description shown in admin or future app experiences.
        /// </summary>
        public string? Description { get; set; }

        public int SortOrder { get; set; }
        public bool IsActive { get; set; } = true;

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAtUtc { get; set; }

        public SellerCategory? SellerCategory { get; set; }
    }
}
'@

# =========================
# DTOs
# =========================

Write-Utf8File (Join-Path $appDtosPath "SellerSubcategoryDto.cs") @'
using System;

namespace ZansiHustle.Application.SellerCategories.Dtos
{
    public class SellerSubcategoryDto
    {
        public Guid Id { get; set; }
        public Guid SellerCategoryId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int SortOrder { get; set; }
        public bool IsActive { get; set; }
    }
}
'@

Write-Utf8File (Join-Path $appDtosPath "SellerCategoryDto.cs") @'
using System;
using System.Collections.Generic;

namespace ZansiHustle.Application.SellerCategories.Dtos
{
    public class SellerCategoryDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int SortOrder { get; set; }
        public bool IsActive { get; set; }

        public List<SellerSubcategoryDto> Subcategories { get; set; } = new();
    }
}
'@

Write-Utf8File (Join-Path $appDtosPath "CreateSellerCategoryRequestDto.cs") @'
using System.Collections.Generic;

namespace ZansiHustle.Application.SellerCategories.Dtos
{
    public class CreateSellerCategoryRequestDto
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int SortOrder { get; set; }
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Optional initial subcategories to create together with the category.
        /// </summary>
        public List<CreateSellerSubcategoryRequestDto> Subcategories { get; set; } = new();
    }
}
'@

Write-Utf8File (Join-Path $appDtosPath "UpdateSellerCategoryRequestDto.cs") @'
namespace ZansiHustle.Application.SellerCategories.Dtos
{
    public class UpdateSellerCategoryRequestDto
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int SortOrder { get; set; }
        public bool IsActive { get; set; } = true;
    }
}
'@

Write-Utf8File (Join-Path $appDtosPath "CreateSellerSubcategoryRequestDto.cs") @'
using System;

namespace ZansiHustle.Application.SellerCategories.Dtos
{
    public class CreateSellerSubcategoryRequestDto
    {
        public Guid SellerCategoryId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int SortOrder { get; set; }
        public bool IsActive { get; set; } = true;
    }
}
'@

Write-Utf8File (Join-Path $appDtosPath "UpdateSellerSubcategoryRequestDto.cs") @'
namespace ZansiHustle.Application.SellerCategories.Dtos
{
    public class UpdateSellerSubcategoryRequestDto
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int SortOrder { get; set; }
        public bool IsActive { get; set; } = true;
    }
}
'@

# =========================
# Repository interface
# =========================

Write-Utf8File (Join-Path $appPersistencePath "ISellerCategoryRepository.cs") @'
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
'@

# =========================
# Service interface
# =========================

Write-Utf8File (Join-Path $appSellerCategoriesPath "ISellerCategoryService.cs") @'
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ZansiHustle.Application.SellerCategories.Dtos;
using ZansiHustle.Shared.Results;

namespace ZansiHustle.Application.SellerCategories
{
    public interface ISellerCategoryService
    {
        Task<Result<List<SellerCategoryDto>>> GetAllAsync(bool activeOnly = false);
        Task<Result<SellerCategoryDto>> GetByIdAsync(Guid id);

        Task<Result<SellerCategoryDto>> CreateAsync(CreateSellerCategoryRequestDto request);
        Task<Result<SellerCategoryDto>> UpdateAsync(Guid id, UpdateSellerCategoryRequestDto request);
        Task<Result> DeleteAsync(Guid id);

        Task<Result<SellerSubcategoryDto>> CreateSubcategoryAsync(CreateSellerSubcategoryRequestDto request);
        Task<Result<SellerSubcategoryDto>> UpdateSubcategoryAsync(Guid id, UpdateSellerSubcategoryRequestDto request);
        Task<Result> DeleteSubcategoryAsync(Guid id);
    }
}
'@

# =========================
# Service implementation
# =========================

Write-Utf8File (Join-Path $appSellerCategoriesPath "SellerCategoryService.cs") @'
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
'@

# =========================
# Repository implementation
# =========================

# Continue from where it left off...

Write-Utf8File (Join-Path $infraRepoPath "SellerCategoryRepository.cs") @'
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
'@

# =========================
# Entity Framework Configurations
# =========================

Write-Utf8File (Join-Path $infraConfigPath "SellerCategoryConfiguration.cs") @'
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ZansiHustle.Domain.SellerCategories;

namespace ZansiHustle.Infrastructure.Data.Configurations.SellerCategories
{
    public class SellerCategoryConfiguration : IEntityTypeConfiguration<SellerCategory>
    {
        public void Configure(EntityTypeBuilder<SellerCategory> builder)
        {
            builder.ToTable("SellerCategories");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Name)
                .IsRequired()
                .HasMaxLength(100);

            builder.HasIndex(x => x.Name)
                .IsUnique();

            builder.Property(x => x.Slug)
                .IsRequired()
                .HasMaxLength(120);

            builder.HasIndex(x => x.Slug)
                .IsUnique();

            builder.Property(x => x.Description)
                .HasMaxLength(500);

            builder.Property(x => x.SortOrder)
                .HasDefaultValue(0);

            builder.Property(x => x.IsActive)
                .HasDefaultValue(true);

            builder.Property(x => x.CreatedAtUtc)
                .IsRequired();

            builder.Property(x => x.UpdatedAtUtc)
                .IsRequired(false);

            builder.HasMany(x => x.Subcategories)
                .WithOne(x => x.SellerCategory)
                .HasForeignKey(x => x.SellerCategoryId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
'@

Write-Utf8File (Join-Path $infraConfigPath "SellerSubcategoryConfiguration.cs") @'
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ZansiHustle.Domain.SellerCategories;

namespace ZansiHustle.Infrastructure.Data.Configurations.SellerCategories
{
    public class SellerSubcategoryConfiguration : IEntityTypeConfiguration<SellerSubcategory>
    {
        public void Configure(EntityTypeBuilder<SellerSubcategory> builder)
        {
            builder.ToTable("SellerSubcategories");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Name)
                .IsRequired()
                .HasMaxLength(100);

            builder.HasIndex(x => new { x.SellerCategoryId, x.Name })
                .IsUnique();

            builder.Property(x => x.Slug)
                .IsRequired()
                .HasMaxLength(120);

            builder.Property(x => x.Description)
                .HasMaxLength(500);

            builder.Property(x => x.SortOrder)
                .HasDefaultValue(0);

            builder.Property(x => x.IsActive)
                .HasDefaultValue(true);

            builder.Property(x => x.CreatedAtUtc)
                .IsRequired();

            builder.Property(x => x.UpdatedAtUtc)
                .IsRequired(false);
        }
    }
}
'@

# =========================
# Database Seed Data
# =========================

Write-Utf8File (Join-Path $infraSeedPath "SellerCategorySeed.cs") @'
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ZansiHustle.Domain.SellerCategories;
using ZansiHustle.Infrastructure.Data;

namespace ZansiHustle.Infrastructure.Data.Seed
{
    public static class SellerCategorySeed
    {
        public static async Task SeedAsync(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<IServiceProvider>>();

            try
            {
                if (await context.SellerCategories.AnyAsync())
                {
                    logger.LogInformation("Seller categories already seeded.");
                    return;
                }

                logger.LogInformation("Seeding seller categories and subcategories...");

                var categories = GetCategoryData();
                await context.SellerCategories.AddRangeAsync(categories);
                await context.SaveChangesAsync();

                logger.LogInformation("Successfully seeded {Count} seller categories with subcategories.", categories.Count);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while seeding seller categories.");
                throw;
            }
        }

        private static List<SellerCategory> GetCategoryData()
        {
            var now = DateTime.UtcNow;
            var order = 0;

            return new List<SellerCategory>
            {
                new SellerCategory
                {
                    Id = Guid.NewGuid(),
                    Name = "Beauty & Personal Care",
                    Slug = "beauty-personal-care",
                    Description = "Hair, nails, makeup, skincare, and beauty services",
                    SortOrder = order++,
                    IsActive = true,
                    CreatedAtUtc = now,
                    Subcategories = new List<SellerSubcategory>
                    {
                        CreateSubcategory("Barbers", "barbers", order++, now),
                        CreateSubcategory("Hair Stylists", "hair-stylists", order++, now),
                        CreateSubcategory("Braiders", "braiders", order++, now),
                        CreateSubcategory("Nail Technicians", "nail-technicians", order++, now),
                        CreateSubcategory("Makeup Artists", "makeup-artists", order++, now),
                        CreateSubcategory("Beauty Salons", "beauty-salons", order++, now),
                        CreateSubcategory("Lash Technicians", "lash-technicians", order++, now),
                        CreateSubcategory("Waxing Specialists", "waxing-specialists", order++, now),
                        CreateSubcategory("Skincare Specialists", "skincare-specialists", order++, now),
                        CreateSubcategory("Massage Therapists", "massage-therapists", order++, now),
                        CreateSubcategory("Mobile Beauty Services", "mobile-beauty-services", order++, now),
                        CreateSubcategory("Bridal Beauty Services", "bridal-beauty-services", order++, now),
                        CreateSubcategory("Men's Grooming", "mens-grooming", order++, now),
                        CreateSubcategory("Wig Installation", "wig-installation", order++, now),
                        CreateSubcategory("Wig Sales", "wig-sales", order++, now),
                        CreateSubcategory("Hair Product Sellers", "hair-product-sellers", order++, now),
                        CreateSubcategory("Beauty Product Sellers", "beauty-product-sellers", order++, now),
                        CreateSubcategory("Tattoo Artists", "tattoo-artists", order++, now),
                        CreateSubcategory("Piercing Services", "piercing-services", order++, now)
                    }
                },
                new SellerCategory
                {
                    Id = Guid.NewGuid(),
                    Name = "Food & Dining",
                    Slug = "food-dining",
                    Description = "Restaurants, catering, meal prep, and food vendors",
                    SortOrder = order++,
                    IsActive = true,
                    CreatedAtUtc = now,
                    Subcategories = new List<SellerSubcategory>
                    {
                        CreateSubcategory("Restaurants", "restaurants", order++, now),
                        CreateSubcategory("Fast Food", "fast-food", order++, now),
                        CreateSubcategory("Takeaways", "takeaways", order++, now),
                        CreateSubcategory("Shisanyama", "shisanyama", order++, now),
                        CreateSubcategory("Catering", "catering", order++, now),
                        CreateSubcategory("Home Cooks", "home-cooks", order++, now),
                        CreateSubcategory("Meal Prep Services", "meal-prep-services", order++, now),
                        CreateSubcategory("Private Chefs", "private-chefs", order++, now),
                        CreateSubcategory("Bakers", "bakers", order++, now),
                        CreateSubcategory("Cake Makers", "cake-makers", order++, now),
                        CreateSubcategory("Dessert Sellers", "dessert-sellers", order++, now),
                        CreateSubcategory("Street Food Vendors", "street-food-vendors", order++, now),
                        CreateSubcategory("Snack Sellers", "snack-sellers", order++, now),
                        CreateSubcategory("Fruit & Veg Sellers", "fruit-veg-sellers", order++, now),
                        CreateSubcategory("Grocery Stores", "grocery-stores", order++, now),
                        CreateSubcategory("Spaza Shops", "spaza-shops", order++, now),
                        CreateSubcategory("Butcheries", "butcheries", order++, now),
                        CreateSubcategory("Coffee Vendors", "coffee-vendors", order++, now),
                        CreateSubcategory("Beverage Sellers", "beverage-sellers", order++, now)
                    }
                },
                new SellerCategory
                {
                    Id = Guid.NewGuid(),
                    Name = "Fashion & Apparel",
                    Slug = "fashion-apparel",
                    Description = "Clothing, shoes, accessories, and tailoring",
                    SortOrder = order++,
                    IsActive = true,
                    CreatedAtUtc = now,
                    Subcategories = new List<SellerSubcategory>
                    {
                        CreateSubcategory("Clothing", "clothing", order++, now),
                        CreateSubcategory("Fashion Boutiques", "fashion-boutiques", order++, now),
                        CreateSubcategory("Streetwear Sellers", "streetwear-sellers", order++, now),
                        CreateSubcategory("Shoe Sellers", "shoe-sellers", order++, now),
                        CreateSubcategory("Sneaker Resellers", "sneaker-resellers", order++, now),
                        CreateSubcategory("Tailoring", "tailoring", order++, now),
                        CreateSubcategory("Dressmakers", "dressmakers", order++, now),
                        CreateSubcategory("Alterations", "alterations", order++, now),
                        CreateSubcategory("Traditional Wear", "traditional-wear", order++, now),
                        CreateSubcategory("Kids Clothing", "kids-clothing", order++, now),
                        CreateSubcategory("Women's Fashion", "womens-fashion", order++, now),
                        CreateSubcategory("Men's Fashion", "mens-fashion", order++, now),
                        CreateSubcategory("Uniform Suppliers", "uniform-suppliers", order++, now),
                        CreateSubcategory("Bags & Handbags", "bags-handbags", order++, now),
                        CreateSubcategory("Accessories", "accessories", order++, now),
                        CreateSubcategory("Jewellery", "jewellery", order++, now),
                        CreateSubcategory("Watch Sellers", "watch-sellers", order++, now),
                        CreateSubcategory("Fabric Sellers", "fabric-sellers", order++, now),
                        CreateSubcategory("Thrift / Pre-Owned Clothing", "thrift-clothing", order++, now)
                    }
                },
                new SellerCategory
                {
                    Id = Guid.NewGuid(),
                    Name = "Home Services",
                    Slug = "home-services",
                    Description = "Cleaning, gardening, handyman, and home maintenance",
                    SortOrder = order++,
                    IsActive = true,
                    CreatedAtUtc = now,
                    Subcategories = new List<SellerSubcategory>
                    {
                        CreateSubcategory("Cleaners", "cleaners", order++, now),
                        CreateSubcategory("Deep Cleaning", "deep-cleaning", order++, now),
                        CreateSubcategory("Laundry Services", "laundry-services", order++, now),
                        CreateSubcategory("Ironing Services", "ironing-services", order++, now),
                        CreateSubcategory("Home Organising", "home-organising", order++, now),
                        CreateSubcategory("Garden Services", "garden-services", order++, now),
                        CreateSubcategory("Landscaping", "landscaping", order++, now),
                        CreateSubcategory("Pest Control", "pest-control", order++, now),
                        CreateSubcategory("Pool Cleaning", "pool-cleaning", order++, now),
                        CreateSubcategory("Carpet Cleaning", "carpet-cleaning", order++, now),
                        CreateSubcategory("Window Cleaning", "window-cleaning", order++, now),
                        CreateSubcategory("Upholstery Cleaning", "upholstery-cleaning", order++, now),
                        CreateSubcategory("Waste Removal", "waste-removal", order++, now),
                        CreateSubcategory("Moving Help", "moving-help", order++, now),
                        CreateSubcategory("Handyman Services", "handyman-services", order++, now),
                        CreateSubcategory("Home Cooking Services", "home-cooking-services", order++, now),
                        CreateSubcategory("Elderly Assistance", "elderly-assistance", order++, now),
                        CreateSubcategory("Home Care Services", "home-care-services", order++, now)
                    }
                },
                new SellerCategory
                {
                    Id = Guid.NewGuid(),
                    Name = "Repairs & Trades",
                    Slug = "repairs-trades",
                    Description = "Electricians, plumbers, builders, and repair services",
                    SortOrder = order++,
                    IsActive = true,
                    CreatedAtUtc = now,
                    Subcategories = new List<SellerSubcategory>
                    {
                        CreateSubcategory("Repair Services", "repair-services", order++, now),
                        CreateSubcategory("Electricians", "electricians", order++, now),
                        CreateSubcategory("Plumbers", "plumbers", order++, now),
                        CreateSubcategory("Builders", "builders", order++, now),
                        CreateSubcategory("Painters", "painters", order++, now),
                        CreateSubcategory("Tilers", "tilers", order++, now),
                        CreateSubcategory("Carpenters", "carpenters", order++, now),
                        CreateSubcategory("Welders", "welders", order++, now),
                        CreateSubcategory("Roofing Services", "roofing-services", order++, now),
                        CreateSubcategory("Ceiling Installers", "ceiling-installers", order++, now),
                        CreateSubcategory("Flooring Installers", "flooring-installers", order++, now),
                        CreateSubcategory("Appliance Repair", "appliance-repair", order++, now),
                        CreateSubcategory("TV Repair", "tv-repair", order++, now),
                        CreateSubcategory("Phone Repair", "phone-repair", order++, now),
                        CreateSubcategory("Laptop Repair", "laptop-repair", order++, now),
                        CreateSubcategory("Fridge Repair", "fridge-repair", order++, now),
                        CreateSubcategory("Aircon Services", "aircon-services", order++, now),
                        CreateSubcategory("Generator Repair", "generator-repair", order++, now),
                        CreateSubcategory("Borehole Services", "borehole-services", order++, now),
                        CreateSubcategory("Gate Motor Repair", "gate-motor-repair", order++, now),
                        CreateSubcategory("Locksmiths", "locksmiths", order++, now)
                    }
                },
                new SellerCategory
                {
                    Id = Guid.NewGuid(),
                    Name = "Electronics & Tech",
                    Slug = "electronics-tech",
                    Description = "Phones, laptops, gadgets, and tech services",
                    SortOrder = order++,
                    IsActive = true,
                    CreatedAtUtc = now,
                    Subcategories = new List<SellerSubcategory>
                    {
                        CreateSubcategory("Electronics", "electronics", order++, now),
                        CreateSubcategory("Phone Sellers", "phone-sellers", order++, now),
                        CreateSubcategory("Laptop Sellers", "laptop-sellers", order++, now),
                        CreateSubcategory("Computer Accessories", "computer-accessories", order++, now),
                        CreateSubcategory("Gaming Consoles", "gaming-consoles", order++, now),
                        CreateSubcategory("TV & Audio", "tv-audio", order++, now),
                        CreateSubcategory("Camera Equipment", "camera-equipment", order++, now),
                        CreateSubcategory("Smart Devices", "smart-devices", order++, now),
                        CreateSubcategory("Phone Accessories", "phone-accessories", order++, now),
                        CreateSubcategory("Tech Repairs", "tech-repairs", order++, now),
                        CreateSubcategory("Software Setup", "software-setup", order++, now),
                        CreateSubcategory("IT Support", "it-support", order++, now),
                        CreateSubcategory("WiFi Installation", "wifi-installation", order++, now),
                        CreateSubcategory("CCTV Installation", "cctv-installation", order++, now),
                        CreateSubcategory("Solar Installation", "solar-installation", order++, now),
                        CreateSubcategory("Data & Airtime Sellers", "data-airtime-sellers", order++, now)
                    }
                },
                new SellerCategory
                {
                    Id = Guid.NewGuid(),
                    Name = "Health & Wellness",
                    Slug = "health-wellness",
                    Description = "Fitness, nutrition, therapy, and wellness services",
                    SortOrder = order++,
                    IsActive = true,
                    CreatedAtUtc = now,
                    Subcategories = new List<SellerSubcategory>
                    {
                        CreateSubcategory("Health & Wellness", "health-wellness", order++, now),
                        CreateSubcategory("Personal Trainers", "personal-trainers", order++, now),
                        CreateSubcategory("Fitness Coaches", "fitness-coaches", order++, now),
                        CreateSubcategory("Gym Instructors", "gym-instructors", order++, now),
                        CreateSubcategory("Dieticians", "dieticians", order++, now),
                        CreateSubcategory("Nutrition Coaches", "nutrition-coaches", order++, now),
                        CreateSubcategory("Counsellors", "counsellors", order++, now),
                        CreateSubcategory("Therapists", "therapists", order++, now),
                        CreateSubcategory("Wellness Coaches", "wellness-coaches", order++, now),
                        CreateSubcategory("Yoga Instructors", "yoga-instructors", order++, now),
                        CreateSubcategory("Pilates Instructors", "pilates-instructors", order++, now),
                        CreateSubcategory("Mobile Clinics", "mobile-clinics", order++, now),
                        CreateSubcategory("Home Nursing", "home-nursing", order++, now),
                        CreateSubcategory("Caregivers", "caregivers", order++, now),
                        CreateSubcategory("Wellness Product Sellers", "wellness-product-sellers", order++, now),
                        CreateSubcategory("Supplement Retailers", "supplement-retailers", order++, now),
                        CreateSubcategory("Physiotherapy Services", "physiotherapy-services", order++, now)
                    }
                },
                new SellerCategory
                {
                    Id = Guid.NewGuid(),
                    Name = "Education & Tutoring",
                    Slug = "education-tutoring",
                    Description = "Tutoring, coaching, lessons, and skills training",
                    SortOrder = order++,
                    IsActive = true,
                    CreatedAtUtc = now,
                    Subcategories = new List<SellerSubcategory>
                    {
                        CreateSubcategory("Education & Tutoring", "education-tutoring", order++, now),
                        CreateSubcategory("Academic Tutors", "academic-tutors", order++, now),
                        CreateSubcategory("Homework Assistance", "homework-assistance", order++, now),
                        CreateSubcategory("Exam Prep Tutors", "exam-prep-tutors", order++, now),
                        CreateSubcategory("University Tutors", "university-tutors", order++, now),
                        CreateSubcategory("Language Tutors", "language-tutors", order++, now),
                        CreateSubcategory("Coding Tutors", "coding-tutors", order++, now),
                        CreateSubcategory("Computer Lessons", "computer-lessons", order++, now),
                        CreateSubcategory("Driving Schools", "driving-schools", order++, now),
                        CreateSubcategory("Music Lessons", "music-lessons", order++, now),
                        CreateSubcategory("Art Lessons", "art-lessons", order++, now),
                        CreateSubcategory("Sports Coaching", "sports-coaching", order++, now),
                        CreateSubcategory("Skills Training", "skills-training", order++, now),
                        CreateSubcategory("Business Coaching", "business-coaching", order++, now),
                        CreateSubcategory("Early Childhood Learning", "early-childhood-learning", order++, now),
                        CreateSubcategory("Special Needs Support", "special-needs-support", order++, now)
                    }
                },
                new SellerCategory
                {
                    Id = Guid.NewGuid(),
                    Name = "Other",
                    Slug = "other",
                    Description = "Miscellaneous categories that don't fit elsewhere",
                    SortOrder = 999,
                    IsActive = true,
                    CreatedAtUtc = now,
                    Subcategories = new List<SellerSubcategory>
                    {
                        CreateSubcategory("Other", "other", 0, now)
                    }
                }
            };
        }

        private static SellerSubcategory CreateSubcategory(string name, string slug, int sortOrder, DateTime createdAt)
        {
            return new SellerSubcategory
            {
                Id = Guid.NewGuid(),
                Name = name,
                Slug = slug,
                Description = null,
                SortOrder = sortOrder,
                IsActive = true,
                CreatedAtUtc = createdAt
            };
        }
    }
}
'@

# =========================
# API Controller
# =========================

Write-Utf8File (Join-Path $apiControllersPath "SellerCategoriesController.cs") @'
using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ZansiHustle.Application.SellerCategories;
using ZansiHustle.Application.SellerCategories.Dtos;
using ZansiHustle.Shared.Results;

namespace ZansiHustle.API.Controllers
{
    /// <summary>
    /// Exposes endpoints for managing seller categories and subcategories.
    /// </summary>
    [ApiController]
    [Authorize]
    [Route("api/[controller]")]
    public class SellerCategoriesController : BaseController
    {
        private readonly ISellerCategoryService _sellerCategoryService;

        public SellerCategoriesController(ISellerCategoryService sellerCategoryService)
        {
            _sellerCategoryService = sellerCategoryService;
        }

        /// <summary>
        /// Gets all seller categories with their subcategories.
        /// </summary>
        [HttpGet]
        [AllowAnonymous]
        [ProducesResponseType(typeof(Result<List<SellerCategoryDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAll([FromQuery] bool activeOnly = false)
        {
            var result = await _sellerCategoryService.GetAllAsync(activeOnly);
            return ToActionResult(result);
        }

        /// <summary>
        /// Gets a seller category by its identifier.
        /// </summary>
        [HttpGet("{id:guid}")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(Result<SellerCategoryDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetById(Guid id)
        {
            var result = await _sellerCategoryService.GetByIdAsync(id);
            return ToActionResult(result);
        }

        /// <summary>
        /// Creates a new seller category.
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Admin,SuperAdmin,TeamManager")]
        [ProducesResponseType(typeof(Result<SellerCategoryDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> Create([FromBody] CreateSellerCategoryRequestDto request)
        {
            var result = await _sellerCategoryService.CreateAsync(request);
            return ToActionResult(result);
        }

        /// <summary>
        /// Updates an existing seller category.
        /// </summary>
        [HttpPut("{id:guid}")]
        [Authorize(Roles = "Admin,SuperAdmin,TeamManager")]
        [ProducesResponseType(typeof(Result<SellerCategoryDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateSellerCategoryRequestDto request)
        {
            var result = await _sellerCategoryService.UpdateAsync(id, request);
            return ToActionResult(result);
        }

        /// <summary>
        /// Deletes a seller category.
        /// </summary>
        [HttpDelete("{id:guid}")]
        [Authorize(Roles = "Admin,SuperAdmin,TeamManager")]
        [ProducesResponseType(typeof(Result), StatusCodes.Status200OK)]
        public async Task<IActionResult> Delete(Guid id)
        {
            var result = await _sellerCategoryService.DeleteAsync(id);
            return ToActionResult(result);
        }

        /// <summary>
        /// Creates a new subcategory under a seller category.
        /// </summary>
        [HttpPost("subcategories")]
        [Authorize(Roles = "Admin,SuperAdmin,TeamManager")]
        [ProducesResponseType(typeof(Result<SellerSubcategoryDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> CreateSubcategory([FromBody] CreateSellerSubcategoryRequestDto request)
        {
            var result = await _sellerCategoryService.CreateSubcategoryAsync(request);
            return ToActionResult(result);
        }

        /// <summary>
        /// Updates an existing subcategory.
        /// </summary>
        [HttpPut("subcategories/{id:guid}")]
        [Authorize(Roles = "Admin,SuperAdmin,TeamManager")]
        [ProducesResponseType(typeof(Result<SellerSubcategoryDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> UpdateSubcategory(Guid id, [FromBody] UpdateSellerSubcategoryRequestDto request)
        {
            var result = await _sellerCategoryService.UpdateSubcategoryAsync(id, request);
            return ToActionResult(result);
        }

        /// <summary>
        /// Deletes a subcategory.
        /// </summary>
        [HttpDelete("subcategories/{id:guid}")]
        [Authorize(Roles = "Admin,SuperAdmin,TeamManager")]
        [ProducesResponseType(typeof(Result), StatusCodes.Status200OK)]
        public async Task<IActionResult> DeleteSubcategory(Guid id)
        {
            var result = await _sellerCategoryService.DeleteSubcategoryAsync(id);
            return ToActionResult(result);
        }
    }
}
'@

# =========================
# Patch AppDbContext
# =========================

if ($PatchAppDbContext) {
    $dbContextPath = Join-Path $rootPath "ZansiHustle.Infrastructure\Data\AppDbContext.cs"
    
    if (Test-Path $dbContextPath) {
        $content = Get-Content $dbContextPath -Raw
        
        # Check if DbSets already exist
        if ($content -notmatch "DbSet<SellerCategory>") {
            $dbSetInsert = @'

        public DbSet<SellerCategory> SellerCategories { get; set; }
        public DbSet<SellerSubcategory> SellerSubcategories { get; set; }
'@
            
            $content = $content -replace '(public class AppDbContext.*?{.*?)(public DbSet)', "`$1$dbSetInsert`n`$2"
        }
        
        # Check if configurations are applied
        if ($content -notmatch "ApplyConfiguration\(new SellerCategoryConfiguration") {
            $configInsert = @'
            
            modelBuilder.ApplyConfiguration(new SellerCategoryConfiguration());
            modelBuilder.ApplyConfiguration(new SellerSubcategoryConfiguration());
'@
            
            $content = $content -replace '(protected override void OnModelCreating.*?{.*?)(base\.OnModelCreating)', "`$1$configInsert`n`n            `$2"
        }
        
        Write-Utf8File $dbContextPath $content
        Write-Host "Updated AppDbContext with SellerCategories and SellerSubcategories" -ForegroundColor Yellow
    }
}

# =========================
# Patch Service Extensions
# =========================

if ($PatchServiceExtensions) {
    $serviceExtensionsPath = Join-Path $rootPath "ZansiHustle.Infrastructure\ServiceExtensions.cs"
    
    if (Test-Path $serviceExtensionsPath) {
        $content = Get-Content $serviceExtensionsPath -Raw
        
        # Check if services are already registered
        if ($content -notmatch "ISellerCategoryRepository") {
            $serviceInsert = @'
            
            // Seller Categories
            services.AddScoped<ISellerCategoryRepository, SellerCategoryRepository>();
            services.AddScoped<ISellerCategoryService, SellerCategoryService>();
'@
            
            $content = $content -replace '(public static IServiceCollection AddInfrastructure.*?{.*?)(return services;)', "`$1$serviceInsert`n`n            `$2"
        }
        
        Write-Utf8File $serviceExtensionsPath $content
        Write-Host "Updated ServiceExtensions with Seller Categories services" -ForegroundColor Yellow
    }
}

Write-Host "`n========================================" -ForegroundColor Cyan
Write-Host "Seller Categories setup complete!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Yellow
Write-Host "1. Run: dotnet ef migrations add AddSellerCategories" -ForegroundColor White
Write-Host "2. Run: dotnet ef database update" -ForegroundColor White
Write-Host "3. The seed data will run automatically on app startup" -ForegroundColor White
Write-Host ""