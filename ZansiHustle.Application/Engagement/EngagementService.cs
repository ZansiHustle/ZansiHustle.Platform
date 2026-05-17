using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ZansiHustle.Application.Engagement.Dtos;
using ZansiHustle.Application.Listings.Dtos;
using ZansiHustle.Application.Marketplace.Dtos;
using ZansiHustle.Application.Persistence.Engagement;
using ZansiHustle.Application.Persistence.Listings;
using ZansiHustle.Application.Persistence.Marketplace;
using ZansiHustle.Application.Persistence.Merchants;
using ZansiHustle.Application.Persistence.Shops;
using ZansiHustle.Domain.Listings;
using ZansiHustle.Domain.Marketplace;
using ZansiHustle.Domain.Merchants;
using ZansiHustle.Domain.Shops;
using ZansiHustle.Shared.Enums.Listings;
using ZansiHustle.Shared.Enums.Marketplace;
using ZansiHustle.Shared.Enums.Merchants;
using ZansiHustle.Shared.Enums.Shops;
using ZansiHustle.Shared.Errors;
using ZansiHustle.Shared.Results;

namespace ZansiHustle.Application.Engagement
{
    /// <inheritdoc cref="IEngagementService"/>
    public sealed class EngagementService : IEngagementService
    {
        private readonly IEngagementRepository _engagement;
        private readonly IListingRepository _listings;
        private readonly IMarketplaceListingRepository _marketplace;
        private readonly IShopProfileRepository _shops;
        private readonly IMerchantRepository _merchants;
        private readonly ILogger<EngagementService> _logger;

        public EngagementService(
            IEngagementRepository engagement,
            IListingRepository listings,
            IMarketplaceListingRepository marketplace,
            IShopProfileRepository shops,
            IMerchantRepository merchants,
            ILogger<EngagementService> logger)
        {
            _engagement = engagement;
            _listings = listings;
            _marketplace = marketplace;
            _shops = shops;
            _merchants = merchants;
            _logger = logger;
        }

        // ─────────────────────────────────────────────────────────────────
        // Listing likes
        // ─────────────────────────────────────────────────────────────────

