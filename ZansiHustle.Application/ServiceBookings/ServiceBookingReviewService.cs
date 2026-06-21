using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ZansiHustle.Application.Persistence.Merchants;
using ZansiHustle.Application.Persistence.ServiceBookings;
using ZansiHustle.Application.ServiceBookings.Dtos;
using ZansiHustle.Domain.ServiceBookings;
using ZansiHustle.Shared.Enums.Reviews;
using ZansiHustle.Shared.Enums.ServiceBookings;
using ZansiHustle.Shared.Errors;
using ZansiHustle.Shared.Results;

namespace ZansiHustle.Application.ServiceBookings
{
    /// <inheritdoc />
    public sealed class ServiceBookingReviewService : IServiceBookingReviewService
    {
        private const int MaxCommentLen = 1000;

        private readonly IServiceBookingReviewRepository _reviews;
        private readonly IServiceBookingRepository _bookings;
        private readonly IMerchantRepository _merchants;
        private readonly ILogger<ServiceBookingReviewService> _logger;

        public ServiceBookingReviewService(
            IServiceBookingReviewRepository reviews,
            IServiceBookingRepository bookings,
            IMerchantRepository merchants,
            ILogger<ServiceBookingReviewService> logger)
        {
            _reviews = reviews;
            _bookings = bookings;
            _merchants = merchants;
            _logger = logger;
        }

        // ── Reads ────────────────────────────────────────────────────

        /// <inheritdoc />
        public async Task<Result<ServiceBookingReviewsDto>> GetForBookingAsync(Guid bookingId, Guid currentUserId)
        {
            try
            {
                var booking = await _bookings.GetByIdWithDetailsAsync(bookingId);
                if (booking is null)
                    return Result<ServiceBookingReviewsDto>.Failure(ErrorCodes.NotFound, "Booking not found.");

                var roleResult = await ResolveRoleAsync(booking, currentUserId);
                if (!roleResult.IsSuccess || roleResult.Data is null)
                    return Result<ServiceBookingReviewsDto>.Failure(roleResult.Code, roleResult.Message);

                var role = roleResult.Data;

                var mine = await _reviews.GetActiveByDirectionAsync(bookingId, role.Direction);
                var canReview = booking.Status == ServiceBookingStatus.Completed && mine is null;

                var dto = new ServiceBookingReviewsDto
                {
                    BookingId = bookingId,
                    BookingStatus = booking.Status.ToString(),
                    ViewerRole = role.Role,
                    CanReview = canReview,
                    MyReview = mine is null ? null : MapItem(mine),
                };

                return Result<ServiceBookingReviewsDto>.Success(dto, "Booking reviews retrieved.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[ServiceBookingReviews] GetForBooking failed for {BookingId}", bookingId);
                return Result<ServiceBookingReviewsDto>.Failure(
                    ErrorCodes.Exception, $"Failed to load booking reviews. {ex.Message}");
            }
        }

        // ── Writes ───────────────────────────────────────────────────

        /// <inheritdoc />
        public async Task<Result<ServiceBookingReviewItemDto>> SubmitAsync(
            Guid bookingId, Guid currentUserId, int rating, string? comment)
        {
            try
            {
                var basic = ValidateInput(rating, comment);
                if (!basic.IsSuccess)
                    return Result<ServiceBookingReviewItemDto>.Failure(basic.Code, basic.Message);

                var booking = await _bookings.GetByIdWithDetailsAsync(bookingId);
                if (booking is null)
                    return Result<ServiceBookingReviewItemDto>.Failure(ErrorCodes.NotFound, "Booking not found.");

                var roleResult = await ResolveRoleAsync(booking, currentUserId);
                if (!roleResult.IsSuccess || roleResult.Data is null)
                    return Result<ServiceBookingReviewItemDto>.Failure(roleResult.Code, roleResult.Message);

                var role = roleResult.Data;

                if (booking.Status != ServiceBookingStatus.Completed)
                    return Result<ServiceBookingReviewItemDto>.Failure(
                        ErrorCodes.BadRequest, "You can review once the service is completed.");

                var existing = await _reviews.GetActiveByDirectionAsync(bookingId, role.Direction);
                if (existing != null)
                    return Result<ServiceBookingReviewItemDto>.Failure(
                        ErrorCodes.Conflict, "You've already rated this booking. Edit your rating instead.");

                var review = new ServiceBookingReview
                {
                    Id = Guid.NewGuid(),
                    BookingId = bookingId,
                    ReviewerUserId = currentUserId,
                    RevieweeUserId = role.RevieweeUserId,
                    Direction = role.Direction,
                    Rating = rating,
                    Comment = string.IsNullOrWhiteSpace(comment) ? null : comment.Trim(),
                    Status = ReviewStatus.Active,
                    CreatedAtUtc = DateTime.UtcNow,
                };

                await _reviews.AddAsync(review);
                var saved = await _reviews.SaveChangesAsync();
                if (!saved)
                    return Result<ServiceBookingReviewItemDto>.Failure(ErrorCodes.Exception, "Failed to save review.");

                return Result<ServiceBookingReviewItemDto>.Success(MapItem(review), "Review submitted.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[ServiceBookingReviews] Submit failed for {BookingId}", bookingId);
                return Result<ServiceBookingReviewItemDto>.Failure(
                    ErrorCodes.Exception, $"Failed to submit review. {ex.Message}");
            }
        }

