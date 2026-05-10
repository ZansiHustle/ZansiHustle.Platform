using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ZansiHustle.Application.Common.Paging;
using ZansiHustle.Application.Listings.Dtos;
using ZansiHustle.Application.Persistence.Listings;
using ZansiHustle.Application.Persistence.Merchants;
using ZansiHustle.Application.Persistence.SellerCategories;
using ZansiHustle.Domain.Listings;
using ZansiHustle.Shared.Enums.Listings;
using ZansiHustle.Shared.Enums.Merchants;
using ZansiHustle.Shared.Errors;
using ZansiHustle.Shared.Results;

namespace ZansiHustle.Application.Listings
{
    public class ListingService : IListingService
    {
        private readonly IListingRepository _listingRepository;
        private readonly IMerchantRepository _merchantRepository;
        private readonly ISellerCategoryRepository _sellerCategoryRepository;
        private readonly ILogger<ListingService> _logger;

        public ListingService(IListingRepository listingRepository, IMerchantRepository merchantRepository, ISellerCategoryRepository sellerCategoryRepository, ILogger<ListingService> logger)
        {
            _listingRepository = listingRepository;
            _merchantRepository = merchantRepository;
            _sellerCategoryRepository = sellerCategoryRepository;
            _logger = logger;
        }

        /// <inheritdoc />
        public async Task<Result<PagedResult<ListingListItemDto>>> SearchAsync(ListingFilterRequestDto filter)
        {
            try
            {
                filter ??= new ListingFilterRequestDto();

                var (items, total) = await _listingRepository.SearchAsync(filter);

                var page = filter.Page <= 0 ? 1 : filter.Page;
                var pageSize = filter.PageSize <= 0 ? 20 : (filter.PageSize > 100 ? 100 : filter.PageSize);

                var data = new PagedResult<ListingListItemDto>
                {
                    Items = items.Select(MapToListItem).ToList(),
                    Total = total,
                    Page = page,
                    PageSize = pageSize
                };

                return Result<PagedResult<ListingListItemDto>>.Success(data, "Listings retrieved successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Listing search failed.");
                return Result<PagedResult<ListingListItemDto>>.Failure(ErrorCodes.Exception, $"Failed to retrieve listings. {ex.Message}");
            }
        }

        /// <inheritdoc />
        public async Task<Result<ListingDto>> GetByIdAsync(Guid id)
        {
            try
            {
                var listing = await _listingRepository.GetByIdAsync(id);

                if (listing is null)
                    return Result<ListingDto>.Failure(ErrorCodes.NotFound, "Listing not found.");

                return Result<ListingDto>.Success(MapToDto(listing), "Listing retrieved successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve listing {Id}.", id);
                return Result<ListingDto>.Failure(ErrorCodes.Exception, $"Failed to retrieve listing. {ex.Message}");
            }
        }

        /// <inheritdoc />
        public async Task<Result<List<ListingListItemDto>>> GetByMerchantAsync(Guid merchantId)
        {
            try
            {
                var merchant = await _merchantRepository.GetByIdAsync(merchantId);

                if (merchant is null)
                    return Result<List<ListingListItemDto>>.Failure(ErrorCodes.NotFound, "Shop not found.");

                var listings = await _listingRepository.GetByMerchantAsync(merchantId);
                var data = listings.Select(MapToListItem).ToList();

                return Result<List<ListingListItemDto>>.Success(data, "Shop listings retrieved successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve listings for merchant {MerchantId}.", merchantId);
                return Result<List<ListingListItemDto>>.Failure(ErrorCodes.Exception, $"Failed to retrieve shop listings. {ex.Message}");
            }
        }

        /// <inheritdoc />
        public async Task<Result<List<ListingListItemDto>>> GetMineAsync(Guid ownerUserId)
        {
            try
            {
                var listings = await _listingRepository.GetByOwnerAsync(ownerUserId);
                var data = listings.Select(MapToListItem).ToList();

                return Result<List<ListingListItemDto>>.Success(data, "Your listings retrieved successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve owner listings for user {UserId}.", ownerUserId);
                return Result<List<ListingListItemDto>>.Failure(ErrorCodes.Exception, $"Failed to retrieve your listings. {ex.Message}");
            }
        }