        public async Task<Result<EngagementToggleResultDto>> LikeListingAsync(Guid userId, Guid listingId, CancellationToken ct = default)
        {
            try
            {
                var listing = await _listings.GetByIdAsync(listingId);
                if (listing is null)
                    return Result<EngagementToggleResultDto>.Failure(ErrorCodes.NotFound, "Listing not found.");

                if (listing.Status != ListingStatus.Active)
                    return Result<EngagementToggleResultDto>.Failure(ErrorCodes.NotFound, "Listing not found.");

                // Owner check via the listing's merchant. We use the repo
                // to avoid loading a Merchant-include on every read of the
                // listing — listings come back without their Merchant nav
                // populated unless the caller asks for it.
                var merchant = await _merchants.GetByIdAsync(listing.MerchantId);
                if (merchant is null)
                    return Result<EngagementToggleResultDto>.Failure(ErrorCodes.NotFound, "Listing not found.");
                if (merchant.OwnerUserId == userId)
                    return Result<EngagementToggleResultDto>.Failure(ErrorCodes.Forbidden, "You can't like your own listing.");

                await _engagement.AddListingLikeAsync(userId, listingId, ct);

                // Re-read so the response carries the up-to-date count.
                var refreshed = await _listings.GetByIdAsync(listingId);
                return Result<EngagementToggleResultDto>.Success(new EngagementToggleResultDto
                {
                    Active = true,
                    Count = refreshed?.LikeCount ?? 0,
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Like listing failed. UserId={UserId} ListingId={ListingId}", userId, listingId);
                return Result<EngagementToggleResultDto>.Failure(ErrorCodes.Exception, "Could not like the listing.");
            }
        }

        public async Task<Result<EngagementToggleResultDto>> UnlikeListingAsync(Guid userId, Guid listingId, CancellationToken ct = default)
        {
            try
            {
                // Unlike doesn't require the target to be Active — a user
                // should be able to remove a stale like from a delisted
                // listing without us re-checking visibility. Just look up
                // the listing for its current count; allow if it exists
                // at all.
                var listing = await _listings.GetByIdAsync(listingId);
                if (listing is null)
                    return Result<EngagementToggleResultDto>.Failure(ErrorCodes.NotFound, "Listing not found.");

                await _engagement.RemoveListingLikeAsync(userId, listingId, ct);

                var refreshed = await _listings.GetByIdAsync(listingId);
                return Result<EngagementToggleResultDto>.Success(new EngagementToggleResultDto
                {
                    Active = false,
                    Count = refreshed?.LikeCount ?? 0,
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unlike listing failed. UserId={UserId} ListingId={ListingId}", userId, listingId);
                return Result<EngagementToggleResultDto>.Failure(ErrorCodes.Exception, "Could not unlike the listing.");
            }
        }

        public async Task<Result<List<ListingListItemDto>>> GetMyLikedListingsAsync(Guid userId, int page, int pageSize, CancellationToken ct = default)
        {
            try
            {
                var entities = await _engagement.GetMyLikedListingsAsync(userId, page, pageSize, ct);
                var dtos = entities.Select(MapListingToListItem).ToList();
                return Result<List<ListingListItemDto>>.Success(dtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Get liked listings failed. UserId={UserId}", userId);
                return Result<List<ListingListItemDto>>.Failure(ErrorCodes.Exception, "Could not load liked listings.");
            }
        }

        // ─────────────────────────────────────────────────────────────────
        // Marketplace likes
        // ─────────────────────────────────────────────────────────────────

        public async Task<Result<EngagementToggleResultDto>> LikeMarketplaceListingAsync(Guid userId, Guid marketplaceListingId, CancellationToken ct = default)
        {
            try
            {
                var listing = await _marketplace.GetByIdAsync(marketplaceListingId);
                if (listing is null || listing.Status != MarketplaceListingStatus.Active)
                    return Result<EngagementToggleResultDto>.Failure(ErrorCodes.NotFound, "Listing not found.");

                if (listing.OwnerUserId == userId)
                    return Result<EngagementToggleResultDto>.Failure(ErrorCodes.Forbidden, "You can't like your own listing.");

                await _engagement.AddMarketplaceLikeAsync(userId, marketplaceListingId, ct);

                var refreshed = await _marketplace.GetByIdAsync(marketplaceListingId);
                return Result<EngagementToggleResultDto>.Success(new EngagementToggleResultDto
                {
                    Active = true,
                    Count = refreshed?.LikeCount ?? 0,
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Like marketplace listing failed. UserId={UserId} Id={Id}", userId, marketplaceListingId);
                return Result<EngagementToggleResultDto>.Failure(ErrorCodes.Exception, "Could not like the marketplace listing.");
            }
        }

        public async Task<Result<EngagementToggleResultDto>> UnlikeMarketplaceListingAsync(Guid userId, Guid marketplaceListingId, CancellationToken ct = default)
        {
            try
            {
                var listing = await _marketplace.GetByIdAsync(marketplaceListingId);
                if (listing is null)
                    return Result<EngagementToggleResultDto>.Failure(ErrorCodes.NotFound, "Listing not found.");

                await _engagement.RemoveMarketplaceLikeAsync(userId, marketplaceListingId, ct);

                var refreshed = await _marketplace.GetByIdAsync(marketplaceListingId);
                return Result<EngagementToggleResultDto>.Success(new EngagementToggleResultDto
                {
                    Active = false,
                    Count = refreshed?.LikeCount ?? 0,
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unlike marketplace listing failed. UserId={UserId} Id={Id}", userId, marketplaceListingId);
                return Result<EngagementToggleResultDto>.Failure(ErrorCodes.Exception, "Could not unlike the marketplace listing.");
            }
        }

        public async Task<Result<List<MarketplaceListingDto>>> GetMyLikedMarketplaceListingsAsync(Guid userId, int page, int pageSize, CancellationToken ct = default)
        {
            try
            {
                var entities = await _engagement.GetMyLikedMarketplaceAsync(userId, page, pageSize, ct);
                var dtos = entities.Select(MapMarketplaceListingToDto).ToList();
                return Result<List<MarketplaceListingDto>>.Success(dtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Get liked marketplace listings failed. UserId={UserId}", userId);
                return Result<List<MarketplaceListingDto>>.Failure(ErrorCodes.Exception, "Could not load liked marketplace listings.");
            }
        }

        // ─────────────────────────────────────────────────────────────────
        // Shop follows
        // ─────────────────────────────────────────────────────────────────

        public async Task<Result<EngagementToggleResultDto>> FollowShopAsync(Guid userId, Guid shopProfileId, CancellationToken ct = default)
        {
            try
            {
                var shop = await _shops.GetByIdAsync(shopProfileId);
                if (shop is null || shop.Status != ShopProfileStatus.Active)
                    return Result<EngagementToggleResultDto>.Failure(ErrorCodes.NotFound, "Shop not found.");

                // Owner check via shop.MerchantId → merchant.OwnerUserId.
                var merchant = await _merchants.GetByIdAsync(shop.MerchantId);
                if (merchant is not null && merchant.OwnerUserId == userId)
                    return Result<EngagementToggleResultDto>.Failure(ErrorCodes.Forbidden, "You can't follow your own shop.");

                await _engagement.AddShopFollowAsync(userId, shopProfileId, ct);

                var refreshed = await _shops.GetByIdAsync(shopProfileId);
                return Result<EngagementToggleResultDto>.Success(new EngagementToggleResultDto
                {
                    Active = true,
                    Count = refreshed?.FollowersCount ?? 0,
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Follow shop failed. UserId={UserId} ShopProfileId={Id}", userId, shopProfileId);
                return Result<EngagementToggleResultDto>.Failure(ErrorCodes.Exception, "Could not follow the shop.");
            }
        }

        public async Task<Result<EngagementToggleResultDto>> UnfollowShopAsync(Guid userId, Guid shopProfileId, CancellationToken ct = default)
        {
            try
            {
                var shop = await _shops.GetByIdAsync(shopProfileId);
                if (shop is null)
                    return Result<EngagementToggleResultDto>.Failure(ErrorCodes.NotFound, "Shop not found.");

                await _engagement.RemoveShopFollowAsync(userId, shopProfileId, ct);

                var refreshed = await _shops.GetByIdAsync(shopProfileId);
                return Result<EngagementToggleResultDto>.Success(new EngagementToggleResultDto
                {
                    Active = false,
                    Count = refreshed?.FollowersCount ?? 0,
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unfollow shop failed. UserId={UserId} ShopProfileId={Id}", userId, shopProfileId);
                return Result<EngagementToggleResultDto>.Failure(ErrorCodes.Exception, "Could not unfollow the shop.");
            }
        }

        public async Task<Result<List<FollowedShopDto>>> GetMyFollowedShopsAsync(Guid userId, int page, int pageSize, CancellationToken ct = default)
        {
            try
            {
                var entities = await _engagement.GetMyFollowedShopsAsync(userId, page, pageSize, ct);
                var dtos = entities.Select(MapShopToFollowedDto).ToList();
                return Result<List<FollowedShopDto>>.Success(dtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Get followed shops failed. UserId={UserId}", userId);
                return Result<List<FollowedShopDto>>.Failure(ErrorCodes.Exception, "Could not load followed shops.");
            }
        }

        // ─────────────────────────────────────────────────────────────────
        // Store saves
        // ─────────────────────────────────────────────────────────────────

        public async Task<Result<EngagementToggleResultDto>> SaveStoreAsync(Guid userId, Guid merchantId, CancellationToken ct = default)
        {
            try
            {
                var merchant = await _merchants.GetByIdAsync(merchantId);
                if (merchant is null)
                    return Result<EngagementToggleResultDto>.Failure(ErrorCodes.NotFound, "Store not found.");

                if (merchant.Type != MerchantType.PhysicalStore)
                    return Result<EngagementToggleResultDto>.Failure(ErrorCodes.BadRequest, "Only physical stores can be saved.");

                if (merchant.Status != MerchantStatus.Active)
                    return Result<EngagementToggleResultDto>.Failure(ErrorCodes.NotFound, "Store not found.");

                if (merchant.OwnerUserId == userId)
                    return Result<EngagementToggleResultDto>.Failure(ErrorCodes.Forbidden, "You can't save your own store.");

                await _engagement.AddStoreSaveAsync(userId, merchantId, ct);

                var refreshed = await _merchants.GetByIdAsync(merchantId);
                return Result<EngagementToggleResultDto>.Success(new EngagementToggleResultDto
                {
                    Active = true,
                    Count = refreshed?.SavesCount ?? 0,
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Save store failed. UserId={UserId} MerchantId={Id}", userId, merchantId);
                return Result<EngagementToggleResultDto>.Failure(ErrorCodes.Exception, "Could not save the store.");
            }
        }

        public async Task<Result<EngagementToggleResultDto>> UnsaveStoreAsync(Guid userId, Guid merchantId, CancellationToken ct = default)
        {
            try
            {
                var merchant = await _merchants.GetByIdAsync(merchantId);
                if (merchant is null)
                    return Result<EngagementToggleResultDto>.Failure(ErrorCodes.NotFound, "Store not found.");

                await _engagement.RemoveStoreSaveAsync(userId, merchantId, ct);

                var refreshed = await _merchants.GetByIdAsync(merchantId);
                return Result<EngagementToggleResultDto>.Success(new EngagementToggleResultDto
                {
                    Active = false,
                    Count = refreshed?.SavesCount ?? 0,
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unsave store failed. UserId={UserId} MerchantId={Id}", userId, merchantId);
                return Result<EngagementToggleResultDto>.Failure(ErrorCodes.Exception, "Could not unsave the store.");
            }
        }

        public async Task<Result<List<SavedStoreDto>>> GetMySavedStoresAsync(Guid userId, int page, int pageSize, CancellationToken ct = default)
        {
            try
            {
                var entities = await _engagement.GetMySavedStoresAsync(userId, page, pageSize, ct);
                var dtos = entities
                    .Where(m => m.Type == MerchantType.PhysicalStore)
                    .Select(MapMerchantToSavedStoreDto)
                    .ToList();
                return Result<List<SavedStoreDto>>.Success(dtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Get saved stores failed. UserId={UserId}", userId);
                return Result<List<SavedStoreDto>>.Failure(ErrorCodes.Exception, "Could not load saved stores.");
            }
        }

        // ─────────────────────────────────────────────────────────────────
        // Mapping helpers — kept inline because the engagement service is
        // the only consumer. If a third surface starts needing these we
        // can promote to a shared mapper.
        // ─────────────────────────────────────────────────────────────────

        private static ListingListItemDto MapListingToListItem(Listing l) => new()
        {
            Id = l.Id,
            Slug = l.Slug,
            Type = l.Type,
            Status = l.Status,
            AvailabilityMode = l.AvailabilityMode,
            ListingSource = l.ListingSource,
            ShopProfileId = l.ShopProfileId,
            ShopProfileName = l.ShopProfile?.Name,
            MerchantId = l.MerchantId,
            MerchantName = l.Merchant?.Name,
            MerchantSlug = l.Merchant?.Slug,
            Title = l.Title,
            Price = l.Price,
            Currency = l.Currency,
            SellerCategoryId = l.SellerCategoryId,
            // SellerCategoryName left null — engagement list doesn't
            // include the category nav. Cards degrade gracefully.
            Province = l.Province,
            City = l.City,
            Images = l.Images,
            IsFeatured = l.IsFeatured,
            IsBoosted = l.IsBoosted,
            Rating = l.Rating,
            ReviewCount = l.ReviewCount,
            LikeCount = l.LikeCount,
            IsLikedByMe = true,
            Stock = l.Stock,
            Condition = l.Condition,
            PricingModel = l.PricingModel,
            VariantCount = 0,
            CreatedAtUtc = l.CreatedAtUtc,
        };

        private static MarketplaceListingDto MapMarketplaceListingToDto(MarketplaceListing l) => new()
        {
            Id = l.Id,
            Title = l.Title,
            Description = l.Description,
            Price = l.Price,
            Currency = l.Currency,
            Category = l.Category,
            Condition = l.Condition,
            Images = l.Images
                .OrderBy(i => i.SortOrder)
                .Select(i => i.Url)
                .ToList(),
            Province = l.Province,
            Location = l.Location,
            AllowOffers = l.AllowOffers,
            SellerName = string.Empty, // Filled by the marketplace service in normal flows; engagement list omits seller join.
            SellerVerified = false,
            SellerUserId = l.OwnerUserId,
            IsBoosted = l.IsBoosted,
            IsFeatured = l.IsFeatured,
            Status = l.Status,
            LikeCount = l.LikeCount,
            IsLikedByMe = true,
            CreatedAtUtc = l.CreatedAtUtc,
            UpdatedAtUtc = l.UpdatedAtUtc,
        };

        private static FollowedShopDto MapShopToFollowedDto(ShopProfile s) => new()
        {
            Id = s.Id,
            MerchantId = s.MerchantId,
            Slug = s.Slug,
            Name = s.Name,
            Description = s.Description,
            LogoUrl = s.LogoUrl,
            BannerUrl = s.BannerUrl,
            Province = s.Province,
            City = s.City,
            Rating = s.Rating,
            ReviewCount = s.ReviewCount,
            FollowersCount = s.FollowersCount,
            IsFollowedByMe = true,
            FollowedAtUtc = s.CreatedAtUtc, // approximation — see MapMerchantToSavedStoreDto note
        };

        private static SavedStoreDto MapMerchantToSavedStoreDto(Merchant m) => new()
        {
            Id = m.Id,
            Slug = m.Slug,
            Name = m.Name,
            Description = m.Description,
            Type = m.Type,
            Category = m.SellerCategory?.Name,
            Province = m.Province,
            City = m.City,
            Suburb = m.Suburb,
            FormattedAddress = m.FormattedAddress,
            Latitude = m.Latitude,
            Longitude = m.Longitude,
            LogoUrl = m.LogoUrl,
            BannerUrl = m.BannerUrl,
            Rating = m.Rating,
            ReviewCount = m.ReviewCount,
            SavesCount = m.SavesCount,
            IsSavedByMe = true,
            // NOTE: the SAVE timestamp lives on StoreSave.CreatedAtUtc;
            // we'd need a join to surface it. For v1 we surface the
            // merchant's created date — close enough to power list
            // ordering (we already order by StoreSave.CreatedAtUtc at
            // the repo level) but the timestamp itself is approximate.
            // Promote to a real value when the Saved screen needs it.
            SavedAtUtc = m.CreatedAtUtc,
        };
    }
}
