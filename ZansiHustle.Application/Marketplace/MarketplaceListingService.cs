using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using ZansiHustle.Application.Common.Paging;
using ZansiHustle.Application.Marketplace.Dtos;
using ZansiHustle.Application.Persistence.Marketplace;
using ZansiHustle.Domain.Identity;
using ZansiHustle.Domain.Marketplace;
using ZansiHustle.Shared.Enums.Marketplace;
using ZansiHustle.Shared.Errors;
using ZansiHustle.Shared.Results;

namespace ZansiHustle.Application.Marketplace
{
    /// <inheritdoc />
    public sealed class MarketplaceListingService : IMarketplaceListingService
    {
        // Hard caps to keep abusive payloads off the database. The
        // values mirror the EF column lengths set in
        // MarketplaceListingConfiguration so EF never has to truncate.
        private const int MaxTitleLen = 100;
        private const int MaxDescriptionLen = 4000;
        private const int MaxCategoryLen = 60;
        private const int MaxProvinceLen = 60;
        private const int MaxLocationLen = 120;
        private const int MaxImageUrlLen = 1000;

        private readonly IMarketplaceListingRepository _repository;
        private readonly UserManager<User> _userManager;
        private readonly ILogger<MarketplaceListingService> _logger;

        public MarketplaceListingService(
            IMarketplaceListingRepository repository,
            UserManager<User> userManager,
            ILogger<MarketplaceListingService> logger)
        {
            _repository = repository;
            _userManager = userManager;
            _logger = logger;
        }

        // ─── Search ──────────────────────────────────────────────────────────

        public async Task<Result<PagedResult<MarketplaceListingDto>>> SearchAsync(MarketplaceListingFilterRequestDto filter)
        {
            try
            {
                filter ??= new MarketplaceListingFilterRequestDto();

                // Public search defaults to Active-only when no Status filter is sent.
                // Sold / archived listings shouldn't appear in the public feed.
                if (!filter.Status.HasValue)
                    filter.Status = MarketplaceListingStatus.Active;

                var (items, total) = await _repository.SearchAsync(filter);

                var paged = new PagedResult<MarketplaceListingDto>
                {
                    Items = items.Select(MapDto).ToList(),
                    Total = total,
                    Page = filter.Page <= 0 ? 1 : filter.Page,
                    PageSize = filter.PageSize <= 0 ? 20 : filter.PageSize,
                };

                return Result<PagedResult<MarketplaceListingDto>>.Success(paged, "Marketplace listings retrieved.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Marketplace search failed.");
                return Result<PagedResult<MarketplaceListingDto>>.Failure(ErrorCodes.Exception, "Failed to search marketplace listings.");
            }
        }

        // ─── Get by id ───────────────────────────────────────────────────────

        public async Task<Result<MarketplaceListingDto>> GetByIdAsync(Guid id)
        {
            try
            {
                var entity = await _repository.GetByIdAsync(id);
                if (entity is null)
                    return Result<MarketplaceListingDto>.Failure(ErrorCodes.NotFound, "Listing not found.");

                return Result<MarketplaceListingDto>.Success(MapDto(entity), "Listing retrieved.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Marketplace GetById failed for {Id}.", id);
                return Result<MarketplaceListingDto>.Failure(ErrorCodes.Exception, "Failed to retrieve listing.");
            }
        }

        // ─── Get mine ────────────────────────────────────────────────────────

        public async Task<Result<List<MarketplaceListingDto>>> GetMineAsync(Guid ownerUserId)
        {
            try
            {
                var items = await _repository.GetByOwnerAsync(ownerUserId);
                return Result<List<MarketplaceListingDto>>.Success(items.Select(MapDto).ToList(), "My listings retrieved.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Marketplace GetMine failed for owner {OwnerId}.", ownerUserId);
                return Result<List<MarketplaceListingDto>>.Failure(ErrorCodes.Exception, "Failed to retrieve listings.");
            }
        }

        // ─── Create ──────────────────────────────────────────────────────────