        /// <inheritdoc />
        public async Task<Result<ListingDto>> CreateAsync(Guid ownerUserId, CreateListingRequestDto request)
        {
            try
            {
                if (request is null)
                    return Result<ListingDto>.Failure(ErrorCodes.BadRequest, "Request is required.");

                if (string.IsNullOrWhiteSpace(request.Title))
                    return Result<ListingDto>.Failure(ErrorCodes.BadRequest, "Listing title is required.");

                var typeCheck = ValidateTypeShape(request.Type, request.PricingModel, request.Stock);

                if (!typeCheck.IsSuccess)
                    return Result<ListingDto>.Failure(typeCheck.Code, typeCheck.Message);

                var merchant = await _merchantRepository.GetByIdAsync(request.MerchantId);

                if (merchant is null)
                    return Result<ListingDto>.Failure(ErrorCodes.NotFound, "Shop not found.");

                if (merchant.OwnerUserId != ownerUserId)
                    return Result<ListingDto>.Failure(ErrorCodes.Forbidden, "You do not have permission to add listings to this shop.");

                if (merchant.Status != ZansiHustle.Shared.Enums.Merchants.MerchantStatus.Active)
                    return Result<ListingDto>.Failure(ErrorCodes.Forbidden, "Your shop must be approved before you can publish listings.");

                var categoryCheck = await ValidateCategoriesAsync(request.SellerCategoryId, request.SellerSubcategoryId);

                if (!categoryCheck.IsSuccess)
                    return Result<ListingDto>.Failure(categoryCheck.Code, categoryCheck.Message);

                // Resolve AvailabilityMode:
                //   • Caller-supplied value wins, subject to validation.
                //   • Otherwise default by merchant type — PhysicalStore
                //     defaults to InStoreOnly so a store-only owner who
                //     submits a listing without specifying mode lands
                //     in their catalog instead of the public feed.
                //   • InStoreOnly is rejected for non-PhysicalStore
                //     merchants (would never surface in any feed).
                var resolvedAvailability = request.AvailabilityMode
                    ?? (merchant.Type == MerchantType.PhysicalStore
                        ? AvailabilityMode.InStoreOnly
                        : AvailabilityMode.OnlineOnly);

                if (resolvedAvailability == AvailabilityMode.InStoreOnly &&
                    merchant.Type != MerchantType.PhysicalStore)
                {
                    return Result<ListingDto>.Failure(
                        ErrorCodes.BadRequest,
                        "InStoreOnly listings require a physical-store merchant.");
                }

                // Defensive guard against bad image strings. Mobile
                // clients now upload to R2 before saving, but earlier
                // builds were persisting picker URIs (`blob:`,
                // `file:`, raw UUIDs) directly. Reject anything that
                // isn't an absolute http(s) URL so the data layer is
                // self-protecting regardless of client version.
                var imageCheck = ValidateImageUrls(request.Images);
                if (!imageCheck.IsSuccess)
                    return Result<ListingDto>.Failure(imageCheck.Code, imageCheck.Message);

                var listing = new Listing
                {
                    Id = Guid.NewGuid(),
                    Code = GenerateCode(),
                    Slug = await GenerateUniqueSlugAsync(request.Title),
                    Type = request.Type,
                    Status = request.Status ?? ListingStatus.Active,
                    AvailabilityMode = resolvedAvailability,
                    MerchantId = request.MerchantId,
                    Title = request.Title.Trim(),
                    Description = request.Description?.Trim(),
                    Price = request.Price < 0 ? 0 : request.Price,
                    Currency = string.IsNullOrWhiteSpace(request.Currency) ? "ZAR" : request.Currency!.Trim().ToUpperInvariant(),
                    SellerCategoryId = request.SellerCategoryId,
                    SellerSubcategoryId = request.SellerSubcategoryId,
                    Province = request.Province?.Trim(),
                    City = request.City?.Trim(),
                    Images = request.Images?.Where(s => !string.IsNullOrWhiteSpace(s)).Select(s => s.Trim()).ToList() ?? new List<string>(),
                    IsFeatured = false,
                    IsBoosted = false,
                    Rating = null,
                    ReviewCount = 0,
                    CreatedAtUtc = DateTime.UtcNow
                };

                if (request.Type == ListingType.Product)
                {
                    listing.Stock = request.Stock ?? 0;
                    listing.Condition = request.Condition;
                    listing.DeliveryOptions = Clean(request.DeliveryOptions);
                }
                else
                {
                    listing.PricingModel = request.PricingModel ?? PricingModel.Fixed;
                    listing.ServiceArea = request.ServiceArea?.Trim();
                    listing.Turnaround = request.Turnaround?.Trim();
                    listing.Availability = Clean(request.Availability);
                    listing.BookingMethods = Clean(request.BookingMethods);
                }

                await _listingRepository.AddAsync(listing);
                var saved = await _listingRepository.SaveChangesAsync();

                if (!saved)
                    return Result<ListingDto>.Failure(ErrorCodes.Exception, "Failed to create listing.");

                var reloaded = await _listingRepository.GetByIdAsync(listing.Id);
                return Result<ListingDto>.Success(MapToDto(reloaded ?? listing), "Listing created successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Listing create failed.");
                return Result<ListingDto>.Failure(ErrorCodes.Exception, $"Failed to create listing. {ex.Message}");
            }
        }

