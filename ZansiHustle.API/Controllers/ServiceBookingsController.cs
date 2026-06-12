using System;
using System.Collections.Generic;
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
    /// Service-booking endpoints: travel-fee quotes + availability (server is the
    /// source of truth), plus the post-payment booking workflow (seller list,
    /// detail, accept / mark-in-progress / mark-complete). Ownership + the
    /// lifecycle rules (day-of, dual-confirm) are enforced in the service layer.
    /// </summary>
    [Route("api/service-bookings")]
    [Authorize]
    public class ServiceBookingsController : BaseController
    {
        private readonly IServiceBookingService _serviceBookingService;
        private readonly ICurrentUserService _currentUserService;

        public ServiceBookingsController(
            IServiceBookingService serviceBookingService,
            ICurrentUserService currentUserService)
        {
            _serviceBookingService = serviceBookingService;
            _currentUserService = currentUserService;
        }

        // ─── Booking workflow ────────────────────────────────────────────────

        /// <summary>Bookings for the current seller's shops (server-filtered:
        /// never failed/stale PendingPayment).</summary>
        [HttpGet("seller")]
        [ProducesResponseType(typeof(Result<List<SellerBookingListItemDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetSellerBookings()
        {
            var userId = _currentUserService.UserId;
            if (!userId.HasValue)
                return ToActionResult(Result<List<SellerBookingListItemDto>>.Failure(ErrorCodes.Unauthorized, "User identifier not found in token."));

            return ToActionResult(await _serviceBookingService.GetForSellerAsync(userId.Value));
        }

        /// <summary>Booking detail for the current user (provider or customer).</summary>
        [HttpGet("{id:guid}")]
        [ProducesResponseType(typeof(Result<ServiceBookingDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetById(Guid id)
        {
            var userId = _currentUserService.UserId;
            if (!userId.HasValue)
                return ToActionResult(Result<ServiceBookingDto>.Failure(ErrorCodes.Unauthorized, "User identifier not found in token."));

            return ToActionResult(await _serviceBookingService.GetByIdForUserAsync(userId.Value, id));
        }

        /// <summary>The booking attached to an order (for the buyer's order screen).</summary>
        [HttpGet("by-order/{orderId:guid}")]
        [ProducesResponseType(typeof(Result<ServiceBookingDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetByOrder(Guid orderId)
        {
            var userId = _currentUserService.UserId;
            if (!userId.HasValue)
                return ToActionResult(Result<ServiceBookingDto>.Failure(ErrorCodes.Unauthorized, "User identifier not found in token."));

            return ToActionResult(await _serviceBookingService.GetByOrderForUserAsync(userId.Value, orderId));
        }

        /// <summary>Provider accepts a requested booking (Requested → Accepted).</summary>
        [HttpPost("{id:guid}/accept")]
        [ProducesResponseType(typeof(Result<ServiceBookingDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> Accept(Guid id)
        {
            var userId = _currentUserService.UserId;
            if (!userId.HasValue)
                return ToActionResult(Result<ServiceBookingDto>.Failure(ErrorCodes.Unauthorized, "User identifier not found in token."));

            return ToActionResult(await _serviceBookingService.AcceptAsync(userId.Value, id));
        }

        /// <summary>Provider rejects a Requested/Accepted booking with a reason —
        /// releases the slot and credits the customer's wallet.</summary>
        [HttpPost("{id:guid}/reject")]
        [ProducesResponseType(typeof(Result<ServiceBookingDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> Reject(Guid id, [FromBody] RejectBookingRequestDto request)
        {
            var userId = _currentUserService.UserId;
            if (!userId.HasValue)
                return ToActionResult(Result<ServiceBookingDto>.Failure(ErrorCodes.Unauthorized, "User identifier not found in token."));

            return ToActionResult(await _serviceBookingService.RejectAsync(userId.Value, id, request ?? new RejectBookingRequestDto()));
        }

        /// <summary>Customer cancels their own booking BEFORE the provider accepts
        /// (Requested/legacy Confirmed only) — releases the slot and credits the
        /// customer's wallet with the full paid amount. Blocked once accepted.</summary>
        [HttpPost("{id:guid}/customer-cancel")]
        [ProducesResponseType(typeof(Result<ServiceBookingDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> CustomerCancel(Guid id, [FromBody] CancelBookingRequestDto request)
        {
            var userId = _currentUserService.UserId;
            if (!userId.HasValue)
                return ToActionResult(Result<ServiceBookingDto>.Failure(ErrorCodes.Unauthorized, "User identifier not found in token."));

            return ToActionResult(await _serviceBookingService.CustomerCancelAsync(userId.Value, id, request ?? new CancelBookingRequestDto()));
        }

        /// <summary>Either party marks the booking started (allowed on/after the
        /// scheduled day; flips to InProgress once both sides have marked).</summary>
        [HttpPost("{id:guid}/mark-in-progress")]
        [ProducesResponseType(typeof(Result<ServiceBookingDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> MarkInProgress(Guid id)
        {
            var userId = _currentUserService.UserId;
            if (!userId.HasValue)
                return ToActionResult(Result<ServiceBookingDto>.Failure(ErrorCodes.Unauthorized, "User identifier not found in token."));

            return ToActionResult(await _serviceBookingService.MarkInProgressAsync(userId.Value, id));
        }

        /// <summary>Either party marks the booking complete (flips to Completed
        /// once both sides have marked).</summary>
        [HttpPost("{id:guid}/mark-complete")]
        [ProducesResponseType(typeof(Result<ServiceBookingDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> MarkComplete(Guid id)
        {
            var userId = _currentUserService.UserId;
            if (!userId.HasValue)
                return ToActionResult(Result<ServiceBookingDto>.Failure(ErrorCodes.Unauthorized, "User identifier not found in token."));

            return ToActionResult(await _serviceBookingService.MarkCompleteAsync(userId.Value, id));
        }

        /// <summary>
        /// Quote the travel fee for a house-call service from its server-side
        /// fulfilment config. None/FlatFee are exact; PerKilometre returns
        /// "not_ready" until a road-distance provider is wired (never faked).
        /// </summary>
        [HttpPost("quote-travel")]
        [ProducesResponseType(typeof(Result<TravelQuoteResultDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> QuoteTravel([FromBody] TravelQuoteRequestDto request)
        {
            var result = await _serviceBookingService.QuoteTravelAsync(request);
            return ToActionResult(result);
        }

        /// <summary>
        /// Bookable availability for a service over a date range. Slots are
        /// generated from the seller's availability and existing active bookings
        /// for the provider are subtracted, so two buyers can't take the same
        /// slot. `from`/`to` are local (yyyy-MM-dd) and clamped to the 30-day
        /// lookahead; both optional (defaults: tomorrow … +30 days). No buyer
        /// PII is exposed in booked slots.
        /// </summary>
        [HttpGet("availability")]
        [ProducesResponseType(typeof(Result<AvailabilityResponseDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAvailability(
            [FromQuery] Guid listingId,
            [FromQuery] DateOnly? from,
            [FromQuery] DateOnly? to)
        {
            var result = await _serviceBookingService.GetAvailabilityAsync(new AvailabilityRequestDto
            {
                ListingId = listingId,
                From = from,
                To = to
            });
            return ToActionResult(result);
        }
    }
}
