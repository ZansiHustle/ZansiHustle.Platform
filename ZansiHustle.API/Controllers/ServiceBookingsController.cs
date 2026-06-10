using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ZansiHustle.Application.ServiceBookings;
using ZansiHustle.Application.ServiceBookings.Dtos;
using ZansiHustle.Shared.Results;

namespace ZansiHustle.API.Controllers
{
    /// <summary>
    /// Service-booking support endpoints. Today: server-computed travel-fee
    /// quotes (source of truth for any travel fee — the client never sends one).
    /// </summary>
    [Route("api/service-bookings")]
    [Authorize]
    public class ServiceBookingsController : BaseController
    {
        private readonly IServiceBookingService _serviceBookingService;

        public ServiceBookingsController(IServiceBookingService serviceBookingService)
        {
            _serviceBookingService = serviceBookingService;
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
