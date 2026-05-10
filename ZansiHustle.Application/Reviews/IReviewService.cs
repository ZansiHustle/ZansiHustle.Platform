using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ZansiHustle.Application.Reviews.Dtos;
using ZansiHustle.Shared.Enums.Reviews;
using ZansiHustle.Shared.Results;

namespace ZansiHustle.Application.Reviews
{
    /// <summary>
    /// Application-layer surface for the review feature. Read endpoints
    /// take an optional <c>currentUserId</c> so they can populate
    /// <c>IsMine</c> / <c>MyReview</c> when the caller is authenticated.
    /// Write endpoints all require an authenticated user.
    /// </summary>
    public interface IReviewService
    {
        Task<Result<List<ReviewDto>>> ListAsync(
            ReviewTargetType targetType, Guid targetId, Guid? currentUserId);

        Task<Result<ReviewSummaryDto>> GetSummaryAsync(
            ReviewTargetType targetType, Guid targetId, Guid? currentUserId);

        Task<Result<ReviewDto>> CreateAsync(Guid currentUserId, CreateReviewRequestDto request);

        Task<Result<ReviewDto>> UpdateAsync(Guid currentUserId, Guid reviewId, UpdateReviewRequestDto request);

        Task<Result> DeleteAsync(Guid currentUserId, Guid reviewId);
    }
}
