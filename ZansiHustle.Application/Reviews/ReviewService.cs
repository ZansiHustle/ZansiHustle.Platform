using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ZansiHustle.Application.Chat;
using ZansiHustle.Application.Persistence.Marketplace;
using ZansiHustle.Application.Persistence.Merchants;
using ZansiHustle.Application.Persistence.Reviews;
using ZansiHustle.Application.Persistence.Shops;
using ZansiHustle.Application.Reviews.Dtos;
using ZansiHustle.Domain.Reviews;
using ZansiHustle.Shared.Enums.Merchants;
using ZansiHustle.Shared.Enums.Reviews;
using ZansiHustle.Shared.Enums.Shops;
using ZansiHustle.Shared.Errors;
using ZansiHustle.Shared.Results;

namespace ZansiHustle.Application.Reviews
{
    /// <summary>
    /// Application-layer review service. Encapsulates:
    ///   • Per-target validation (Store: PhysicalStore + Active +
    ///     not-owned-by-caller).
    ///   • One-active-review-per-(reviewer, target) rule.
    ///   • Aggregate updates on Merchant.Rating / ReviewCount when a
    ///     Store review is created / edited / soft-deleted, so existing
    ///     consumers of the merchant DTO keep rendering correctly.
    ///   • DTO projection — uses the repository's batched user join
    ///     to build the reviewer display name + avatar.
    /// </summary>
    public class ReviewService : IReviewService
    {
        private readonly IReviewRepository _reviews;
        private readonly IMerchantRepository _merchants;
        private readonly IShopProfileRepository _shops;
        private readonly IMarketplaceListingRepository _marketplaceListings;
        private readonly IChatService _chat;
        private readonly ILogger<ReviewService> _logger;

        public ReviewService(
            IReviewRepository reviews,
            IMerchantRepository merchants,
            IShopProfileRepository shops,
            IMarketplaceListingRepository marketplaceListings,
            IChatService chat,
            ILogger<ReviewService> logger)
        {
            _reviews = reviews;
            _merchants = merchants;
            _shops = shops;
            _marketplaceListings = marketplaceListings;
            _chat = chat;
            _logger = logger;
        }

        // ── Reads ────────────────────────────────────────────────────

        /// <inheritdoc />
        public async Task<Result<List<ReviewDto>>> ListAsync(
            ReviewTargetType targetType, Guid targetId, Guid? currentUserId)
        {
            try
            {
                var rows = await _reviews.GetActiveForTargetAsync(targetType, targetId);
                if (rows.Count == 0)
                    return Result<List<ReviewDto>>.Success(new List<ReviewDto>(), "No reviews yet.");

                var displayMap = await _reviews.GetReviewerDisplaysAsync(rows.Select(r => r.ReviewerUserId));
                var dtos = rows.Select(r => MapToDto(r, displayMap, currentUserId)).ToList();
                return Result<List<ReviewDto>>.Success(dtos, "Reviews retrieved successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Reviews] List failed for {TargetType}/{TargetId}", targetType, targetId);
                return Result<List<ReviewDto>>.Failure(ErrorCodes.Exception, $"Failed to load reviews. {ex.Message}");
            }
        }

        /// <inheritdoc />
        public async Task<Result<ReviewSummaryDto>> GetSummaryAsync(
            ReviewTargetType targetType, Guid targetId, Guid? currentUserId)
        {
            try
            {
                var (avg, count) = await _reviews.GetSummaryAsync(targetType, targetId);

                ReviewDto? mine = null;
                if (currentUserId.HasValue)
                {
                    var myRow = await _reviews.GetActiveByReviewerAsync(targetType, targetId, currentUserId.Value);
                    if (myRow != null)
                    {
                        var displayMap = await _reviews.GetReviewerDisplaysAsync(new[] { myRow.ReviewerUserId });
                        mine = MapToDto(myRow, displayMap, currentUserId);
                    }
                }

                return Result<ReviewSummaryDto>.Success(new ReviewSummaryDto
                {
                    TargetType = targetType,
                    TargetId = targetId,
                    AverageRating = avg,
                    ReviewCount = count,
                    MyReview = mine,
                }, "Review summary retrieved successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Reviews] Summary failed for {TargetType}/{TargetId}", targetType, targetId);
                return Result<ReviewSummaryDto>.Failure(ErrorCodes.Exception, $"Failed to load review summary. {ex.Message}");
            }
        }

        // ── Writes ───────────────────────────────────────────────────