        public async Task<Result<MarketplaceListingDto>> CreateAsync(Guid ownerUserId, CreateMarketplaceListingRequestDto request)
        {
            try
            {
                if (request is null)
                    return Result<MarketplaceListingDto>.Failure(ErrorCodes.BadRequest, "Request body is required.");

                var validation = ValidateCreate(request);
                if (!validation.IsSuccess)
                    return Result<MarketplaceListingDto>.Failure(validation.Code, validation.Message);

                var owner = await _userManager.FindByIdAsync(ownerUserId.ToString());
                if (owner is null)
                    return Result<MarketplaceListingDto>.Failure(ErrorCodes.Unauthorized, "Owner user not found.");

                var listing = new MarketplaceListing
                {
                    Id = Guid.NewGuid(),
                    OwnerUserId = ownerUserId,
                    Title = request.Title.Trim(),
                    Description = request.Description?.Trim() ?? string.Empty,
                    Price = request.Price,
                    Currency = "ZAR",
                    Category = request.Category.Trim(),
                    Condition = request.Condition,
                    Province = request.Province.Trim(),
                    Location = request.Location?.Trim() ?? string.Empty,
                    AllowOffers = request.AllowOffers,
                    Status = MarketplaceListingStatus.Active,
                    CreatedAtUtc = DateTime.UtcNow,
                };

                if (request.ImageUrls is { Count: > 0 })
                {
                    var sortOrder = 0;
                    foreach (var url in request.ImageUrls)
                    {
                        if (string.IsNullOrWhiteSpace(url)) continue;
                        var trimmed = url.Trim();
                        if (trimmed.Length > MaxImageUrlLen) continue;

                        listing.Images.Add(new MarketplaceListingImage
                        {
                            Id = Guid.NewGuid(),
                            ListingId = listing.Id,
                            Url = trimmed,
                            SortOrder = sortOrder++,
                            CreatedAtUtc = DateTime.UtcNow,
                        });
                    }
                }

                await _repository.AddAsync(listing);
                await _repository.SaveChangesAsync();

                // Re-load with the Owner nav populated for the response.
                var fresh = await _repository.GetByIdAsync(listing.Id) ?? listing;
                return Result<MarketplaceListingDto>.Success(MapDto(fresh), "Listing created.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Marketplace Create failed for owner {OwnerId}.", ownerUserId);
                return Result<MarketplaceListingDto>.Failure(ErrorCodes.Exception, "Failed to create listing.");
            }
        }

        // ─── Update status (Sold / Archived / Active) ────────────────────────

        public async Task<Result<MarketplaceListingDto>> UpdateStatusAsync(
            Guid ownerUserId,
            Guid listingId,
            UpdateMarketplaceListingStatusRequestDto request)
        {
            try
            {
                if (request is null)
                    return Result<MarketplaceListingDto>.Failure(ErrorCodes.BadRequest, "Request body is required.");

                var listing = await _repository.GetByIdAsync(listingId);
                if (listing is null)
                    return Result<MarketplaceListingDto>.Failure(ErrorCodes.NotFound, "Listing not found.");

                if (listing.OwnerUserId != ownerUserId)
                    return Result<MarketplaceListingDto>.Failure(ErrorCodes.Forbidden, "You do not have permission to modify this listing.");

                if (!Enum.IsDefined(typeof(MarketplaceListingStatus), request.Status))
                    return Result<MarketplaceListingDto>.Failure(ErrorCodes.BadRequest, "Status is invalid.");

                listing.Status = request.Status;
                listing.UpdatedAtUtc = DateTime.UtcNow;

                _repository.Update(listing);
                await _repository.SaveChangesAsync();

                return Result<MarketplaceListingDto>.Success(MapDto(listing), "Status updated.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Marketplace UpdateStatus failed for listing {ListingId}.", listingId);
                return Result<MarketplaceListingDto>.Failure(ErrorCodes.Exception, "Failed to update status.");
            }
        }

        // ─── Add image ───────────────────────────────────────────────────────

