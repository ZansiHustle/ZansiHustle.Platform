using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ZansiHustle.Application.Merchants.Dtos;
using ZansiHustle.Application.Persistence.Merchants;
using ZansiHustle.Application.Persistence.SellerCategories;
using ZansiHustle.Domain.Merchants;
using ZansiHustle.Shared.Enums.Merchants;
using ZansiHustle.Shared.Errors;
using ZansiHustle.Shared.Results;

namespace ZansiHustle.Application.Merchants
{
    /// <summary>
    /// Provides business logic for merchant operations.
    /// </summary>
    public class MerchantService : IMerchantService
    {
        private readonly IMerchantRepository _merchantRepository;
        private readonly ISellerCategoryRepository _sellerCategoryRepository;

        public MerchantService(IMerchantRepository merchantRepository, ISellerCategoryRepository sellerCategoryRepository)
        {
            _merchantRepository = merchantRepository;
            _sellerCategoryRepository = sellerCategoryRepository;
        }

        /// <inheritdoc />
        public async Task<Result<List<MerchantDto>>> GetAllAsync()
        {
            try
            {
                var merchants = await _merchantRepository.GetAllAsync();
                var data = merchants.Select(MapToDto).ToList();

                return Result<List<MerchantDto>>.Success(data, "Merchants retrieved successfully.");
            }
            catch (Exception ex)
            {
                return Result<List<MerchantDto>>.Failure(ErrorCodes.Exception, $"An error occurred while retrieving merchants. {ex.Message}");
            }
        }

        /// <inheritdoc />
        public async Task<Result<MerchantDto>> GetByIdAsync(Guid id)
        {
            try
            {
                var merchant = await _merchantRepository.GetByIdAsync(id);

                if (merchant is null)
                    return Result<MerchantDto>.Failure(ErrorCodes.NotFound, "Merchant not found.");

                return Result<MerchantDto>.Success(MapToDto(merchant), "Merchant retrieved successfully.");
            }
            catch (Exception ex)
            {
                return Result<MerchantDto>.Failure(ErrorCodes.Exception, $"An error occurred while retrieving the merchant. {ex.Message}");
            }
        }

        /// <inheritdoc />
        public async Task<Result<MerchantDto>> CreateAsync(CreateMerchantRequestDto request)
        {
            try
            {
                if (request is null)
                    return Result<MerchantDto>.Failure(ErrorCodes.BadRequest, "Request is required.");

                if (string.IsNullOrWhiteSpace(request.Name))
                    return Result<MerchantDto>.Failure(ErrorCodes.BadRequest, "Merchant name is required.");

                var categoryCheck = await ValidateCategoriesAsync(request.SellerCategoryId, request.SellerSubcategoryId);
                if (!categoryCheck.IsSuccess)
                    return Result<MerchantDto>.Failure(categoryCheck.Code, categoryCheck.Message);

                var entity = new Merchant
                {
                    Id = Guid.NewGuid(),
                    Code = GenerateCode(),
                    Slug = await GenerateUniqueSlugAsync(request.Name),
                    Name = request.Name.Trim(),
                    Description = request.Description?.Trim(),
                    Type = request.Type,
                    OwnerUserId = request.OwnerUserId,
                    SellerCategoryId = request.SellerCategoryId,
                    SellerSubcategoryId = request.SellerSubcategoryId,
                    ContactEmail = request.ContactEmail?.Trim(),
                    ContactPhoneNumber = request.ContactPhoneNumber?.Trim(),
                    Province = request.Province?.Trim(),
                    City = request.City?.Trim(),
                    AddressLine1 = request.AddressLine1?.Trim(),
                    WebsiteUrl = request.WebsiteUrl?.Trim(),
                    LogoUrl = request.LogoUrl?.Trim(),
                    BannerUrl = request.BannerUrl?.Trim(),
                    Rating = request.Rating,
                    Status = MerchantStatus.Pending,
                    KycStatus = MerchantKycStatus.Pending,
                    IsPayoutEligible = false,
                    FollowersCount = 0,
                    ReviewCount = 0,
                    TotalOrders = 0,
                    TotalRevenue = 0m,
                    CreatedAtUtc = DateTime.UtcNow
                };

                await _merchantRepository.AddAsync(entity);
                var saved = await _merchantRepository.SaveChangesAsync();

                if (!saved)
                    return Result<MerchantDto>.Failure(ErrorCodes.Exception, "Failed to create merchant.");

                var reloaded = await _merchantRepository.GetByIdAsync(entity.Id);
                return Result<MerchantDto>.Success(MapToDto(reloaded ?? entity), "Merchant created successfully.");
            }
            catch (Exception ex)
            {
                return Result<MerchantDto>.Failure(ErrorCodes.Exception, $"An error occurred while creating the merchant. {ex.Message}");
            }
        }