        /// <inheritdoc />
        public async Task<Result<ReviewDto>> CreateAsync(Guid currentUserId, CreateReviewRequestDto request)
        {
            try
            {
                if (request is null)
                    return Result<ReviewDto>.Failure(ErrorCodes.BadRequest, "Request is required.");

                var ratingCheck = ValidateRating(request.Rating);
                if (!ratingCheck.IsSuccess)
                    return Result<ReviewDto>.Failure(ratingCheck.Code, ratingCheck.Message);

                var commentCheck = ValidateComment(request.Comment);
                if (!commentCheck.IsSuccess)
                    return Result<ReviewDto>.Failure(commentCheck.Code, commentCheck.Message);

                var targetCheck = await ValidateTargetAsync(request.TargetType, request.TargetId, currentUserId);
                if (!targetCheck.IsSuccess)
                    return Result<ReviewDto>.Failure(targetCheck.Code, targetCheck.Message);

                // One-active-review-per-(reviewer, target). The DB
                // doesn't enforce this with a filtered unique index
                // today (a soft-deleted row would conflict), so we
                // check at the service level. Filtered-index follow-up
                // tracked in the controller doc comment.
                var existing = await _reviews.GetActiveByReviewerAsync(
                    request.TargetType, request.TargetId, currentUserId);
                if (existing != null)
                {
                    return Result<ReviewDto>.Failure(
                        ErrorCodes.Conflict,
                        "You've already reviewed this. Edit your existing review instead.");
                }

                var review = new Review
                {
                    Id = Guid.NewGuid(),
                    TargetType = request.TargetType,
                    TargetId = request.TargetId,
                    ReviewerUserId = currentUserId,
                    Rating = request.Rating,
                    Comment = string.IsNullOrWhiteSpace(request.Comment) ? null : request.Comment.Trim(),
                    Status = ReviewStatus.Active,
                    CreatedAtUtc = DateTime.UtcNow,
                };

                await _reviews.AddAsync(review);
                var saved = await _reviews.SaveChangesAsync();

                if (!saved)
                    return Result<ReviewDto>.Failure(ErrorCodes.Exception, "Failed to save review.");

                await RefreshAggregateAsync(request.TargetType, request.TargetId);

                var displayMap = await _reviews.GetReviewerDisplaysAsync(new[] { review.ReviewerUserId });
                return Result<ReviewDto>.Success(
                    MapToDto(review, displayMap, currentUserId),
                    "Review submitted.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Reviews] Create failed");
                return Result<ReviewDto>.Failure(ErrorCodes.Exception, $"Failed to submit review. {ex.Message}");
            }
        }

        /// <inheritdoc />
        public async Task<Result<ReviewDto>> UpdateAsync(Guid currentUserId, Guid reviewId, UpdateReviewRequestDto request)
        {
            try
            {
                if (request is null)
                    return Result<ReviewDto>.Failure(ErrorCodes.BadRequest, "Request is required.");

                var ratingCheck = ValidateRating(request.Rating);
                if (!ratingCheck.IsSuccess)
                    return Result<ReviewDto>.Failure(ratingCheck.Code, ratingCheck.Message);

                var commentCheck = ValidateComment(request.Comment);
                if (!commentCheck.IsSuccess)
                    return Result<ReviewDto>.Failure(commentCheck.Code, commentCheck.Message);

                var review = await _reviews.GetByIdAsync(reviewId);
                if (review is null || review.Status == ReviewStatus.Deleted)
                    return Result<ReviewDto>.Failure(ErrorCodes.NotFound, "Review not found.");

                if (review.ReviewerUserId != currentUserId)
                    return Result<ReviewDto>.Failure(ErrorCodes.Forbidden, "You can only edit your own review.");

                review.Rating = request.Rating;
                review.Comment = string.IsNullOrWhiteSpace(request.Comment) ? null : request.Comment.Trim();
                review.UpdatedAtUtc = DateTime.UtcNow;

                _reviews.Update(review);
                var saved = await _reviews.SaveChangesAsync();

                if (!saved)
                    return Result<ReviewDto>.Failure(ErrorCodes.Exception, "Failed to update review.");

                await RefreshAggregateAsync(review.TargetType, review.TargetId);

                var displayMap = await _reviews.GetReviewerDisplaysAsync(new[] { review.ReviewerUserId });
                return Result<ReviewDto>.Success(
                    MapToDto(review, displayMap, currentUserId),
                    "Review updated.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Reviews] Update failed for {ReviewId}", reviewId);
                return Result<ReviewDto>.Failure(ErrorCodes.Exception, $"Failed to update review. {ex.Message}");
            }
        }

