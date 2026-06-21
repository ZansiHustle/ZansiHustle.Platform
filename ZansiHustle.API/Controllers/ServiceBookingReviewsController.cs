using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ZansiHustle.Application.Common.Interfaces.Shared;
using ZansiHustle.Application.ServiceBookings;
using ZansiHustle.Application.ServiceBookings.Dtos;
using ZansiHustle.Shared.Errors;
using ZansiHustle.Shared.Results;

namespace ZansiHustle.API.Controllers
{
    /// <summary>
    /// Two-way (customer ↔ provider) service-booking reviews. All routes are
    /// scoped to a booking and require an authenticated user who is a party to
    /// that booking. The viewer's role + direction are resolved server-side; the
    /// client never picks who it's reviewing.
    ///
    /// Privacy (v1): GET returns only the caller's OWN review, never the
    /// counterpart's.
    /// </summary>
    [ApiController]
    [Authorize]
    [Route("api/service-bookings/{bookingId:guid}/reviews")]
    public class ServiceBookingReviewsController : BaseController
    {
        private readonly IServiceBookingReviewService _service;
        private readonly ICurrentUserService _currentUserService;

        public ServiceBookingReviewsController(
            IServiceBookingReviewService service, ICurrentUserService currentUserService)
        {
            _service = service;
            _currentUserService = currentUserService;
        }

        /// <summary>The caller's review surface for the booking (own review + flags).</summary>
        [HttpGet]
        [ProducesResponseType(typeof(Result<ServiceBookingReviewsDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetForBooking(Guid bookingId)
        {
            var userId = _currentUserService.UserId;
            if (!userId.HasValue)
                return ToActionResult(Result<ServiceBookingReviewsDto>.Failure(
                    ErrorCodes.Unauthorized, "User identifier not found in token."));

            var result = await _service.GetForBookingAsync(bookingId, userId.Value);
            return ToActionResult(result);
        }

        /// <summary>Create the caller's review for a completed booking.</summary>
        [HttpPost]
        [ProducesResponseType(typeof(Result<ServiceBookingReviewItemDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> Submit(
            Guid bookingId, [FromBody] SubmitServiceBookingReviewRequestDto request)
        {
            var userId = _currentUserService.UserId;
            if (!userId.HasValue)
                return ToActionResult(Result<ServiceBookingReviewItemDto>.Failure(
                    ErrorCodes.Unauthorized, "User identifier not found in token."));

            request ??= new SubmitServiceBookingReviewRequestDto();
            var result = await _service.SubmitAsync(bookingId, userId.Value, request.Rating, request.Comment);
            return ToActionResult(result);
        }

        /// <summary>Edit the caller's existing review for the booking.</summary>
        [HttpPut("mine")]
        [ProducesResponseType(typeof(Result<ServiceBookingReviewItemDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> UpdateMine(
            Guid bookingId, [FromBody] SubmitServiceBookingReviewRequestDto request)
        {
            var userId = _currentUserService.UserId;
            if (!userId.HasValue)
                return ToActionResult(Result<ServiceBookingReviewItemDto>.Failure(
                    ErrorCodes.Unauthorized, "User identifier not found in token."));

            request ??= new SubmitServiceBookingReviewRequestDto();
            var result = await _service.UpdateMineAsync(bookingId, userId.Value, request.Rating, request.Comment);
            return ToActionResult(result);
        }
    }
}