        /// <inheritdoc />
        public async Task<Result<MerchantDto>> UpdateAsync(Guid id, UpdateMerchantRequestDto request)
        {
            try
            {
                if (request is null)
                    return Result<MerchantDto>.Failure(ErrorCodes.BadRequest, "Request is required.");

                if (string.IsNullOrWhiteSpace(request.Name))
                    return Result<MerchantDto>.Failure(ErrorCodes.BadRequest, "Merchant name is required.");

                var merchant = await _merchantRepository.GetByIdAsync(id);

                if (merchant is null)
                    return Result<MerchantDto>.Failure(ErrorCodes.NotFound, "Merchant not found.");

                var categoryCheck = await ValidateCategoriesAsync(request.SellerCategoryId, request.SellerSubcategoryId);
                if (!categoryCheck.IsSuccess)
                    return Result<MerchantDto>.Failure(categoryCheck.Code, categoryCheck.Message);

                ApplyNameAndSlugAsync(merchant, request.Name);
                merchant.Description = request.Description?.Trim();
                merchant.Type = request.Type;
                merchant.Status = request.Status;
                merchant.SellerCategoryId = request.SellerCategoryId;
                merchant.SellerSubcategoryId = request.SellerSubcategoryId;
                merchant.ContactEmail = request.ContactEmail?.Trim();
                merchant.ContactPhoneNumber = request.ContactPhoneNumber?.Trim();
                merchant.Province = request.Province?.Trim();
                merchant.City = request.City?.Trim();
                merchant.AddressLine1 = request.AddressLine1?.Trim();
                merchant.WebsiteUrl = request.WebsiteUrl?.Trim();
                merchant.LogoUrl = request.LogoUrl?.Trim();
                merchant.BannerUrl = request.BannerUrl?.Trim();
                merchant.Rating = request.Rating;
                merchant.UpdatedAtUtc = DateTime.UtcNow;

                _merchantRepository.Update(merchant);
                var saved = await _merchantRepository.SaveChangesAsync();

                if (!saved)
                    return Result<MerchantDto>.Failure(ErrorCodes.Exception, "Failed to update merchant.");

                var reloaded = await _merchantRepository.GetByIdAsync(merchant.Id);
                return Result<MerchantDto>.Success(MapToDto(reloaded ?? merchant), "Merchant updated successfully.");
            }
            catch (Exception ex)
            {
                return Result<MerchantDto>.Failure(ErrorCodes.Exception, $"An error occurred while updating the merchant. {ex.Message}");
            }
        }

        /// <inheritdoc />
        public async Task<Result<MerchantDto>> VerifyKycAsync(Guid id)
        {
            try
            {
                var merchant = await _merchantRepository.GetByIdAsync(id);

                if (merchant is null)
                    return Result<MerchantDto>.Failure(ErrorCodes.NotFound, "Merchant not found.");

                merchant.KycStatus = MerchantKycStatus.Verified;
                merchant.Status = MerchantStatus.Active;
                merchant.UpdatedAtUtc = DateTime.UtcNow;

                _merchantRepository.Update(merchant);
                var saved = await _merchantRepository.SaveChangesAsync();

                if (!saved)
                    return Result<MerchantDto>.Failure(ErrorCodes.Exception, "Failed to verify merchant KYC.");

                return Result<MerchantDto>.Success(MapToDto(merchant), "Merchant KYC verified successfully.");
            }
            catch (Exception ex)
            {
                return Result<MerchantDto>.Failure(ErrorCodes.Exception, $"An error occurred while verifying merchant KYC. {ex.Message}");
            }
        }