        /// <inheritdoc />
        public async Task<Result> DeleteAsync(Guid currentUserId, Guid reviewId)
        {
            try
            {
                var review = await _reviews.GetByIdAsync(reviewId);
                if (review is null || review.Status == ReviewStatus.Deleted)
                    return Result.Failure(ErrorCodes.NotFound, "Review not found.");

                if (review.ReviewerUserId != currentUserId)
                    return Result.Failure(ErrorCodes.Forbidden, "You can only delete your own review.");

                review.Status = ReviewStatus.Deleted;
                review.DeletedAtUtc = DateTime.UtcNow;
                review.UpdatedAtUtc = DateTime.UtcNow;
                _reviews.Update(review);

                var saved = await _reviews.SaveChangesAsync();

                if (!saved)
                    return Result.Failure(ErrorCodes.Exception, "Failed to delete review.");

                await RefreshAggregateAsync(review.TargetType, review.TargetId);

                return Result.Success("Review deleted.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Reviews] Delete failed for {ReviewId}", reviewId);
                return Result.Failure(ErrorCodes.Exception, $"Failed to delete review. {ex.Message}");
            }
        }

        // ── Validation helpers ───────────────────────────────────────

        private static Result ValidateRating(int rating)
        {
            if (rating < 1 || rating > 5)
                return Result.Failure(ErrorCodes.BadRequest, "Rating must be between 1 and 5.");
            return Result.Success();
        }

        private static Result ValidateComment(string? comment)
        {
            // Comment is optional. Cap length so a runaway client
            // can't post a 10MB review. The DB column is HasMaxLength(2000)
            // and would truncate / error anyway — failing here gives a
            // friendly message.
            if (!string.IsNullOrEmpty(comment) && comment.Length > 2000)
                return Result.Failure(ErrorCodes.BadRequest, "Comment is too long (max 2000 characters).");
            return Result.Success();
        }

        /// <summary>
        /// Per-target-type validation. V1 implements Store fully:
        /// the merchant must exist, be a PhysicalStore, be Active,
        /// and NOT be owned by the caller. Other target types
        /// surface a "not yet supported" message so the schema is
        /// future-proofed without enabling unfinished surfaces.
        /// </summary>
        private async Task<Result> ValidateTargetAsync(
            ReviewTargetType targetType, Guid targetId, Guid currentUserId)
        {
            switch (targetType)
            {
                case ReviewTargetType.Store:
                {
                    var merchant = await _merchants.GetByIdAsync(targetId);
                    if (merchant is null)
                        return Result.Failure(ErrorCodes.NotFound, "Store not found.");

                    if (merchant.Type != MerchantType.PhysicalStore)
                        return Result.Failure(
                            ErrorCodes.BadRequest,
                            "This merchant isn't a physical store.");

                    if (merchant.Status != MerchantStatus.Active)
                        return Result.Failure(
                            ErrorCodes.BadRequest,
                            "This store isn't accepting reviews yet.");

                    if (merchant.OwnerUserId == currentUserId)
                        return Result.Failure(
                            ErrorCodes.Forbidden,
                            "You can't review your own store.");

                    return Result.Success();
                }

                case ReviewTargetType.Shop:
                {
                    // Buyer-facing storefront. Distinct from Store
                    // (PhysicalStore merchant) — the targetId here is a
                    // ShopProfile id, not a Merchant id. We still resolve
                    // ownership via the shop's owning merchant since the
                    // OwnerUserId lives on Merchant, not ShopProfile.
                    var shop = await _shops.GetByIdAsync(targetId);
                    if (shop is null)
                        return Result.Failure(ErrorCodes.NotFound, "Shop not found.");

                    if (shop.Status != ShopProfileStatus.Active)
                        return Result.Failure(
                            ErrorCodes.BadRequest,
                            "This shop isn't accepting reviews yet.");

                    var shopOwner = await _merchants.GetByIdAsync(shop.MerchantId);
                    // A shop can't exist without its owning merchant (FK
                    // is Restrict on delete), but treat a missing owner
                    // as not-found rather than a 500 — it would be a
                    // data-corruption case, not a user error.
                    if (shopOwner is null)
                        return Result.Failure(ErrorCodes.NotFound, "Shop owner not found.");

                    if (shopOwner.OwnerUserId == currentUserId)
                        return Result.Failure(
                            ErrorCodes.Forbidden,
                            "You can't review your own shop.");

                    return Result.Success();
                }

                case ReviewTargetType.MarketplaceListing:
                {
                    // Casual peer-to-peer listing. TargetId = MarketplaceListing.Id.
                    // Owner is a User (not a Merchant) — ownership check is direct.
                    // Feedback is allowed after active/sold/archived (per product),
                    // so we deliberately DON'T gate on listing.Status.
                    var listing = await _marketplaceListings.GetByIdAsync(targetId);
                    if (listing is null)
                        return Result.Failure(ErrorCodes.NotFound, "Listing not found.");

                    if (listing.OwnerUserId == currentUserId)
                        return Result.Failure(
                            ErrorCodes.Forbidden,
                            "You can't review your own listing.");

                    // Interaction gate — require a prior buyer ↔ seller chat about
                    // this listing so reviews come from people who actually engaged.
                    var hasInteracted = await _chat.HasMarketplaceConversationAsync(targetId, currentUserId);
                    if (!hasInteracted)
                        return Result.Failure(
                            ErrorCodes.Forbidden,
                            "Message the seller first. You can leave feedback after you've interacted about this listing.");

                    return Result.Success();
                }

                case ReviewTargetType.Product:
                case ReviewTargetType.Service:
                    return Result.Failure(
                        ErrorCodes.BadRequest,
                        "Reviews for this target type are not yet supported.");

                default:
                    return Result.Failure(ErrorCodes.BadRequest, "Unknown review target type.");
            }
        }

