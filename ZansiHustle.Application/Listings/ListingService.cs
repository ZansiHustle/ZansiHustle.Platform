using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ZansiHustle.Application.Common.Paging;
using ZansiHustle.Application.Listings.Dtos;
using ZansiHustle.Application.Media.Storage;
using ZansiHustle.Application.Persistence.Listings;
using ZansiHustle.Application.Persistence.Merchants;
using ZansiHustle.Application.Persistence.SellerCategories;
using ZansiHustle.Application.Persistence.Shops;
using ZansiHustle.Domain.Listings;
using ZansiHustle.Shared.Enums.Listings;
using ZansiHustle.Shared.Enums.Merchants;
using ZansiHustle.Shared.Enums.Shops;
using ZansiHustle.Shared.Errors;
using ZansiHustle.Shared.Results;

namespace ZansiHustle.Application.Listings
{
    public class ListingService : IListingService
    {
        private readonly IListingRepository _listingRepository;
        private readonly IMerchantRepository _merchantRepository;
        private readonly ISellerCategoryRepository _sellerCategoryRepository;
        private readonly IShopProfileRepository _shopProfileRepository;
        private readonly IStorageUrlResolver _storageUrlResolver;
        private readonly ILogger<ListingService> _logger;

        public ListingService(
            IListingRepository listingRepository,
            IMerchantRepository merchantRepository,
            ISellerCategoryRepository sellerCategoryRepository,
            IShopProfileRepository shopProfileRepository,
            IStorageUrlResolver storageUrlResolver,
            ILogger<ListingService> logger)
        {
            _listingRepository = listingRepository;
            _merchantRepository = merchantRepository;
            _sellerCategoryRepository = sellerCategoryRepository;
            _shopProfileRepository = shopProfileRepository;
            _storageUrlResolver = storageUrlResolver;
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

                var mapped = new List<ListingListItemDto>(items.Count);
                foreach (var item in items)
                    mapped.Add(await MapToListItemAsync(item));

                var data = new PagedResult<ListingListItemDto>
                {
                    Items = mapped,
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

                return Result<ListingDto>.Success(await MapToDtoAsync(listing), "Listing retrieved successfully.");
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
                var data = new List<ListingListItemDto>(listings.Count);
                foreach (var item in listings)
                    data.Add(await MapToListItemAsync(item));

                return Result<List<ListingListItemDto>>.Success(data, "Shop listings retrieved successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve listings for merchant {MerchantId}.", merchantId);
                return Result<List<ListingListItemDto>>.Failure(ErrorCodes.Exception, $"Failed to retrieve shop listings. {ex.Message}");
            }
        }

        /// <inheritdoc />
        public async Task<Result<List<ListingListItemDto>>> GetByShopProfileAsync(Guid shopProfileId)
        {
            try
            {
                // We don't 404 on a missing shop here — return an empty
                // list. The mobile client's ShopProfile page already
                // handles "shop loaded with zero listings" gracefully,
                // and an empty list is also the correct response for a
                // brand-new shop with no items yet.
                var listings = await _listingRepository.GetByShopProfileAsync(shopProfileId);
                var data = new List<ListingListItemDto>(listings.Count);
                foreach (var item in listings)
                    data.Add(await MapToListItemAsync(item));

                return Result<List<ListingListItemDto>>.Success(data, "Shop listings retrieved successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve listings for shop profile {ShopProfileId}.", shopProfileId);
                return Result<List<ListingListItemDto>>.Failure(ErrorCodes.Exception, $"Failed to retrieve shop listings. {ex.Message}");
            }
        }

