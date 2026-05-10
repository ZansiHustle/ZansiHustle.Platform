using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ZansiHustle.Application.Common.Interfaces.Shared;
using ZansiHustle.Application.Reviews;
using ZansiHustle.Application.Reviews.Dtos;
using ZansiHustle.Shared.Enums.Reviews;
using ZansiHustle.Shared.Errors;
using ZansiHustle.Shared.Results;

namespace ZansiHustle.API.Controllers
{
    /// <summary>
    /// Polymorphic reviews API. The (TargetType, TargetId) pair scopes
    /// every read; write operations are scoped to the authenticated
    /// user. V1 surfaces only Store reviews — other target types are
    /// rejected by the service-layer validator with a clear message.
    ///
    /// Read endpoints are tolerant of unauthenticated callers (they
    /// just don't get the `IsMine` / `MyReview` projections); writes
    /// require auth. The `[Authorize]` attribute is declared per-action
    /// rather than at controller scope so reads stay open.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class ReviewsController : BaseController
    {
        private readonly IReviewService _reviewService;
        private readonly ICurrentUserService _currentUserService;

        public ReviewsController(IReviewService reviewService, ICurrentUserService currentUserService)
        {
            _reviewService = reviewService;
            _currentUserService = currentUserService;
        }

        /// <summary>
        /// Lists Active reviews for a target. `IsMine` is set when the
        /// caller is authenticated and authored a returned row.
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(Result<List<ReviewDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> List(
            [FromQuery] ReviewTargetType targetType,
            [FromQuery] Guid targetId)
        {
            var result = await _reviewService.ListAsync(targetType, targetId, _currentUserService.UserId);
            return ToActionResult(result);
        }

        /// <summary>
        /// Aggregate (avg, count) for a target. Authenticated callers
        /// also get back their own existing review (if any) so the UI
        /// can render "Edit your review" without a list call.
        /// </summary>
        [HttpGet("summary")]
        [ProducesResponseType(typeof(Result<ReviewSummaryDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetSummary(
            [FromQuery] ReviewTargetType targetType,
            [FromQuery] Guid targetId)
        {
            var result = await _reviewService.GetSummaryAsync(targetType, targetId, _currentUserService.UserId);
            return ToActionResult(result);
        }

        /// <summary>Creates a review for the authenticated user.</summary>
        [HttpPost]
        [Authorize]
        [ProducesResponseType(typeof(Result<ReviewDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> Create([FromBody] CreateReviewRequestDto request)
        {
            var userId = _currentUserService.UserId;
            if (!userId.HasValue)
                return ToActionResult(Result<ReviewDto>.Failure(ErrorCodes.Unauthorized, "User identifier not found in token."));

            var result = await _reviewService.CreateAsync(userId.Value, request);
            return ToActionResult(result);
        }

        /// <summary>Updates the authenticated user's own review.</summary>
        [HttpPut("{id:guid}")]
        [Authorize]
        [ProducesResponseType(typeof(Result<ReviewDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateReviewRequestDto request)
        {
            var userId = _currentUserService.UserId;
            if (!userId.HasValue)
                return ToActionResult(Result<ReviewDto>.Failure(ErrorCodes.Unauthorized, "User identifier not found in token."));

            var result = await _reviewService.UpdateAsync(userId.Value, id, request);
            return ToActionResult(result);
        }

        /// <summary>Soft-deletes the authenticated user's own review.</summary>
        [HttpDelete("{id:guid}")]
        [Authorize]
        [ProducesResponseType(typeof(Result), StatusCodes.Status200OK)]
        public async Task<IActionResult> Delete(Guid id)
        {
            var userId = _currentUserService.UserId;
            if (!userId.HasValue)
                return ToActionResult(Result.Failure(ErrorCodes.Unauthorized, "User identifier not found in token."));

            var result = await _reviewService.DeleteAsync(userId.Value, id);
            return ToActionResult(result);
        }
    }
}