        /// <inheritdoc />
        public async Task<Result<ServiceBookingReviewItemDto>> UpdateMineAsync(
            Guid bookingId, Guid currentUserId, int rating, string? comment)
        {
            try
            {
                var basic = ValidateInput(rating, comment);
                if (!basic.IsSuccess)
                    return Result<ServiceBookingReviewItemDto>.Failure(basic.Code, basic.Message);

                var booking = await _bookings.GetByIdWithDetailsAsync(bookingId);
                if (booking is null)
                    return Result<ServiceBookingReviewItemDto>.Failure(ErrorCodes.NotFound, "Booking not found.");

                var roleResult = await ResolveRoleAsync(booking, currentUserId);
                if (!roleResult.IsSuccess || roleResult.Data is null)
                    return Result<ServiceBookingReviewItemDto>.Failure(roleResult.Code, roleResult.Message);

                var role = roleResult.Data;

                var review = await _reviews.GetActiveByDirectionAsync(bookingId, role.Direction);
                if (review is null)
                    return Result<ServiceBookingReviewItemDto>.Failure(
                        ErrorCodes.NotFound, "You haven't rated this booking yet.");

                review.Rating = rating;
                review.Comment = string.IsNullOrWhiteSpace(comment) ? null : comment.Trim();
                review.UpdatedAtUtc = DateTime.UtcNow;

                _reviews.Update(review);
                var saved = await _reviews.SaveChangesAsync();
                if (!saved)
                    return Result<ServiceBookingReviewItemDto>.Failure(ErrorCodes.Exception, "Failed to update review.");

                return Result<ServiceBookingReviewItemDto>.Success(MapItem(review), "Review updated.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[ServiceBookingReviews] UpdateMine failed for {BookingId}", bookingId);
                return Result<ServiceBookingReviewItemDto>.Failure(
                    ErrorCodes.Exception, $"Failed to update review. {ex.Message}");
            }
        }

        // ── Helpers ──────────────────────────────────────────────────

        /// <summary>Resolved viewer role + the review direction/reviewee it implies.</summary>
        private sealed record RoleResolution(
            string Role, ServiceBookingReviewDirection Direction, Guid RevieweeUserId);

        /// <summary>
        /// Resolve the caller's role on the booking and the resulting review
        /// direction + reviewee. Customer → CustomerToProvider (reviewee =
        /// provider owner); provider owner → ProviderToCustomer (reviewee =
        /// customer). Anyone else → Forbidden.
        /// </summary>
        private async Task<Result<RoleResolution>> ResolveRoleAsync(
            ServiceBooking booking, Guid currentUserId)
        {
            // Provider owner is the booking merchant's OwnerUserId.
            var merchant = await _merchants.GetByIdAsync(booking.MerchantId);
            var providerOwnerId = merchant?.OwnerUserId ?? Guid.Empty;

            if (currentUserId == booking.CustomerUserId)
            {
                return Result<RoleResolution>.Success(new RoleResolution(
                    "customer", ServiceBookingReviewDirection.CustomerToProvider, providerOwnerId));
            }

            if (providerOwnerId != Guid.Empty && currentUserId == providerOwnerId)
            {
                return Result<RoleResolution>.Success(new RoleResolution(
                    "provider", ServiceBookingReviewDirection.ProviderToCustomer, booking.CustomerUserId));
            }

            return Result<RoleResolution>.Failure(
                ErrorCodes.Forbidden, "You're not part of this booking.");
        }

        private static Result ValidateInput(int rating, string? comment)
        {
            if (rating < 1 || rating > 5)
                return Result.Failure(ErrorCodes.BadRequest, "Rating must be between 1 and 5.");
            if (!string.IsNullOrEmpty(comment) && comment.Length > MaxCommentLen)
                return Result.Failure(ErrorCodes.BadRequest, $"Comment is too long (max {MaxCommentLen} characters).");
            return Result.Success();
        }

        private static ServiceBookingReviewItemDto MapItem(ServiceBookingReview r) => new()
        {
            Id = r.Id,
            Direction = DirectionToWire(r.Direction),
            Rating = r.Rating,
            Comment = r.Comment,
            CreatedAtUtc = r.CreatedAtUtc,
            UpdatedAtUtc = r.UpdatedAtUtc,
        };

        private static string DirectionToWire(ServiceBookingReviewDirection direction) => direction switch
        {
            ServiceBookingReviewDirection.CustomerToProvider => "customer_to_provider",
            ServiceBookingReviewDirection.ProviderToCustomer => "provider_to_customer",
            _ => direction.ToString(),
        };
    }
}