        /// <inheritdoc />
        public async Task<Result<ListingDto>> UpdateAsync(Guid ownerUserId, Guid listingId, UpdateListingRequestDto request)
        {
            try
            {
                if (request is null)
                    return Result<ListingDto>.Failure(ErrorCodes.BadRequest, "Request is required.");

                if (string.IsNullOrWhiteSpace(request.Title))
                    return Result<ListingDto>.Failure(ErrorCodes.BadRequest, "Listing title is required.");

                var listing = await _listingRepository.GetByIdAsync(listingId);

                if (listing is null)
                    return Result<ListingDto>.Failure(ErrorCodes.NotFound, "Listing not found.");

                if (listing.Merchant is null || listing.Merchant.OwnerUserId != ownerUserId)
                    return Result<ListingDto>.Failure(ErrorCodes.Forbidden, "You do not have permission to update this listing.");

                if (listing.Merchant.Status != ZansiHustle.Shared.Enums.Merchants.MerchantStatus.Active)
                    return Result<ListingDto>.Failure(ErrorCodes.Forbidden, "Your shop must be approved before you can update listings.");

                var typeCheck = ValidateTypeShape(listing.Type, request.PricingModel, request.Stock);

                if (!typeCheck.IsSuccess)
                    return Result<ListingDto>.Failure(typeCheck.Code, typeCheck.Message);

                var categoryCheck = await ValidateCategoriesAsync(request.SellerCategoryId, request.SellerSubcategoryId);

                if (!categoryCheck.IsSuccess)
                    return Result<ListingDto>.Failure(categoryCheck.Code, categoryCheck.Message);

                listing.Title = request.Title.Trim();
                listing.Description = request.Description?.Trim();
                listing.Price = request.Price < 0 ? 0 : request.Price;
                listing.Currency = string.IsNullOrWhiteSpace(request.Currency) ? listing.Currency : request.Currency!.Trim().ToUpperInvariant();
                listing.SellerCategoryId = request.SellerCategoryId;
                listing.SellerSubcategoryId = request.SellerSubcategoryId;
                listing.Province = request.Province?.Trim();
                listing.City = request.City?.Trim();

                if (request.Images is not null)
                {
                    var updateImageCheck = ValidateImageUrls(request.Images);
                    if (!updateImageCheck.IsSuccess)
                        return Result<ListingDto>.Failure(updateImageCheck.Code, updateImageCheck.Message);
                    listing.Images = request.Images
                        .Where(s => !string.IsNullOrWhiteSpace(s))
                        .Select(s => s.Trim())
                        .ToList();
                }

                if (request.Status.HasValue)
                    listing.Status = request.Status.Value;

                // Owners may flip AvailabilityMode at any time, but the
                // merchant-type rule still applies — InStoreOnly only
                // makes sense for a PhysicalStore merchant.
                if (request.AvailabilityMode.HasValue)
                {
                    if (request.AvailabilityMode.Value == AvailabilityMode.InStoreOnly &&
                        listing.Merchant!.Type != MerchantType.PhysicalStore)
                    {
                        return Result<ListingDto>.Failure(
                            ErrorCodes.BadRequest,
                            "InStoreOnly listings require a physical-store merchant.");
                    }

                    listing.AvailabilityMode = request.AvailabilityMode.Value;
                }

                if (listing.Type == ListingType.Product)
                {
                    listing.Stock = request.Stock ?? listing.Stock;
                    listing.Condition = request.Condition ?? listing.Condition;

                    if (request.DeliveryOptions is not null)
                        listing.DeliveryOptions = Clean(request.DeliveryOptions);
                }
                else
                {
                    listing.PricingModel = request.PricingModel ?? listing.PricingModel;
                    listing.ServiceArea = request.ServiceArea?.Trim() ?? listing.ServiceArea;
                    listing.Turnaround = request.Turnaround?.Trim() ?? listing.Turnaround;

                    if (request.Availability is not null)
                        listing.Availability = Clean(request.Availability);

                    if (request.BookingMethods is not null)
                        listing.BookingMethods = Clean(request.BookingMethods);
                }

                listing.UpdatedAtUtc = DateTime.UtcNow;

                _listingRepository.Update(listing);
                var saved = await _listingRepository.SaveChangesAsync();

                if (!saved)
                    return Result<ListingDto>.Failure(ErrorCodes.Exception, "Failed to update listing.");

                var reloaded = await _listingRepository.GetByIdAsync(listing.Id);
                return Result<ListingDto>.Success(MapToDto(reloaded ?? listing), "Listing updated successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Listing update failed for {ListingId}.", listingId);
                return Result<ListingDto>.Failure(ErrorCodes.Exception, $"Failed to update listing. {ex.Message}");
            }
        }