        /// <inheritdoc />
        public async Task<Result<MerchantDto>> UpdatePayoutEligibilityAsync(Guid id, bool eligible)
        {
            try
            {
                var merchant = await _merchantRepository.GetByIdAsync(id);

                if (merchant is null)
                    return Result<MerchantDto>.Failure(ErrorCodes.NotFound, "Merchant not found.");

                merchant.IsPayoutEligible = eligible;
                merchant.UpdatedAtUtc = DateTime.UtcNow;

                _merchantRepository.Update(merchant);
                var saved = await _merchantRepository.SaveChangesAsync();

                if (!saved)
                    return Result<MerchantDto>.Failure(ErrorCodes.Exception, "Failed to update merchant payout eligibility.");

                return Result<MerchantDto>.Success(MapToDto(merchant), "Merchant payout eligibility updated successfully.");
            }
            catch (Exception ex)
            {
                return Result<MerchantDto>.Failure(ErrorCodes.Exception, $"An error occurred while updating merchant payout eligibility. {ex.Message}");
            }
        }

        /// <inheritdoc />
        public async Task<Result> DeleteAsync(Guid id)
        {
            try
            {
                var merchant = await _merchantRepository.GetByIdAsync(id);

                if (merchant is null)
                    return Result.Failure(ErrorCodes.NotFound, "Merchant not found.");

                _merchantRepository.Delete(merchant);
                var saved = await _merchantRepository.SaveChangesAsync();

                if (!saved)
                    return Result.Failure(ErrorCodes.Exception, "Failed to delete merchant.");

                return Result.Success("Merchant deleted successfully.");
            }
            catch (Exception ex)
            {
                return Result.Failure(ErrorCodes.Exception, $"An error occurred while deleting the merchant. {ex.Message}");
            }
        }

        /// <inheritdoc />
        public async Task<Result<List<MerchantDto>>> GetMineAsync(Guid ownerUserId)
        {
            try
            {
                var merchants = await _merchantRepository.GetByOwnerAsync(ownerUserId);
                var data = merchants.Select(MapToDto).ToList();

                return Result<List<MerchantDto>>.Success(data, "Shops retrieved successfully.");
            }
            catch (Exception ex)
            {
                return Result<List<MerchantDto>>.Failure(ErrorCodes.Exception, $"An error occurred while retrieving your shops. {ex.Message}");
            }
        }

        /// <inheritdoc />
        public async Task<Result<MerchantDto>> CreateMineAsync(Guid ownerUserId, CreateMyMerchantRequestDto request)
        {
            try
            {
                if (request is null)
                    return Result<MerchantDto>.Failure(ErrorCodes.BadRequest, "Request is required.");

                if (string.IsNullOrWhiteSpace(request.Name))
                    return Result<MerchantDto>.Failure(ErrorCodes.BadRequest, "Shop name is required.");

                var categoryCheck = await ValidateCategoriesAsync(request.SellerCategoryId, request.SellerSubcategoryId);
                if (!categoryCheck.IsSuccess)
                    return Result<MerchantDto>.Failure(categoryCheck.Code, categoryCheck.Message);

                var entity = new Merchant
                {
                    Id = Guid.NewGuid(),
                    Code = GenerateCode(),
                    Slug = await GenerateUniqueSlugAsync(request.Name),
                    Name = request.Name.Trim(),
                    Description = request.Description?.Trim(),
                    Type = request.Type,
                    OwnerUserId = ownerUserId,
                    SellerCategoryId = request.SellerCategoryId,
                    SellerSubcategoryId = request.SellerSubcategoryId,
                    ContactEmail = request.ContactEmail?.Trim(),
                    ContactPhoneNumber = request.ContactPhoneNumber?.Trim(),
                    Province = request.Province?.Trim(),
                    City = request.City?.Trim(),
                    AddressLine1 = request.AddressLine1?.Trim(),
                    WebsiteUrl = request.WebsiteUrl?.Trim(),
                    LogoUrl = request.LogoUrl?.Trim(),
                    BannerUrl = request.BannerUrl?.Trim(),
                    Status = MerchantStatus.Pending,
                    KycStatus = MerchantKycStatus.Pending,
                    IsPayoutEligible = false,
                    FollowersCount = 0,
                    ReviewCount = 0,
                    TotalOrders = 0,
                    TotalRevenue = 0m,
                    CreatedAtUtc = DateTime.UtcNow
                };

                await _merchantRepository.AddAsync(entity);
                var saved = await _merchantRepository.SaveChangesAsync();

                if (!saved)
                    return Result<MerchantDto>.Failure(ErrorCodes.Exception, "Failed to create shop.");

                var reloaded = await _merchantRepository.GetByIdAsync(entity.Id);
                return Result<MerchantDto>.Success(MapToDto(reloaded ?? entity), "Shop created successfully.");
            }
            catch (Exception ex)
            {
                return Result<MerchantDto>.Failure(ErrorCodes.Exception, $"An error occurred while creating your shop. {ex.Message}");
            }
        }