        public async Task<Result<MarketplaceListingDto>> AddImageAsync(
            Guid ownerUserId,
            Guid listingId,
            AddMarketplaceListingImageRequestDto request)
        {
            try
            {
                if (request is null || string.IsNullOrWhiteSpace(request.Url))
                    return Result<MarketplaceListingDto>.Failure(ErrorCodes.BadRequest, "Image URL is required.");

                var url = request.Url.Trim();
                if (url.Length > MaxImageUrlLen)
                    return Result<MarketplaceListingDto>.Failure(ErrorCodes.BadRequest, $"Image URL exceeds {MaxImageUrlLen} characters.");

                var listing = await _repository.GetByIdAsync(listingId);
                if (listing is null)
                    return Result<MarketplaceListingDto>.Failure(ErrorCodes.NotFound, "Listing not found.");

                if (listing.OwnerUserId != ownerUserId)
                    return Result<MarketplaceListingDto>.Failure(ErrorCodes.Forbidden, "You do not have permission to modify this listing.");

                var sortOrder = request.SortOrder
                                ?? (listing.Images.Count == 0 ? 0 : listing.Images.Max(i => i.SortOrder) + 1);

                var image = new MarketplaceListingImage
                {
                    Id = Guid.NewGuid(),
                    ListingId = listing.Id,
                    Url = url,
                    SortOrder = sortOrder,
                    CreatedAtUtc = DateTime.UtcNow,
                };

                await _repository.AddImageAsync(image);

                listing.UpdatedAtUtc = DateTime.UtcNow;
                _repository.Update(listing);

                await _repository.SaveChangesAsync();

                var fresh = await _repository.GetByIdAsync(listing.Id) ?? listing;
                return Result<MarketplaceListingDto>.Success(MapDto(fresh), "Image attached.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Marketplace AddImage failed for listing {ListingId}.", listingId);
                return Result<MarketplaceListingDto>.Failure(ErrorCodes.Exception, "Failed to attach image.");
            }
        }

        // ─── Helpers ─────────────────────────────────────────────────────────

        private static Result ValidateCreate(CreateMarketplaceListingRequestDto request)
        {
            if (string.IsNullOrWhiteSpace(request.Title))
                return Result.Failure(ErrorCodes.BadRequest, "Title is required.");
            if (request.Title.Length > MaxTitleLen)
                return Result.Failure(ErrorCodes.BadRequest, $"Title must be <= {MaxTitleLen} characters.");

            if (request.Description is { Length: > MaxDescriptionLen })
                return Result.Failure(ErrorCodes.BadRequest, $"Description must be <= {MaxDescriptionLen} characters.");

            if (request.Price < 0)
                return Result.Failure(ErrorCodes.BadRequest, "Price must be >= 0.");

            if (string.IsNullOrWhiteSpace(request.Category))
                return Result.Failure(ErrorCodes.BadRequest, "Category is required.");
            if (request.Category.Length > MaxCategoryLen)
                return Result.Failure(ErrorCodes.BadRequest, $"Category must be <= {MaxCategoryLen} characters.");

            if (!Enum.IsDefined(typeof(ProductCondition), request.Condition))
                return Result.Failure(ErrorCodes.BadRequest, "Condition is invalid.");

            if (string.IsNullOrWhiteSpace(request.Province))
                return Result.Failure(ErrorCodes.BadRequest, "Province is required.");
            if (request.Province.Length > MaxProvinceLen)
                return Result.Failure(ErrorCodes.BadRequest, $"Province must be <= {MaxProvinceLen} characters.");

            if (request.Location is { Length: > MaxLocationLen })
                return Result.Failure(ErrorCodes.BadRequest, $"Location must be <= {MaxLocationLen} characters.");

            return Result.Success();
        }

        private static MarketplaceListingDto MapDto(MarketplaceListing entity)
        {
            return new MarketplaceListingDto
            {
                Id = entity.Id,
                Title = entity.Title,
                Description = entity.Description,
                Price = entity.Price,
                Currency = entity.Currency,
                Category = entity.Category,
                Condition = entity.Condition,
                Images = entity.Images
                    .OrderBy(i => i.SortOrder)
                    .ThenBy(i => i.CreatedAtUtc)
                    .Select(i => i.Url)
                    .ToList(),
                Province = entity.Province,
                Location = entity.Location,
                AllowOffers = entity.AllowOffers,
                SellerName = ComposeSellerName(entity.Owner),
                // Email-confirmed is the only "verified" signal Identity
                // gives us today. When a richer verification system
                // ships, swap this for that flag.
                SellerVerified = entity.Owner?.EmailConfirmed ?? false,
                SellerUserId = entity.OwnerUserId,
                IsBoosted = entity.IsBoosted,
                IsFeatured = entity.IsFeatured,
                Status = entity.Status,
                CreatedAtUtc = entity.CreatedAtUtc,
                UpdatedAtUtc = entity.UpdatedAtUtc,
            };
        }

        private static string ComposeSellerName(User? owner)
        {
            if (owner is null) return string.Empty;
            var first = (owner.FirstName ?? string.Empty).Trim();
            var last = (owner.LastName ?? string.Empty).Trim();
            var combined = string.Join(' ', new[] { first, last }.Where(s => !string.IsNullOrEmpty(s)));
            if (!string.IsNullOrEmpty(combined)) return combined;
            return owner.UserName ?? string.Empty;
        }
    }
}