        /// <inheritdoc />
        public async Task<Result> DeleteAsync(Guid ownerUserId, Guid listingId)
        {
            try
            {
                var listing = await _listingRepository.GetByIdAsync(listingId);

                if (listing is null)
                    return Result.Failure(ErrorCodes.NotFound, "Listing not found.");

                if (listing.Merchant is null || listing.Merchant.OwnerUserId != ownerUserId)
                    return Result.Failure(ErrorCodes.Forbidden, "You do not have permission to delete this listing.");

                if (listing.Merchant.Status != ZansiHustle.Shared.Enums.Merchants.MerchantStatus.Active)
                    return Result.Failure(ErrorCodes.Forbidden, "Your shop must be approved before you can delete listings.");

                _listingRepository.Delete(listing);
                var saved = await _listingRepository.SaveChangesAsync();

                if (!saved)
                    return Result.Failure(ErrorCodes.Exception, "Failed to delete listing.");

                return Result.Success("Listing deleted successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Listing delete failed for {ListingId}.", listingId);
                return Result.Failure(ErrorCodes.Exception, $"Failed to delete listing. {ex.Message}");
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

        /// <summary>
        /// Reject image strings that aren't absolute http(s) URLs. Catches
        /// legacy mobile clients that may have shipped picker URIs
        /// (`blob:`, `file:`, `content:`) or raw media-asset UUIDs
        /// directly into the listing payload — those values were never
        /// uploaded to R2 and would 404 on every cross-session render.
        /// New clients upload via `uploadMediaAsset` first; this guard
        /// is the data layer's belt-and-braces.
        /// </summary>
        private static Result ValidateImageUrls(IEnumerable<string>? images)
        {
            if (images is null) return Result.Success();

            foreach (var raw in images)
            {
                if (string.IsNullOrWhiteSpace(raw)) continue;
                var trimmed = raw.Trim();
                var lower = trimmed.ToLowerInvariant();
                if (!lower.StartsWith("http://") && !lower.StartsWith("https://"))
                {
                    return Result.Failure(
                        ErrorCodes.BadRequest,
                        "Listing images must be uploaded before saving — only http(s) URLs are accepted.");
                }
            }

            return Result.Success();
        }

        private static Result ValidateTypeShape(ListingType type, PricingModel? pricingModel, int? stock)
        {
            if (type == ListingType.Service && pricingModel is null)
            {
                // Not fatal — CreateAsync defaults to Fixed. Kept open for future
                // stricter validation.
                return Result.Success();
            }

            if (type == ListingType.Product && stock is < 0)
                return Result.Failure(ErrorCodes.BadRequest, "Stock cannot be negative.");

            return Result.Success();
        }

        private async Task<string> GenerateUniqueSlugAsync(string title)
        {
            var baseSlug = Slugify(title);

            if (string.IsNullOrEmpty(baseSlug))
                baseSlug = "listing";

            var candidate = baseSlug;
            var suffix = 2;
            while (await _listingRepository.ExistsBySlugAsync(candidate))
            {
                candidate = $"{baseSlug}-{suffix}";
                suffix++;

                if (suffix > 9999)
                {
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
            var normalized = lower.Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder(normalized.Length);
            foreach (var ch in normalized)
            {
                var cat = CharUnicodeInfo.GetUnicodeCategory(ch);

                if (cat != UnicodeCategory.NonSpacingMark)
                    sb.Append(ch);
            }

            var cleaned = sb.ToString().Normalize(NormalizationForm.FormC);
            cleaned = Regex.Replace(cleaned, @"[^a-z0-9\s-]", " ");
            cleaned = Regex.Replace(cleaned, @"[\s-]+", "-").Trim('-');

            if (cleaned.Length > 100)
                cleaned = cleaned[..100].Trim('-');

            return cleaned;
        }

        private static string GenerateCode()
        {
            return $"LIS-{DateTime.UtcNow:yyyyMMddHHmmssfff}";
        }

        private static List<string>? Clean(List<string>? values)
        {
            if (values is null)
                return null;

            var cleaned = values
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Select(s => s.Trim())
                .ToList();

            return cleaned.Count == 0 ? null : cleaned;
        }

        private static ListingDto MapToDto(Listing listing)
        {
            return new ListingDto
            {
                Id = listing.Id,
                Code = listing.Code,
                Slug = listing.Slug,
                Type = listing.Type,
                Status = listing.Status,
                AvailabilityMode = listing.AvailabilityMode,
                MerchantId = listing.MerchantId,
                MerchantName = listing.Merchant?.Name,
                MerchantSlug = listing.Merchant?.Slug,
                MerchantLogoUrl = listing.Merchant?.LogoUrl,
                Title = listing.Title,
                Description = listing.Description,
                Price = listing.Price,
                Currency = listing.Currency,
                SellerCategoryId = listing.SellerCategoryId,
                SellerCategoryName = listing.SellerCategory?.Name,
                SellerSubcategoryId = listing.SellerSubcategoryId,
                SellerSubcategoryName = listing.SellerSubcategory?.Name,
                Province = listing.Province,
                City = listing.City,
                Images = listing.Images ?? new List<string>(),
                IsFeatured = listing.IsFeatured,
                IsBoosted = listing.IsBoosted,
                // Source-of-truth correction — same as MerchantService
                // (see comment there). A listing with zero reviews must
                // not surface a non-null rating.
                Rating = listing.ReviewCount > 0 ? listing.Rating : null,
                ReviewCount = listing.ReviewCount,
                Stock = listing.Stock,
                Condition = listing.Condition,
                DeliveryOptions = listing.DeliveryOptions,
                PricingModel = listing.PricingModel,
                ServiceArea = listing.ServiceArea,
                Turnaround = listing.Turnaround,
                Availability = listing.Availability,
                BookingMethods = listing.BookingMethods,
                CreatedAtUtc = listing.CreatedAtUtc,
                UpdatedAtUtc = listing.UpdatedAtUtc
            };
        }

        private static ListingListItemDto MapToListItem(Listing listing)
        {
            return new ListingListItemDto
            {
                Id = listing.Id,
                Slug = listing.Slug,
                Type = listing.Type,
                Status = listing.Status,
                AvailabilityMode = listing.AvailabilityMode,
                MerchantId = listing.MerchantId,
                MerchantName = listing.Merchant?.Name,
                MerchantSlug = listing.Merchant?.Slug,
                Title = listing.Title,
                Price = listing.Price,
                Currency = listing.Currency,
                SellerCategoryId = listing.SellerCategoryId,
                SellerCategoryName = listing.SellerCategory?.Name,
                Province = listing.Province,
                City = listing.City,
                Images = listing.Images ?? new List<string>(),
                IsFeatured = listing.IsFeatured,
                IsBoosted = listing.IsBoosted,
                // Source-of-truth correction — same as MerchantService
                // (see comment there). A listing with zero reviews must
                // not surface a non-null rating.
                Rating = listing.ReviewCount > 0 ? listing.Rating : null,
                ReviewCount = listing.ReviewCount,
                Stock = listing.Stock,
                Condition = listing.Condition,
                PricingModel = listing.PricingModel,
                CreatedAtUtc = listing.CreatedAtUtc
            };
        }
    }
}