        /// <inheritdoc />
        public async Task<Result<MerchantDto>> UpdateMineAsync(Guid ownerUserId, Guid merchantId, UpdateMyMerchantRequestDto request)
        {
            try
            {
                if (request is null)
                    return Result<MerchantDto>.Failure(ErrorCodes.BadRequest, "Request is required.");

                if (string.IsNullOrWhiteSpace(request.Name))
                    return Result<MerchantDto>.Failure(ErrorCodes.BadRequest, "Shop name is required.");

                var merchant = await _merchantRepository.GetByIdAsync(merchantId);

                if (merchant is null)
                    return Result<MerchantDto>.Failure(ErrorCodes.NotFound, "Shop not found.");

                if (merchant.OwnerUserId != ownerUserId)
                    return Result<MerchantDto>.Failure(ErrorCodes.Forbidden, "You do not have permission to update this shop.");

                var categoryCheck = await ValidateCategoriesAsync(request.SellerCategoryId, request.SellerSubcategoryId);
                if (!categoryCheck.IsSuccess)
                    return Result<MerchantDto>.Failure(categoryCheck.Code, categoryCheck.Message);

                ApplyNameAndSlugAsync(merchant, request.Name);
                merchant.Description = request.Description?.Trim();
                merchant.Type = request.Type;
                merchant.SellerCategoryId = request.SellerCategoryId;
                merchant.SellerSubcategoryId = request.SellerSubcategoryId;
                merchant.ContactEmail = request.ContactEmail?.Trim();
                merchant.ContactPhoneNumber = request.ContactPhoneNumber?.Trim();
                merchant.Province = request.Province?.Trim();
                merchant.City = request.City?.Trim();
                merchant.AddressLine1 = request.AddressLine1?.Trim();
                merchant.WebsiteUrl = request.WebsiteUrl?.Trim();
                merchant.LogoUrl = request.LogoUrl?.Trim();
                merchant.BannerUrl = request.BannerUrl?.Trim();
                merchant.UpdatedAtUtc = DateTime.UtcNow;

                _merchantRepository.Update(merchant);
                var saved = await _merchantRepository.SaveChangesAsync();

                if (!saved)
                    return Result<MerchantDto>.Failure(ErrorCodes.Exception, "Failed to update shop.");

                var reloaded = await _merchantRepository.GetByIdAsync(merchant.Id);
                return Result<MerchantDto>.Success(MapToDto(reloaded ?? merchant), "Shop updated successfully.");
            }
            catch (Exception ex)
            {
                return Result<MerchantDto>.Failure(ErrorCodes.Exception, $"An error occurred while updating your shop. {ex.Message}");
            }
        }

        /// <inheritdoc />
        public async Task<Result> DeleteMineAsync(Guid ownerUserId, Guid merchantId)
        {
            try
            {
                var merchant = await _merchantRepository.GetByIdAsync(merchantId);

                if (merchant is null)
                    return Result.Failure(ErrorCodes.NotFound, "Shop not found.");

                if (merchant.OwnerUserId != ownerUserId)
                    return Result.Failure(ErrorCodes.Forbidden, "You do not have permission to delete this shop.");

                _merchantRepository.Delete(merchant);
                var saved = await _merchantRepository.SaveChangesAsync();

                if (!saved)
                    return Result.Failure(ErrorCodes.Exception, "Failed to delete shop.");

                return Result.Success("Shop deleted successfully.");
            }
            catch (Exception ex)
            {
                return Result.Failure(ErrorCodes.Exception, $"An error occurred while deleting your shop. {ex.Message}");
            }
        }

