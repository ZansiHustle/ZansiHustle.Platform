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
        private readonly Application.Media.IMediaService _mediaService;
        private readonly Application.Persistence.Media.IMediaAssetRepository _mediaRepo;

        public MerchantService(
            IMerchantRepository merchantRepository,
            ISellerCategoryRepository sellerCategoryRepository,
            Application.Media.IMediaService mediaService,
            Application.Persistence.Media.IMediaAssetRepository mediaRepo)
        {
            _merchantRepository = merchantRepository;
            _sellerCategoryRepository = sellerCategoryRepository;
            _mediaService = mediaService;
            _mediaRepo = mediaRepo;
        }

        // Verification uploads required for self-registration. Used both at
        // CreateMineAsync time (to fail-fast if a required upload is missing)
        // and at VerifyKycAsync time (each must be Approved).
        private static readonly Shared.Enums.Media.MediaPurpose[] RequiredVerificationPurposes =
        {
            Shared.Enums.Media.MediaPurpose.IdDocument,
            Shared.Enums.Media.MediaPurpose.Portrait,
            Shared.Enums.Media.MediaPurpose.VerificationProductSample,
        };

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
                merchant.WhatsAppNumber = request.WhatsAppNumber?.Trim();
                merchant.SocialHandle = request.SocialHandle?.Trim();
                merchant.IdNumber = request.IdNumber?.Trim();
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

                // KYC verify requires every required verification upload to
                // be Approved. Admins approve uploads individually via the
                // /api/media/{id}/review endpoint; this gate ensures KYC
                // status flips ONLY when the document review is complete.
                var media = await _mediaRepo.GetByOwnerAsync(
                    Shared.Enums.Media.OwnerEntityType.Merchant, id);
                foreach (var purpose in RequiredVerificationPurposes)
                {
                    var approved = media.Any(m =>
                        m.Purpose == purpose &&
                        m.Status == Shared.Enums.Media.MediaStatus.Approved);
                    if (!approved)
                    {
                        return Result<MerchantDto>.Failure(ErrorCodes.BadRequest,
                            $"Cannot verify KYC: required {purpose} upload is not yet approved.");
                    }
                }

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
        public async Task<Result<MerchantDto>> ApproveAsync(Guid id)
        {
            try
            {
                var merchant = await _merchantRepository.GetByIdAsync(id);

                if (merchant is null)
                    return Result<MerchantDto>.Failure(ErrorCodes.NotFound, "Merchant not found.");

                if (merchant.Status == MerchantStatus.Active)
                    return Result<MerchantDto>.Success(MapToDto(merchant), "Merchant already approved.");

                merchant.Status = MerchantStatus.Active;
                merchant.UpdatedAtUtc = DateTime.UtcNow;

                _merchantRepository.Update(merchant);
                var saved = await _merchantRepository.SaveChangesAsync();

                if (!saved)
                    return Result<MerchantDto>.Failure(ErrorCodes.Exception, "Failed to approve merchant.");

                return Result<MerchantDto>.Success(MapToDto(merchant), "Merchant approved.");
            }
            catch (Exception ex)
            {
                return Result<MerchantDto>.Failure(ErrorCodes.Exception, $"An error occurred while approving the merchant. {ex.Message}");
            }
        }

        /// <inheritdoc />
        public async Task<Result<MerchantDto>> RejectAsync(Guid id, string? reason = null)
        {
            try
            {
                var merchant = await _merchantRepository.GetByIdAsync(id);

                if (merchant is null)
                    return Result<MerchantDto>.Failure(ErrorCodes.NotFound, "Merchant not found.");

                merchant.Status = MerchantStatus.Suspended;
                merchant.UpdatedAtUtc = DateTime.UtcNow;

                _merchantRepository.Update(merchant);
                var saved = await _merchantRepository.SaveChangesAsync();

                if (!saved)
                    return Result<MerchantDto>.Failure(ErrorCodes.Exception, "Failed to reject merchant.");

                return Result<MerchantDto>.Success(MapToDto(merchant), string.IsNullOrWhiteSpace(reason) ? "Merchant rejected." : $"Merchant rejected: {reason}");
            }
            catch (Exception ex)
            {
                return Result<MerchantDto>.Failure(ErrorCodes.Exception, $"An error occurred while rejecting the merchant. {ex.Message}");
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

                // Seller-first onboarding: a merchant record represents the
                // seller account, not necessarily a published storefront. The
                // shop name is therefore optional at creation — the seller can
                // set it later when they actually open a storefront. Default
                // to a placeholder so existing Name/Slug invariants hold.
                var name = string.IsNullOrWhiteSpace(request.Name)
                    ? BuildDefaultMerchantName(request.ContactEmail, request.ContactPhoneNumber)
                    : request.Name.Trim();

                var categoryCheck = await ValidateCategoriesAsync(request.SellerCategoryId, request.SellerSubcategoryId);
                if (!categoryCheck.IsSuccess)
                    return Result<MerchantDto>.Failure(categoryCheck.Code, categoryCheck.Message);

                var entity = new Merchant
                {
                    Id = Guid.NewGuid(),
                    Code = GenerateCode(),
                    Slug = await GenerateUniqueSlugAsync(name),
                    Name = name,
                    Description = request.Description?.Trim(),
                    Type = request.Type,
                    OwnerUserId = ownerUserId,
                    SellerCategoryId = request.SellerCategoryId,
                    SellerSubcategoryId = request.SellerSubcategoryId,
                    ContactEmail = request.ContactEmail?.Trim(),
                    ContactPhoneNumber = request.ContactPhoneNumber?.Trim(),
                    WhatsAppNumber = request.WhatsAppNumber?.Trim(),
                    SocialHandle = request.SocialHandle?.Trim(),
                    IdNumber = request.IdNumber?.Trim(),
                    ReferralCode = string.IsNullOrWhiteSpace(request.ReferralCode) ? null : request.ReferralCode.Trim().ToUpperInvariant(),
                    Province = request.Province?.Trim(),
                    City = request.City?.Trim(),
                    AddressLine1 = request.AddressLine1?.Trim(),
                    Suburb = request.Suburb?.Trim(),
                    PostalCode = request.PostalCode?.Trim(),
                    Country = request.Country?.Trim(),
                    CountryCode = string.IsNullOrWhiteSpace(request.CountryCode) ? null : request.CountryCode.Trim().ToUpperInvariant(),
                    Latitude = request.Latitude,
                    Longitude = request.Longitude,
                    GooglePlaceId = request.GooglePlaceId?.Trim(),
                    FormattedAddress = request.FormattedAddress?.Trim(),
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

                // Validate required verification uploads BEFORE writing the
                // Merchant row. Each required purpose must be present among
                // the supplied media-asset ids; admin then approves them
                // separately to flip KycStatus.
                if (request.MediaAssetIds is { Count: > 0 })
                {
                    var assets = await _mediaRepo.GetByIdsAsync(request.MediaAssetIds);
                    foreach (var purpose in RequiredVerificationPurposes)
                    {
                        var hit = assets.Any(a =>
                            a.UploadedByUserId == ownerUserId &&
                            a.Purpose == purpose);
                        if (!hit)
                        {
                            return Result<MerchantDto>.Failure(ErrorCodes.BadRequest,
                                $"Verification upload missing: {purpose}.");
                        }
                    }
                }
                else
                {
                    return Result<MerchantDto>.Failure(ErrorCodes.BadRequest,
                        "Verification uploads are required (ID document, selfie, product sample).");
                }

                await _merchantRepository.AddAsync(entity);
                var saved = await _merchantRepository.SaveChangesAsync();

                if (!saved)
                    return Result<MerchantDto>.Failure(ErrorCodes.Exception, "Failed to create shop.");

                // Re-parent the orphan uploads onto the new Merchant id so
                // the admin Sellers drawer can fetch them by owner.
                await _mediaService.AttachToOwnerAsync(
                    request.MediaAssetIds,
                    Shared.Enums.Media.OwnerEntityType.Merchant,
                    entity.Id);

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
                merchant.WhatsAppNumber = request.WhatsAppNumber?.Trim();
                merchant.SocialHandle = request.SocialHandle?.Trim();
                merchant.IdNumber = request.IdNumber?.Trim();
                merchant.Province = request.Province?.Trim();
                merchant.City = request.City?.Trim();
                merchant.AddressLine1 = request.AddressLine1?.Trim();
                merchant.Suburb = request.Suburb?.Trim();
                merchant.PostalCode = request.PostalCode?.Trim();
                merchant.Country = request.Country?.Trim();
                merchant.CountryCode = string.IsNullOrWhiteSpace(request.CountryCode) ? null : request.CountryCode.Trim().ToUpperInvariant();
                merchant.Latitude = request.Latitude;
                merchant.Longitude = request.Longitude;
                merchant.GooglePlaceId = request.GooglePlaceId?.Trim();
                merchant.FormattedAddress = request.FormattedAddress?.Trim();
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

        // Used by the seller-first onboarding flow (CreateMineAsync) when the
        // applicant hasn't picked a shop name yet. Derives a friendly
        // placeholder from the contact email/phone so the seller record is
        // still distinguishable in admin listings until they rename it.
        private static string BuildDefaultMerchantName(string? email, string? phone)
        {
            if (!string.IsNullOrWhiteSpace(email))
            {
                var local = email.Trim().Split('@')[0];
                if (!string.IsNullOrWhiteSpace(local))
                {
                    var firstWord = local.Split(new[] { '.', '_', '-' }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
                    if (!string.IsNullOrWhiteSpace(firstWord))
                    {
                        var capitalised = char.ToUpperInvariant(firstWord[0]) + firstWord[1..].ToLowerInvariant();
                        return $"{capitalised}'s Hustle";
                    }
                }
            }

            if (!string.IsNullOrWhiteSpace(phone))
                return $"Seller {phone.Trim()[^4..]}";

            return $"Seller {DateTime.UtcNow:yyyyMMddHHmm}";
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
                WhatsAppNumber = merchant.WhatsAppNumber,
                SocialHandle = merchant.SocialHandle,
                IdNumber = merchant.IdNumber,
                ReferralCode = merchant.ReferralCode,
                ReferrerUserId = merchant.ReferrerUserId,
                Province = merchant.Province,
                City = merchant.City,
                AddressLine1 = merchant.AddressLine1,
                Suburb = merchant.Suburb,
                PostalCode = merchant.PostalCode,
                Country = merchant.Country,
                CountryCode = merchant.CountryCode,
                Latitude = merchant.Latitude,
                Longitude = merchant.Longitude,
                GooglePlaceId = merchant.GooglePlaceId,
                FormattedAddress = merchant.FormattedAddress,
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
