using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ZansiHustle.Application.Common.Interfaces.Shared;
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
        private readonly ICurrentUserService _currentUser;
        private readonly ILogger<ListingService> _logger;

        public ListingService(
            IListingRepository listingRepository,
            IMerchantRepository merchantRepository,
            ISellerCategoryRepository sellerCategoryRepository,
            IShopProfileRepository shopProfileRepository,
            IStorageUrlResolver storageUrlResolver,
            ICurrentUserService currentUser,
            ILogger<ListingService> logger)
        {
            _listingRepository = listingRepository;
            _merchantRepository = merchantRepository;
            _sellerCategoryRepository = sellerCategoryRepository;
            _shopProfileRepository = shopProfileRepository;
            _storageUrlResolver = storageUrlResolver;
            _currentUser = currentUser;
            _logger = logger;
        }

        // True when the signed-in user owns this listing (via its merchant's
        // owner). Authoritative ownership signal for the mobile app to mark
        // "Your listing" + gate buy/book/review/report on a user's OWN items.
        // Safe for anonymous callers (UserId null → false). Relies on the
        // listing's Merchant navigation already being loaded by the queries
        // (it is — MerchantName/Slug are mapped from it).
        private bool ComputeIsOwner(Listing listing)
        {
            var me = _currentUser.UserId;
            return me is Guid uid
                && listing.Merchant?.OwnerUserId is Guid ownerId
                && ownerId == uid;
        }

        /// <inheritdoc />
        public async Task<Result<AssignShopItemsResultDto>> AssignToShopAsync(
            Guid ownerUserId, Guid shopProfileId, IReadOnlyCollection<Guid> listingIds)
        {
            try
            {
                if (ownerUserId == Guid.Empty)
                    return Result<AssignShopItemsResultDto>.Failure(ErrorCodes.Unauthorized, "User identifier not found.");

                var shop = await _shopProfileRepository.GetByIdAsync(shopProfileId);
                if (shop is null)
                    return Result<AssignShopItemsResultDto>.Failure(ErrorCodes.NotFound, "The selected shop does not exist.");

                // Ownership: the shop's merchant must be owned by the caller.
                var merchant = await _merchantRepository.GetByIdAsync(shop.MerchantId);
                if (merchant is null || merchant.OwnerUserId != ownerUserId)
                    return Result<AssignShopItemsResultDto>.Failure(ErrorCodes.Forbidden, "You can only manage your own shop.");

                if (shop.Status == ShopProfileStatus.Suspended)
                    return Result<AssignShopItemsResultDto>.Failure(ErrorCodes.Forbidden, "Cannot add items to a suspended shop.");

                var ids = (listingIds ?? Array.Empty<Guid>())
                    .Where(g => g != Guid.Empty).Distinct().ToList();
                if (ids.Count == 0)
                    return Result<AssignShopItemsResultDto>.Failure(ErrorCodes.BadRequest, "Select at least one item to add.");

                var result = new AssignShopItemsResultDto();
                foreach (var id in ids)
                {
                    var listing = await _listingRepository.GetByIdAsync(id);
                    // Skip silently anything that isn't the caller's own item under
                    // this shop's merchant — never touch another seller's listing.
                    if (listing is null
                        || listing.Merchant?.OwnerUserId != ownerUserId
                        || listing.MerchantId != shop.MerchantId)
                    {
                        result.Skipped++;
                        continue;
                    }

                    if (listing.ListingSource == ListingSource.ShopProfile && listing.ShopProfileId == shopProfileId)
                    {
                        result.AlreadyInShop++; // idempotent — no duplicate
                        continue;
                    }

                    listing.ListingSource = ListingSource.ShopProfile;
                    listing.ShopProfileId = shopProfileId;
                    listing.UpdatedAtUtc = DateTime.UtcNow;
                    _listingRepository.Update(listing);
                    result.Attached++;
                }

                if (result.Attached > 0)
                    await _listingRepository.SaveChangesAsync();

                return Result<AssignShopItemsResultDto>.Success(result, "Shop items updated.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AssignToShop failed. shop={ShopId} user={UserId}", shopProfileId, ownerUserId);
                return Result<AssignShopItemsResultDto>.Failure(ErrorCodes.Exception, "Could not update shop items.");
            }
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

                    // Delivery package details (parcel profile).
                    listing.PackageSizeCategory = request.PackageSizeCategory;
                    listing.PackageWeightKg = request.PackageWeightKg;
                    listing.PackageLengthCm = request.PackageLengthCm;
                    listing.PackageWidthCm = request.PackageWidthCm;
                    listing.PackageHeightCm = request.PackageHeightCm;
                    listing.PackageFragile = request.PackageFragile;
                    listing.PackageContentsDescription = request.PackageContentsDescription?.Trim();

                    var parcelCheck = ValidateParcel(listing);
                    if (!parcelCheck.IsSuccess)
                        return Result<ListingDto>.Failure(parcelCheck.Code, parcelCheck.Message);
                }
                else
                {
                    listing.PricingModel = request.PricingModel ?? PricingModel.Fixed;
                    listing.ServiceArea = request.ServiceArea?.Trim();
                    listing.Turnaround = request.Turnaround?.Trim();
                    listing.Availability = Clean(request.Availability);
                    listing.BookingMethods = Clean(request.BookingMethods);

                    // Fulfilment (house call / provider location / both).
                    listing.FulfilmentMode = request.FulfilmentMode;
                    listing.AllowsHouseCall = request.AllowsHouseCall;
                    listing.AllowsProviderLocation = request.AllowsProviderLocation;
                    listing.ProviderLocationName = request.ProviderLocationName?.Trim();
                    listing.ProviderAddressLine1 = request.ProviderAddressLine1?.Trim();
                    listing.ProviderAddressLine2 = request.ProviderAddressLine2?.Trim();
                    listing.ProviderCity = request.ProviderCity?.Trim();
                    listing.ProviderProvince = request.ProviderProvince?.Trim();
                    listing.ProviderPostalCode = request.ProviderPostalCode?.Trim();
                    listing.ProviderLatitude = request.ProviderLatitude;
                    listing.ProviderLongitude = request.ProviderLongitude;
                    listing.TravelFeeType = request.TravelFeeType;
                    listing.TravelFeePerKm = request.TravelFeePerKm;
                    listing.TravelFeeFlatAmount = request.TravelFeeFlatAmount;
                    listing.FreeTravelRadiusKm = request.FreeTravelRadiusKm;
                    listing.MaxTravelDistanceKm = request.MaxTravelDistanceKm;
                    listing.TravelFeeMinimum = request.TravelFeeMinimum;
                    listing.TravelFeeMaximum = request.TravelFeeMaximum;
                    listing.HouseCallSurchargeAmount = request.HouseCallSurchargeAmount;
                    listing.LeadTimeHours = request.LeadTimeHours;
                    listing.BufferMinutes = request.BufferMinutes;
                    listing.EstimatedDurationMinutes = request.EstimatedDurationMinutes;

                    NormalizeFulfilment(listing);
                    var fulfilCheck = ValidateServiceFulfilment(listing);
                    if (!fulfilCheck.IsSuccess)
                        return Result<ListingDto>.Failure(fulfilCheck.Code, fulfilCheck.Message);
                    var durationCheck = ValidateServiceDuration(listing.EstimatedDurationMinutes);
                    if (!durationCheck.IsSuccess)
                        return Result<ListingDto>.Failure(durationCheck.Code, durationCheck.Message);
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

                // Externally-managed listings (synced from an approved external
                // catalog source, e.g. ZansiTech) are never editable through the
                // ordinary seller path — the source system is authoritative for
                // these rows and its next sync always overwrites them anyway.
                // Blocking here avoids the confusing alternative: a seller edit
                // that appears to save, then silently vanishes on the next sync.
                if (!string.IsNullOrEmpty(listing.ExternalSourceCode))
                    return Result<ListingDto>.Failure(
                        ErrorCodes.Forbidden,
                        $"This listing is managed by an external catalog ('{listing.ExternalSourceCode}') and can't be edited here. Changes must be made in the source system.");

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

                    // Parcel merge — null fields leave existing values unchanged.
                    listing.PackageSizeCategory = request.PackageSizeCategory ?? listing.PackageSizeCategory;
                    listing.PackageWeightKg = request.PackageWeightKg ?? listing.PackageWeightKg;
                    listing.PackageLengthCm = request.PackageLengthCm ?? listing.PackageLengthCm;
                    listing.PackageWidthCm = request.PackageWidthCm ?? listing.PackageWidthCm;
                    listing.PackageHeightCm = request.PackageHeightCm ?? listing.PackageHeightCm;
                    listing.PackageFragile = request.PackageFragile ?? listing.PackageFragile;
                    listing.PackageContentsDescription =
                        request.PackageContentsDescription?.Trim() ?? listing.PackageContentsDescription;

                    var parcelCheck = ValidateParcel(listing);
                    if (!parcelCheck.IsSuccess)
                        return Result<ListingDto>.Failure(parcelCheck.Code, parcelCheck.Message);
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

                    // Fulfilment merge — null fields leave existing values
                    // unchanged (same pattern as ServiceArea/Turnaround above).
                    listing.FulfilmentMode = request.FulfilmentMode ?? listing.FulfilmentMode;
                    listing.AllowsHouseCall = request.AllowsHouseCall ?? listing.AllowsHouseCall;
                    listing.AllowsProviderLocation = request.AllowsProviderLocation ?? listing.AllowsProviderLocation;
                    listing.ProviderLocationName = request.ProviderLocationName?.Trim() ?? listing.ProviderLocationName;
                    listing.ProviderAddressLine1 = request.ProviderAddressLine1?.Trim() ?? listing.ProviderAddressLine1;
                    listing.ProviderAddressLine2 = request.ProviderAddressLine2?.Trim() ?? listing.ProviderAddressLine2;
                    listing.ProviderCity = request.ProviderCity?.Trim() ?? listing.ProviderCity;
                    listing.ProviderProvince = request.ProviderProvince?.Trim() ?? listing.ProviderProvince;
                    listing.ProviderPostalCode = request.ProviderPostalCode?.Trim() ?? listing.ProviderPostalCode;
                    listing.ProviderLatitude = request.ProviderLatitude ?? listing.ProviderLatitude;
                    listing.ProviderLongitude = request.ProviderLongitude ?? listing.ProviderLongitude;
                    listing.TravelFeeType = request.TravelFeeType ?? listing.TravelFeeType;
                    listing.TravelFeePerKm = request.TravelFeePerKm ?? listing.TravelFeePerKm;
                    listing.TravelFeeFlatAmount = request.TravelFeeFlatAmount ?? listing.TravelFeeFlatAmount;
                    listing.FreeTravelRadiusKm = request.FreeTravelRadiusKm ?? listing.FreeTravelRadiusKm;
                    listing.MaxTravelDistanceKm = request.MaxTravelDistanceKm ?? listing.MaxTravelDistanceKm;
                    listing.TravelFeeMinimum = request.TravelFeeMinimum ?? listing.TravelFeeMinimum;
                    listing.TravelFeeMaximum = request.TravelFeeMaximum ?? listing.TravelFeeMaximum;
                    listing.HouseCallSurchargeAmount =
                        request.HouseCallSurchargeAmount ?? listing.HouseCallSurchargeAmount;
                    listing.LeadTimeHours = request.LeadTimeHours ?? listing.LeadTimeHours;
                    listing.BufferMinutes = request.BufferMinutes ?? listing.BufferMinutes;
                    listing.EstimatedDurationMinutes =
                        request.EstimatedDurationMinutes ?? listing.EstimatedDurationMinutes;

                    var durationCheck = ValidateServiceDuration(listing.EstimatedDurationMinutes);
                    if (!durationCheck.IsSuccess)
                        return Result<ListingDto>.Failure(durationCheck.Code, durationCheck.Message);

                    NormalizeFulfilment(listing);
                    var fulfilCheck = ValidateServiceFulfilment(listing);
                    if (!fulfilCheck.IsSuccess)
                        return Result<ListingDto>.Failure(fulfilCheck.Code, fulfilCheck.Message);
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
                    // Surface the underlying DB reason (e.g. a CHECK/length/NOT-NULL
                    // violation) instead of a blind generic message — previously this
                    // swallowed the cause, so a save failure was an opaque 500 with no
                    // way to tell what the DB rejected. The base exception message is
                    // the SQL provider's detail. Logged AND returned so the client can
                    // show the seller (and us) exactly what failed.
                    var detail = ex.GetBaseException().Message;
                    _logger.LogError(ex,
                        "Database error updating listing {ListingId}. Detail={Detail}", listingId, detail);
                    return Result<ListingDto>.Failure(
                        ErrorCodes.Exception,
                        $"Could not save your listing: {detail}");
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

        // ── Product parcel profile ──────────────────────────────────────────

        /// <summary>
        /// Validate a product's delivery package details. Migration-safe: a
        /// product with NO parcel data at all passes (legacy rows / drafts /
        /// in-store-only items are never retro-blocked — the buyer checkout +
        /// create-from-quote guard gate courier delivery on completeness). But
        /// once the seller starts entering package details, the parcel must be
        /// COMPLETE and coherent: weight + all three dimensions positive and
        /// within sane bounds. No half-filled / token parcels.
        /// </summary>
        private static Result ValidateParcel(Listing l)
        {
            if (l.Type != ListingType.Product)
                return Result.Success();

            var anyProvided =
                l.PackageSizeCategory.HasValue
                || l.PackageWeightKg.HasValue
                || l.PackageLengthCm.HasValue
                || l.PackageWidthCm.HasValue
                || l.PackageHeightCm.HasValue
                || (l.PackageFragile ?? false)
                || !string.IsNullOrWhiteSpace(l.PackageContentsDescription);

            if (!anyProvided)
                return Result.Success();

            if (!(l.PackageWeightKg is > 0m)
                || !(l.PackageLengthCm is > 0m)
                || !(l.PackageWidthCm is > 0m)
                || !(l.PackageHeightCm is > 0m))
                return Result.Failure(ErrorCodes.BadRequest,
                    "Package weight and all dimensions (length, width, height) are required for delivery.");

            // Sanity caps — guard against fat-fingered values that would break
            // courier rating (e.g. cm entered as mm, kg as grams).
            if (l.PackageWeightKg > 1000m)
                return Result.Failure(ErrorCodes.BadRequest, "Package weight looks too large (max 1000 kg).");
            if (l.PackageLengthCm > 500m || l.PackageWidthCm > 500m || l.PackageHeightCm > 500m)
                return Result.Failure(ErrorCodes.BadRequest, "Package dimensions look too large (max 500 cm per side).");

            return Result.Success();
        }

        private static ListingParcelDto? BuildParcelDto(Listing l)
        {
            if (l.Type != ListingType.Product)
                return null;

            var anyProvided =
                l.PackageSizeCategory.HasValue
                || l.PackageWeightKg.HasValue
                || l.PackageLengthCm.HasValue
                || l.PackageWidthCm.HasValue
                || l.PackageHeightCm.HasValue
                || (l.PackageFragile ?? false)
                || !string.IsNullOrWhiteSpace(l.PackageContentsDescription);

            if (!anyProvided)
                return null;

            return new ListingParcelDto
            {
                SizeCategory = l.PackageSizeCategory,
                WeightKg = l.PackageWeightKg,
                LengthCm = l.PackageLengthCm,
                WidthCm = l.PackageWidthCm,
                HeightCm = l.PackageHeightCm,
                Fragile = l.PackageFragile ?? false,
                ContentsDescription = l.PackageContentsDescription,
                IsComplete = l.IsParcelComplete,
            };
        }

        // ── Service fulfilment ──────────────────────────────────────────────

        /// <summary>
        /// Fill in the Allows* flags + travel-fee type from the mode when the
        /// seller left them implicit, so the stored entity (and the DTO) are
        /// internally consistent regardless of which fields the client sent.
        /// </summary>
        private static void NormalizeFulfilment(Listing listing)
        {
            if (listing.Type != ListingType.Service || listing.FulfilmentMode is null)
                return;

            var mode = listing.FulfilmentMode.Value;
            listing.AllowsHouseCall ??=
                mode is ServiceFulfilmentMode.HouseCallOnly or ServiceFulfilmentMode.Both;
            listing.AllowsProviderLocation ??=
                mode is ServiceFulfilmentMode.ProviderLocationOnly or ServiceFulfilmentMode.Both;
            listing.TravelFeeType ??= ServiceTravelFeeType.None;
        }

        /// <summary>
        /// Validate a service listing's EFFECTIVE fulfilment state (after
        /// normalisation). Migration-safe: when <c>FulfilmentMode</c> is null
        /// (existing rows / not configured yet) this is a no-op — existing data
        /// is never retro-blocked. New/updated configs must be coherent.
        /// </summary>
        /// <summary>
        /// Service booking duration must be 15 minutes … 12 hours when supplied.
        /// Null is allowed (legacy services fall back to 60 at booking time).
        /// Applies to services only — products never set this.
        /// </summary>
        private static Result ValidateServiceDuration(int? minutes)
        {
            if (minutes is int m && (m < 15 || m > 720))
                return Result.Failure(
                    ErrorCodes.BadRequest,
                    "Service duration must be between 15 minutes and 12 hours.");
            return Result.Success();
        }

        private static Result ValidateServiceFulfilment(Listing l)
        {
            if (l.Type != ListingType.Service || l.FulfilmentMode is null)
                return Result.Success();

            var mode = l.FulfilmentMode.Value;
            var house = l.AllowsHouseCall ?? false;
            var provider = l.AllowsProviderLocation ?? false;

            // Mode/flags consistency — block impossible configs.
            switch (mode)
            {
                case ServiceFulfilmentMode.ProviderLocationOnly:
                    if (!provider || house)
                        return Result.Failure(ErrorCodes.BadRequest,
                            "Provider-location-only services must enable provider location and disable house calls.");
                    break;
                case ServiceFulfilmentMode.HouseCallOnly:
                    if (!house || provider)
                        return Result.Failure(ErrorCodes.BadRequest,
                            "House-call-only services must enable house calls and disable provider location.");
                    break;
                case ServiceFulfilmentMode.Both:
                    if (!house || !provider)
                        return Result.Failure(ErrorCodes.BadRequest,
                            "Services offered as both must enable house calls and provider location.");
                    break;
                default:
                    return Result.Failure(ErrorCodes.BadRequest, "Unknown service fulfilment mode.");
            }

            var hasProviderLocation =
                !string.IsNullOrWhiteSpace(l.ProviderLocationName) ||
                !string.IsNullOrWhiteSpace(l.ProviderAddressLine1) ||
                !string.IsNullOrWhiteSpace(l.ProviderCity) ||
                (l.ProviderLatitude.HasValue && l.ProviderLongitude.HasValue);

            // Provider location required when buyers can visit.
            if (provider && !hasProviderLocation)
                return Result.Failure(ErrorCodes.BadRequest,
                    "Provider location details are required when buyers can visit your location.");

            if (house)
            {
                // A base location is needed so travel/distance can be computed.
                if (!hasProviderLocation)
                    return Result.Failure(ErrorCodes.BadRequest,
                        "A provider base location is required for house-call services so travel can be calculated.");

                var feeType = l.TravelFeeType ?? ServiceTravelFeeType.None;
                if (feeType == ServiceTravelFeeType.PerKilometre &&
                    !(l.TravelFeePerKm.HasValue && l.TravelFeePerKm.Value > 0))
                {
                    return Result.Failure(ErrorCodes.BadRequest,
                        "A per-kilometre travel rate greater than zero is required for per-km travel fees.");
                }
                if (feeType == ServiceTravelFeeType.FlatFee &&
                    (l.TravelFeeFlatAmount is null || l.TravelFeeFlatAmount < 0))
                {
                    return Result.Failure(ErrorCodes.BadRequest,
                        "A flat travel fee of zero or more is required for flat travel fees.");
                }
                if (l.MaxTravelDistanceKm is <= 0)
                    return Result.Failure(ErrorCodes.BadRequest,
                        "Max travel distance must be greater than zero.");
                if (l.FreeTravelRadiusKm is < 0)
                    return Result.Failure(ErrorCodes.BadRequest,
                        "Free travel radius cannot be negative.");

                // House-call surcharge: optional, but when set it must be 0 (no
                // extra) or at least the R20 product minimum — no token amounts.
                if (l.HouseCallSurchargeAmount is < 0)
                    return Result.Failure(ErrorCodes.BadRequest,
                        "House-call extra cannot be negative.");
                if (l.HouseCallSurchargeAmount is > 0 and < MinHouseCallSurcharge)
                    return Result.Failure(ErrorCodes.BadRequest,
                        $"House-call extra must be at least R{MinHouseCallSurcharge:0} (or 0 for no extra).");
            }
            else
            {
                // Surcharge only applies to house-call services — clear any stray
                // value on a provider-location-only listing so it can't be charged.
                l.HouseCallSurchargeAmount = null;
            }

            return Result.Success();
        }

        /// <summary>
        /// Build the fulfilment DTO from a service listing. Null for products
        /// and for services the seller hasn't configured (<c>FulfilmentMode</c>
        /// null) — the mobile flow then uses its safe fallback.
        /// </summary>
        private static ServiceFulfilmentDto? BuildFulfilmentDto(Listing l)
        {
            if (l.Type != ListingType.Service || l.FulfilmentMode is null)
                return null;

            var mode = l.FulfilmentMode.Value;
            var house = l.AllowsHouseCall ??
                (mode is ServiceFulfilmentMode.HouseCallOnly or ServiceFulfilmentMode.Both);
            var provider = l.AllowsProviderLocation ??
                (mode is ServiceFulfilmentMode.ProviderLocationOnly or ServiceFulfilmentMode.Both);

            var summary = string.Join(", ", new[]
            {
                l.ProviderLocationName,
                l.ProviderAddressLine1,
                l.ProviderCity,
                l.ProviderProvince,
            }.Where(s => !string.IsNullOrWhiteSpace(s)));

            var hasAnyLocation =
                !string.IsNullOrWhiteSpace(summary) ||
                (l.ProviderLatitude.HasValue && l.ProviderLongitude.HasValue);

            return new ServiceFulfilmentDto
            {
                Mode = mode,
                AllowsHouseCall = house,
                AllowsProviderLocation = provider,
                ProviderLocation = hasAnyLocation
                    ? new ServiceProviderLocationDto
                    {
                        Name = l.ProviderLocationName,
                        Summary = string.IsNullOrWhiteSpace(summary) ? null : summary,
                        AddressLine1 = l.ProviderAddressLine1,
                        AddressLine2 = l.ProviderAddressLine2,
                        City = l.ProviderCity,
                        Province = l.ProviderProvince,
                        PostalCode = l.ProviderPostalCode,
                        Latitude = l.ProviderLatitude,
                        Longitude = l.ProviderLongitude,
                    }
                    : null,
                TravelFeeType = l.TravelFeeType ?? ServiceTravelFeeType.None,
                TravelFeePerKm = l.TravelFeePerKm,
                TravelFeeFlatAmount = l.TravelFeeFlatAmount,
                FreeTravelRadiusKm = l.FreeTravelRadiusKm,
                MaxTravelDistanceKm = l.MaxTravelDistanceKm,
                TravelFeeMinimum = l.TravelFeeMinimum,
                TravelFeeMaximum = l.TravelFeeMaximum,
                HouseCallSurchargeAmount = l.HouseCallSurchargeAmount,
                LeadTimeHours = l.LeadTimeHours,
                BufferMinutes = l.BufferMinutes,
                EstimatedDurationMinutes = l.EstimatedDurationMinutes,
            };
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

        /// <summary>Product minimum for a house-call surcharge when the seller
        /// sets one (ZAR). 0 (no extra) is also allowed; anything in between is
        /// rejected so sellers can't add a token amount.</summary>
        private const decimal MinHouseCallSurcharge = 20m;

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
                ShopLogoUrl = listing.ShopProfile?.LogoUrl,
                MerchantId = listing.MerchantId,
                MerchantName = listing.Merchant?.Name,
                MerchantSlug = listing.Merchant?.Slug,
                MerchantLogoUrl = listing.Merchant?.LogoUrl,
                MerchantProfileImageUrl = listing.Merchant?.ProfileImageUrl,
                IsOwner = ComputeIsOwner(listing),
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
                Parcel = BuildParcelDto(listing),
                PricingModel = listing.PricingModel,
                ServiceArea = listing.ServiceArea,
                Turnaround = listing.Turnaround,
                Availability = listing.Availability,
                BookingMethods = listing.BookingMethods,
                Fulfilment = BuildFulfilmentDto(listing),
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
                ShopLogoUrl = listing.ShopProfile?.LogoUrl,
                MerchantId = listing.MerchantId,
                MerchantName = listing.Merchant?.Name,
                MerchantSlug = listing.Merchant?.Slug,
                MerchantLogoUrl = listing.Merchant?.LogoUrl,
                MerchantProfileImageUrl = listing.Merchant?.ProfileImageUrl,
                MerchantVerified = listing.Merchant != null
                    && listing.Merchant.KycStatus == ZansiHustle.Shared.Enums.Merchants.MerchantKycStatus.Verified,
                IsOwner = ComputeIsOwner(listing),
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