        // ── Aggregate updates ────────────────────────────────────────

        /// <summary>
        /// Refresh the target's aggregate Rating / ReviewCount fields
        /// after a review write so existing consumers (StoreProfile
        /// rating row, store list cards, dashboard stat tiles) keep
        /// showing accurate numbers without each having to call the
        /// summary endpoint.
        ///
        /// Runs as a SECOND save after the review CRUD has persisted —
        /// EF Core won't let us peek at unsaved tracked changes from
        /// the application layer (no AppDbContext access here), so the
        /// numbers are computed from the post-save view of the table.
        /// If this second save fails the next review CRUD will refresh.
        ///
        /// V1 only updates Merchant aggregates for Store targets. The
        /// equivalent Listing.Rating / ReviewCount update will land
        /// when Product / Service review targets are enabled.
        /// </summary>
        private async Task RefreshAggregateAsync(ReviewTargetType targetType, Guid targetId)
        {
            // Compute the post-save summary once, then update whichever
            // aggregate table the targetType points at. Product / Service
            // targets currently have no aggregate row to refresh (Listing
            // aggregates ship when those review surfaces enable) so we
            // bail out early for them.
            if (targetType != ReviewTargetType.Store
                && targetType != ReviewTargetType.Shop
                && targetType != ReviewTargetType.MarketplaceListing)
                return;

            var (avg, count) = await _reviews.GetSummaryAsync(targetType, targetId);

            switch (targetType)
            {
                case ReviewTargetType.Store:
                {
                    var merchant = await _merchants.GetByIdAsync(targetId);
                    if (merchant is null) return;
                    merchant.Rating = avg;
                    merchant.ReviewCount = count;
                    _merchants.Update(merchant);
                    // MerchantRepository.Update doesn't itself save —
                    // calling through to its SaveChangesAsync flushes
                    // the change in the same DbContext (scoped per
                    // request, shared with ReviewRepository above).
                    await _merchants.SaveChangesAsync();
                    return;
                }

                case ReviewTargetType.Shop:
                {
                    var shop = await _shops.GetByIdAsync(targetId);
                    if (shop is null) return;
                    shop.Rating = avg;
                    shop.ReviewCount = count;
                    _shops.Update(shop);
                    await _shops.SaveChangesAsync();
                    return;
                }

                case ReviewTargetType.MarketplaceListing:
                {
                    var listing = await _marketplaceListings.GetByIdAsync(targetId);
                    if (listing is null) return;
                    listing.Rating = avg;
                    listing.ReviewCount = count;
                    _marketplaceListings.Update(listing);
                    await _marketplaceListings.SaveChangesAsync();
                    return;
                }
            }
        }

        // ── Projection helpers ───────────────────────────────────────

        private static ReviewDto MapToDto(
            Review review,
            Dictionary<Guid, ReviewerDisplayInfo> displayMap,
            Guid? currentUserId)
        {
            var info = displayMap.TryGetValue(review.ReviewerUserId, out var x)
                ? x
                : new ReviewerDisplayInfo("Anonymous", null);
            return new ReviewDto
            {
                Id = review.Id,
                TargetType = review.TargetType,
                TargetId = review.TargetId,
                ReviewerUserId = review.ReviewerUserId,
                ReviewerDisplayName = string.IsNullOrWhiteSpace(info.DisplayName) ? "Anonymous" : info.DisplayName,
                ReviewerAvatarUrl = info.AvatarUrl,
                Rating = review.Rating,
                Comment = review.Comment,
                CreatedAtUtc = review.CreatedAtUtc,
                UpdatedAtUtc = review.UpdatedAtUtc,
                IsMine = currentUserId.HasValue && review.ReviewerUserId == currentUserId.Value,
            };
        }
    }
}