        private async Task<Result> ValidateCategoriesAsync(Guid? categoryId, Guid? subcategoryId)
        {
            if (categoryId.HasValue)
            {
                var cat = await _sellerCategoryRepository.GetByIdAsync(categoryId.Value);

                if (cat is null)
                    return Result.Failure(ErrorCodes.BadRequest, "The selected category does not exist.");
            }

            if (subcategoryId.HasValue)
            {
                var sub = await _sellerCategoryRepository.GetSubcategoryByIdAsync(subcategoryId.Value);

                if (sub is null)
                    return Result.Failure(ErrorCodes.BadRequest, "The selected subcategory does not exist.");

                if (categoryId.HasValue && sub.SellerCategoryId != categoryId.Value)
                    return Result.Failure(ErrorCodes.BadRequest, "The selected subcategory does not belong to the selected category.");
            }

            return Result.Success();
        }

        private void ApplyNameAndSlugAsync(Merchant merchant, string newName)
        {
            var trimmed = newName.Trim();
            if (string.Equals(merchant.Name, trimmed, StringComparison.Ordinal))
                return;

            merchant.Name = trimmed;
            // Slug is immutable after creation to keep external references (URLs,
            // listing back-links) stable. Callers can expose a rename-slug endpoint
            // later if needed.
        }

        private async Task<string> GenerateUniqueSlugAsync(string name)
        {
            var baseSlug = Slugify(name);
            if (string.IsNullOrEmpty(baseSlug))
                baseSlug = "shop";

            var candidate = baseSlug;
            var suffix = 2;
            while (await _merchantRepository.ExistsBySlugAsync(candidate))
            {
                candidate = $"{baseSlug}-{suffix}";
                suffix++;

                if (suffix > 9999)
                {
                    // Defensive fallback — append random token to escape the loop.
                    candidate = $"{baseSlug}-{Guid.NewGuid().ToString("N")[..6]}";
                    break;
                }
            }

            return candidate;
        }

        private static string Slugify(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;

            var lower = value.Trim().ToLowerInvariant();
            // Strip accents/diacritics.
            var normalized = lower.Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder(normalized.Length);
            foreach (var ch in normalized)
            {
                var cat = System.Globalization.CharUnicodeInfo.GetUnicodeCategory(ch);

                if (cat != System.Globalization.UnicodeCategory.NonSpacingMark)
                    sb.Append(ch);
            }

            var cleaned = sb.ToString().Normalize(NormalizationForm.FormC);
            cleaned = Regex.Replace(cleaned, @"[^a-z0-9\s-]", " ");
            cleaned = Regex.Replace(cleaned, @"[\s-]+", "-").Trim('-');

            if (cleaned.Length > 80)
                cleaned = cleaned[..80].Trim('-');

            return cleaned;
        }

        private static string GenerateCode()
        {
            return $"MER-{DateTime.UtcNow:yyyyMMddHHmmssfff}";
        }

        private static MerchantDto MapToDto(Merchant merchant)
        {
            return new MerchantDto
            {
                Id = merchant.Id,
                Code = merchant.Code,
                Slug = merchant.Slug,
                Name = merchant.Name,
                Description = merchant.Description,
                Type = merchant.Type,
                Status = merchant.Status,
                KycStatus = merchant.KycStatus,
                IsPayoutEligible = merchant.IsPayoutEligible,
                OwnerUserId = merchant.OwnerUserId,
                SellerCategoryId = merchant.SellerCategoryId,
                SellerCategoryName = merchant.SellerCategory?.Name,
                SellerSubcategoryId = merchant.SellerSubcategoryId,
                SellerSubcategoryName = merchant.SellerSubcategory?.Name,
                ContactEmail = merchant.ContactEmail,
                ContactPhoneNumber = merchant.ContactPhoneNumber,
                Province = merchant.Province,
                City = merchant.City,
                AddressLine1 = merchant.AddressLine1,
                WebsiteUrl = merchant.WebsiteUrl,
                LogoUrl = merchant.LogoUrl,
                BannerUrl = merchant.BannerUrl,
                FollowersCount = merchant.FollowersCount,
                Rating = merchant.Rating,
                ReviewCount = merchant.ReviewCount,
                TotalOrders = merchant.TotalOrders,
                TotalRevenue = merchant.TotalRevenue,
                CreatedAtUtc = merchant.CreatedAtUtc,
                UpdatedAtUtc = merchant.UpdatedAtUtc
            };
        }
    }
}