        /// <inheritdoc />
        public async Task<Result<List<ListingListItemDto>>> GetMineAsync(Guid ownerUserId)
        {
            try
            {
                var listings = await _listingRepository.GetByOwnerAsync(ownerUserId);
                var data = new List<ListingListItemDto>(listings.Count);
                foreach (var item in listings)
                    data.Add(await MapToListItemAsync(item));

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

                // Resolve ListingSource + validate ShopProfileId.
                //   • Caller-supplied source wins. When omitted, infer
                //     from the merchant type: PhysicalStore merchants
                //     get PhysicalStore, OnlineStore merchants get
                //     SellerAccount (the safe default — never silently
                //     attach to a shop the caller didn't explicitly
                //     pick).
                //   • ShopProfileId MUST be set when source is
                //     ShopProfile, MUST be null otherwise.
                //   • The referenced ShopProfile MUST exist, NOT be
                //     suspended, and belong to the SAME MerchantId as
                //     the listing. This stops a seller from listing
                //     under someone else's shop even if they guess
                //     the GUID.
                var resolvedSource = request.ListingSource
                    ?? (merchant.Type == MerchantType.PhysicalStore
                        ? ListingSource.PhysicalStore
                        : ListingSource.SellerAccount);

                if (resolvedSource == ListingSource.ShopProfile)
                {
                    if (request.ShopProfileId is null)
                        return Result<ListingDto>.Failure(
                            ErrorCodes.BadRequest,
                            "ShopProfileId is required when listing under a shop storefront.");

                    var shop = await _shopProfileRepository.GetByIdAsync(request.ShopProfileId.Value);
                    if (shop is null)
                        return Result<ListingDto>.Failure(
                            ErrorCodes.NotFound,
                            "The selected shop profile does not exist.");

                    if (shop.MerchantId != request.MerchantId)
                        return Result<ListingDto>.Failure(
                            ErrorCodes.Forbidden,
                            "The selected shop profile does not belong to this merchant.");

                    if (shop.Status == ShopProfileStatus.Suspended)
                        return Result<ListingDto>.Failure(
                            ErrorCodes.Forbidden,
                            "Cannot list under a suspended shop profile.");
                }
                else if (request.ShopProfileId is not null)
                {
                    return Result<ListingDto>.Failure(
                        ErrorCodes.BadRequest,
                        "ShopProfileId may only be provided when listing under a shop storefront.");
                }

                if (resolvedSource == ListingSource.PhysicalStore &&
                    merchant.Type != MerchantType.PhysicalStore)
                {
                    return Result<ListingDto>.Failure(
                        ErrorCodes.BadRequest,
                        "PhysicalStore listings require a physical-store merchant.");
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
                    ListingSource = resolvedSource,
                    ShopProfileId = resolvedSource == ListingSource.ShopProfile
                        ? request.ShopProfileId
                        : null,
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

                // Variants: optional on create. Validated here and
                // attached to the entity so the same SaveChangesAsync
                // call below persists everything in one round-trip.
                // Ids from the request are ignored on create — every
                // variant is a fresh insert under the new listing.
                var variantCheck = ValidateVariants(request.Variants);
                if (!variantCheck.IsSuccess)
                    return Result<ListingDto>.Failure(variantCheck.Code, variantCheck.Message);

                if (request.Variants is { Count: > 0 })
                {
                    listing.Variants = BuildVariantsForCreate(request.Variants, listing.Id);
                }

                await _listingRepository.AddAsync(listing);
                var saved = await _listingRepository.SaveChangesAsync();

                if (!saved)
                    return Result<ListingDto>.Failure(ErrorCodes.Exception, "Failed to create listing.");

                var reloaded = await _listingRepository.GetByIdAsync(listing.Id);
                return Result<ListingDto>.Success(await MapToDtoAsync(reloaded ?? listing), "Listing created successfully.");
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

                // Variants: null in the request → leave variants
                // untouched. Non-null (empty included) → REPLACE the
                // full set. We previously tried a diff-by-id strategy
                // and it kept producing false DbUpdateConcurrency
                // failures on perfectly normal edits — the tracker /
                // navigation-collection state got fragile around new
                // Added rows + matched Modified rows in the same
                // SaveChanges. The replace strategy is simpler and
                // production-safe: ExecuteDelete every row for this
                // listing, then INSERT the requested set with fresh
                // server-issued PKs. Wrapped in a single transaction
                // so a half-replace can't happen.
                //
                // Trade-off: variant IDs are NOT stable across an
                // update. That's acceptable today because no order /
                // cart / wishlist references variant ids yet; when
                // those ship we'll move to a soft-delete / versioning
                // model. For now, the buyer detail screen always
                // refetches after the seller saves, so it picks up
                // the new ids automatically.
                IReadOnlyList<ListingVariant>? newVariants = null;
                if (request.Variants is not null)
                {
                    var variantCheck = ValidateVariants(request.Variants);
                    if (!variantCheck.IsSuccess)
                        return Result<ListingDto>.Failure(variantCheck.Code, variantCheck.Message);

                    // BuildVariantsForCreate already issues fresh Guids
                    // and ignores any client-supplied `Id` field — same
                    // function the create path uses. Reusing it keeps
                    // the variant construction in one place.
                    newVariants = BuildVariantsForCreate(request.Variants, listing.Id);
                }

                listing.UpdatedAtUtc = DateTime.UtcNow;

                // Atomic save: scalar listing changes + variant replace
                // in one transaction. The repo handles tracker cleanup
                // around the ExecuteDelete so the navigation collection
                // doesn't drag stale-tracked entities into SaveChanges.
                bool saved;
                try
                {
                    saved = await _listingRepository.SaveListingAndReplaceVariantsAsync(
                        listing,
                        newVariants);
                }
                catch (DbUpdateConcurrencyException ex)
                {
                    // Verbose log so the next concurrency report in
                    // prod tells us exactly which entity / state / PK
                    // tripped the 0-rows check, instead of just the
                    // entity-type name.
                    var entries = string.Join(
                        " | ",
                        ex.Entries.Select(e =>
                        {
                            var pkProp = e.Metadata.FindPrimaryKey()?.Properties.FirstOrDefault();
                            var pk = pkProp is null
                                ? "?"
                                : e.Property(pkProp.Name).CurrentValue?.ToString() ?? "(null)";
                            return $"{e.Entity.GetType().Name}#{pk}[{e.State}]";
                        }));
                    _logger.LogWarning(
                        ex,
                        "Concurrency conflict updating listing {ListingId}. Failing entries: {Entries}",
                        listingId,
                        entries);
                    return Result<ListingDto>.Failure(
                        ErrorCodes.Conflict,
                        "This listing was updated or deleted in another session. Refresh and try again.");
                }
                catch (DbUpdateException ex)
                {
                    _logger.LogError(ex, "Database error updating listing {ListingId}.", listingId);
                    return Result<ListingDto>.Failure(
                        ErrorCodes.Exception,
                        "Could not save your listing. Please try again.");
                }

                if (!saved)
                    return Result<ListingDto>.Failure(ErrorCodes.Exception, "Failed to update listing.");

                var reloaded = await _listingRepository.GetByIdAsync(listing.Id);
                return Result<ListingDto>.Success(await MapToDtoAsync(reloaded ?? listing), "Listing updated successfully.");
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

        // ── Variants ────────────────────────────────────────────────

        /// <summary>
        /// Hard cap on variants per listing. Keeps a single saved
        /// listing from ballooning into a giant payload and forces the
        /// seller to think in terms of meaningful options rather than
        /// listing every permutation.
        /// </summary>
        private const int MaxVariantsPerListing = 20;

        /// <summary>
        /// Shape-only validation for a variant request set. Null input
        /// (i.e. caller didn't send a variants section) is success.
        /// </summary>
        private static Result ValidateVariants(List<ListingVariantRequestDto>? variants)
        {
            if (variants is null) return Result.Success();
            if (variants.Count > MaxVariantsPerListing)
                return Result.Failure(
                    ErrorCodes.BadRequest,
                    $"A listing can have at most {MaxVariantsPerListing} variants.");

            for (var i = 0; i < variants.Count; i++)
            {
                var v = variants[i];
                if (v is null)
                    return Result.Failure(ErrorCodes.BadRequest, $"Variant #{i + 1} is missing.");

                if (string.IsNullOrWhiteSpace(v.Name))
                    return Result.Failure(ErrorCodes.BadRequest, $"Variant #{i + 1} needs a name.");

                if (v.Name.Trim().Length > 80)
                    return Result.Failure(ErrorCodes.BadRequest, $"Variant name is too long (max 80 chars).");

                if (!string.IsNullOrEmpty(v.Description) && v.Description.Length > 300)
                    return Result.Failure(ErrorCodes.BadRequest, $"Variant description is too long (max 300 chars).");

                if (!string.IsNullOrEmpty(v.Sku) && v.Sku.Length > 64)
                    return Result.Failure(ErrorCodes.BadRequest, "Variant SKU is too long (max 64 chars).");

                if (v.UsesCustomPrice)
                {
                    if (!v.Price.HasValue)
                        return Result.Failure(
                            ErrorCodes.BadRequest,
                            $"Variant \"{v.Name.Trim()}\" needs a custom price (or untick custom pricing).");
                    if (v.Price.Value < 0)
                        return Result.Failure(
                            ErrorCodes.BadRequest,
                            $"Variant \"{v.Name.Trim()}\" price can't be negative.");
                }

                if (v.Stock is < 0)
                    return Result.Failure(
                        ErrorCodes.BadRequest,
                        $"Variant \"{v.Name.Trim()}\" stock can't be negative.");
            }

            // Reject duplicate names (case-insensitive) — two "White"
            // rows on the same listing is almost always a user error
            // and would confuse buyers + the future cart snapshot.
            var dupGroup = variants
                .Where(v => !string.IsNullOrWhiteSpace(v.Name))
                .GroupBy(v => v.Name.Trim(), StringComparer.OrdinalIgnoreCase)
                .FirstOrDefault(g => g.Count() > 1);
            if (dupGroup is not null)
                return Result.Failure(
                    ErrorCodes.BadRequest,
                    $"Variant \"{dupGroup.Key}\" appears more than once.");

            return Result.Success();
        }

        /// <summary>
        /// Build a fresh variant collection for a brand-new listing.
        /// Any client-supplied <c>Id</c> is intentionally ignored —
        /// every row gets a new server-issued id on create. SortOrder
        /// falls back to the request order when not supplied.
        /// </summary>
        private static List<ListingVariant> BuildVariantsForCreate(
            List<ListingVariantRequestDto> variants, Guid listingId)
        {
            var now = DateTime.UtcNow;
            var result = new List<ListingVariant>(variants.Count);
            for (var i = 0; i < variants.Count; i++)
            {
                var v = variants[i];
                result.Add(new ListingVariant
                {
                    Id = Guid.NewGuid(),
                    ListingId = listingId,
                    Name = v.Name.Trim(),
                    Description = string.IsNullOrWhiteSpace(v.Description) ? null : v.Description.Trim(),
                    UsesCustomPrice = v.UsesCustomPrice,
                    Price = v.UsesCustomPrice ? v.Price : null,
                    Stock = v.Stock,
                    Sku = string.IsNullOrWhiteSpace(v.Sku) ? null : v.Sku.Trim(),
                    // Empty SortOrder → fall back to request index so the
                    // saved order matches the seller's input order.
                    SortOrder = v.SortOrder == 0 ? i : v.SortOrder,
                    IsActive = v.IsActive,
                    CreatedAtUtc = now,
                });
            }
            return result;
        }

        // NOTE: ApplyVariantDiff is gone. Variant updates now use a
        // wholesale-replace strategy via
        // IListingRepository.SaveListingAndReplaceVariantsAsync — see
        // the doc comment on that method for the why. Construction of
        // the new variant entities flows through BuildVariantsForCreate
        // for both create and update paths.

        private static ListingVariantDto MapVariantToDto(ListingVariant variant)
        {
            return new ListingVariantDto
            {
                Id = variant.Id,
                ListingId = variant.ListingId,
                Name = variant.Name,
                Description = variant.Description,
                Price = variant.UsesCustomPrice ? variant.Price : null,
                UsesCustomPrice = variant.UsesCustomPrice,
                Stock = variant.Stock,
                Sku = variant.Sku,
                SortOrder = variant.SortOrder,
                IsActive = variant.IsActive,
                CreatedAtUtc = variant.CreatedAtUtc,
                UpdatedAtUtc = variant.UpdatedAtUtc,
            };
        }

        /// <summary>
        /// Refresh every stored image URL through <see cref="IStorageUrlResolver"/>.
        /// Mirrors the pattern in <c>MarketplaceListingService.MapDtoAsync</c> and
        /// <c>MerchantService</c>: the resolver is a no-op for permanent
        /// CDN URLs (<c>Storage:R2:PublicBaseUrl</c> set) and re-issues a
        /// fresh signed URL for legacy <c>cloudflarestorage.com</c> rows
        /// or expired LocalFilesystem signed URLs. Without this step,
        /// stored URLs that signed at upload time go 403 once the TTL
        /// expires and the listing's image grid silently breaks.
        /// </summary>
        private async Task<List<string>> ResolveImagesAsync(Listing listing)
        {
            var raw = listing.Images ?? new List<string>();
            if (raw.Count == 0) return new List<string>();

            var resolved = new List<string>(raw.Count);
            foreach (var img in raw)
            {
                var refreshed = await _storageUrlResolver.RefreshAsync(img) ?? img;
                _logger.LogDebug(
                    "[ListingImageResolver] listing={ListingId} stored={Stored} resolved={Resolved}",
                    listing.Id, img, refreshed);
                resolved.Add(refreshed);
            }
            return resolved;
        }

        private async Task<ListingDto> MapToDtoAsync(Listing listing)
        {
            var images = await ResolveImagesAsync(listing);
            return new ListingDto
            {
                Id = listing.Id,
                Code = listing.Code,
                Slug = listing.Slug,
                Type = listing.Type,
                Status = listing.Status,
                AvailabilityMode = listing.AvailabilityMode,
                ListingSource = listing.ListingSource,
                ShopProfileId = listing.ShopProfileId,
                ShopProfileName = listing.ShopProfile?.Name,
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
                Images = images,
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
                // Project variants in stable seller-defined order. The
                // seller-side edit flow round-trips on this list, so we
                // surface ALL variants (including inactive) — the buyer
                // detail screen filters by IsActive for display.
                Variants = listing.Variants
                    .OrderBy(v => v.SortOrder)
                    .ThenBy(v => v.CreatedAtUtc)
                    .Select(MapVariantToDto)
                    .ToList(),
                CreatedAtUtc = listing.CreatedAtUtc,
                UpdatedAtUtc = listing.UpdatedAtUtc
            };
        }

        private async Task<ListingListItemDto> MapToListItemAsync(Listing listing)
        {
            var images = await ResolveImagesAsync(listing);
            return new ListingListItemDto
            {
                Id = listing.Id,
                Slug = listing.Slug,
                Type = listing.Type,
                Status = listing.Status,
                AvailabilityMode = listing.AvailabilityMode,
                ListingSource = listing.ListingSource,
                ShopProfileId = listing.ShopProfileId,
                ShopProfileName = listing.ShopProfile?.Name,
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
                Images = images,
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
                // Active-only count for the buyer feed cards — soft-
                // hidden variants don't influence the "X options" hint.
                VariantCount = listing.Variants.Count(v => v.IsActive),
                CreatedAtUtc = listing.CreatedAtUtc
            };
        }
    }
}
