using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ZansiHustle.Application.Listings;
using ZansiHustle.Application.Persistence.Listings;
using ZansiHustle.Application.Persistence.ServiceBookings;
using ZansiHustle.Application.ServiceBookings.Dtos;
using ZansiHustle.Shared.Enums.Listings;
using ZansiHustle.Shared.Errors;
using ZansiHustle.Shared.Results;

namespace ZansiHustle.Application.ServiceBookings
{
    /// <summary>
    /// Travel-fee quoting AND booking availability for services. Reads the
    /// listing's server-side config (so the client can't tamper) — travel fee is
    /// exact for None/FlatFee (PerKilometre stays honest "not_ready"), and
    /// availability subtracts existing active bookings from seller availability.
    /// </summary>
    public class ServiceBookingService : IServiceBookingService
    {
        private readonly IListingService _listingService;
        private readonly IListingRepository _listingRepository;
        private readonly IServiceBookingRepository _serviceBookingRepository;
        private readonly ILogger<ServiceBookingService> _logger;

        public ServiceBookingService(
            IListingService listingService,
            IListingRepository listingRepository,
            IServiceBookingRepository serviceBookingRepository,
            ILogger<ServiceBookingService> logger)
        {
            _listingService = listingService;
            _listingRepository = listingRepository;
            _serviceBookingRepository = serviceBookingRepository;
            _logger = logger;
        }

        // ─── Availability ────────────────────────────────────────────────────

        /// <inheritdoc />
        public async Task<Result<AvailabilityResponseDto>> GetAvailabilityAsync(AvailabilityRequestDto request)
        {
            if (request is null || request.ListingId == Guid.Empty)
                return Result<AvailabilityResponseDto>.Failure(ErrorCodes.BadRequest, "A listing id is required.");

            var listing = await _listingRepository.GetByIdAsync(request.ListingId);
            if (listing is null)
                return Result<AvailabilityResponseDto>.Failure(ErrorCodes.NotFound, "Service not found.");
            if (listing.Type != ListingType.Service)
                return Result<AvailabilityResponseDto>.Failure(ErrorCodes.BadRequest, "Availability applies to services only.");
            if (listing.Status != ListingStatus.Active)
                return Result<AvailabilityResponseDto>.Failure(ErrorCodes.BadRequest, "This service is not currently available.");

            var nowUtc = DateTime.UtcNow;
            var todayLocal = DateOnly.FromDateTime(nowUtc + BookingAvailabilityDefaults.SaUtcOffset);
            var earliest = todayLocal.AddDays(1); // first bookable day = tomorrow
            var maxDate = todayLocal.AddDays(BookingAvailabilityDefaults.LookaheadDays);

            // Resolve + clamp the requested window into [tomorrow, today+lookahead].
            var from = request.From ?? earliest;
            if (from < earliest) from = earliest;
            var to = request.To ?? maxDate;
            if (to > maxDate) to = maxDate;
            if (to < from) to = from;

            var avail = ServiceAvailabilityCalculator.ParseAvailability(listing.Availability);

            // Effective booking duration (seller value or 60 fallback) — drives
            // slot length + window-fit, so a 3h service blocks 3h of calendar.
            var duration = ServiceAvailabilityCalculator.ResolveDuration(listing.EstimatedDurationMinutes);

            // UTC window covering the whole local range (end-exclusive next day).
            var rangeStartUtc = ServiceAvailabilityCalculator.ToUtc(from, new TimeOnly(0, 0));
            var rangeEndUtc = ServiceAvailabilityCalculator.ToUtc(to.AddDays(1), new TimeOnly(0, 0));

            var activeBookings = await _serviceBookingRepository
                .GetActiveForMerchantInRangeAsync(listing.MerchantId, rangeStartUtc, rangeEndUtc, nowUtc);

            var response = new AvailabilityResponseDto
            {
                ListingId = listing.Id,
                From = Iso(from),
                To = Iso(to),
                MaxLookaheadDays = BookingAvailabilityDefaults.LookaheadDays,
                Timezone = BookingAvailabilityDefaults.TimezoneId,
                SetupIncomplete = !avail.IsConfigured,
                UsesFallbackAvailability = !avail.IsConfigured,
                Message = avail.IsConfigured
                    ? null
                    : "This provider hasn't set their availability yet — times shown are estimates and the provider confirms after booking."
            };

            var availableDates = new HashSet<string>();

            for (var date = from; date <= to; date = date.AddDays(1))
            {
                var slots = ServiceAvailabilityCalculator.GenerateSlotsForDate(date, avail, duration);
                foreach (var slot in slots)
                {
                    var blocked = activeBookings.Any(b =>
                        ServiceAvailabilityCalculator.Overlaps(
                            slot.StartAtUtc, slot.EndAtUtc,
                            b.StartAtUtc.AddMinutes(-b.BufferMinutes),
                            b.EndAtUtc.AddMinutes(b.BufferMinutes)));

                    var isAvailable = !blocked;
                    if (isAvailable) availableDates.Add(Iso(date));

                    response.Slots.Add(new AvailabilitySlotDto
                    {
                        Date = Iso(date),
                        StartTime = Hm(slot.StartTime),
                        EndTime = Hm(slot.EndTime),
                        StartAtUtc = slot.StartAtUtc,
                        EndAtUtc = slot.EndAtUtc,
                        IsAvailable = isAvailable,
                        Reason = isAvailable ? null : "booked"
                    });
                }
            }

            response.AvailableDates = availableDates.OrderBy(d => d).ToList();

            // Booked windows that fall inside the queried range — no buyer PII.
            response.BookedSlots = activeBookings
                .Where(b => b.EndAtUtc > rangeStartUtc && b.StartAtUtc < rangeEndUtc)
                .OrderBy(b => b.StartAtUtc)
                .Select(b =>
                {
                    var localStart = b.StartAtUtc + BookingAvailabilityDefaults.SaUtcOffset;
                    var localEnd = b.EndAtUtc + BookingAvailabilityDefaults.SaUtcOffset;
                    return new BookedSlotDto
                    {
                        Date = localStart.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                        StartTime = localStart.ToString("HH:mm", CultureInfo.InvariantCulture),
                        EndTime = localEnd.ToString("HH:mm", CultureInfo.InvariantCulture),
                        StartAtUtc = b.StartAtUtc,
                        EndAtUtc = b.EndAtUtc,
                        Status = b.Status.ToString()
                    };
                })
                .ToList();

            return Result<AvailabilityResponseDto>.Success(response, "Availability computed.");
        }

        private static string Iso(DateOnly d) => d.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        private static string Hm(TimeOnly t) => t.ToString("HH:mm", CultureInfo.InvariantCulture);

        public async Task<Result<TravelQuoteResultDto>> QuoteTravelAsync(TravelQuoteRequestDto request)
        {
            if (request is null || request.ListingId == Guid.Empty)
                return Result<TravelQuoteResultDto>.Failure(ErrorCodes.BadRequest, "A listing id is required.");

            var listingResult = await _listingService.GetByIdAsync(request.ListingId);
            if (!listingResult.IsSuccess || listingResult.Data is null)
                return Result<TravelQuoteResultDto>.Failure(ErrorCodes.NotFound, "Service not found.");

            var listing = listingResult.Data;
            if (listing.Type != ListingType.Service)
                return Result<TravelQuoteResultDto>.Failure(ErrorCodes.BadRequest, "Travel quotes apply to services only.");

            var serviceFee = listing.Price;
            var currency = string.IsNullOrWhiteSpace(listing.Currency) ? "ZAR" : listing.Currency;
            var fulfilment = listing.Fulfilment;

            TravelQuoteResultDto NotReady(string message) => new()
            {
                Status = "not_ready",
                DistanceKm = null,
                DurationMinutes = null,
                TravelFee = 0m,
                ServiceFee = serviceFee,
                Total = serviceFee,
                Currency = currency,
                Message = message,
            };

            TravelQuoteResultDto Ok(decimal travelFee, decimal? distanceKm, string? message = null) => new()
            {
                Status = "ok",
                DistanceKm = distanceKm,
                DurationMinutes = null,
                TravelFee = travelFee,
                ServiceFee = serviceFee,
                Total = serviceFee + travelFee,
                Currency = currency,
                Message = message,
            };

            // Provider hasn't configured fulfilment → honest not-ready.
            if (fulfilment is null)
                return Result<TravelQuoteResultDto>.Success(
                    NotReady("This provider hasn't set up travel pricing yet — arrange any travel cost directly with them."),
                    "Travel quote computed.");

            // Not a house-call service → no travel fee.
            if (!fulfilment.AllowsHouseCall)
                return Result<TravelQuoteResultDto>.Success(
                    Ok(0m, 0m, "No travel fee — this service is offered at the provider's location."),
                    "Travel quote computed.");

            switch (fulfilment.TravelFeeType)
            {
                case ServiceTravelFeeType.None:
                    return Result<TravelQuoteResultDto>.Success(
                        Ok(0m, null, "No travel fee for this service."),
                        "Travel quote computed.");

                case ServiceTravelFeeType.FlatFee:
                {
                    var fee = Clamp(
                        fulfilment.TravelFeeFlatAmount ?? 0m,
                        fulfilment.TravelFeeMinimum,
                        fulfilment.TravelFeeMaximum);
                    return Result<TravelQuoteResultDto>.Success(
                        Ok(fee, null),
                        "Travel quote computed.");
                }

                case ServiceTravelFeeType.PerKilometre:
                    // No road-distance provider (Google Routes) wired yet. We do
                    // NOT straight-line estimate and present it as a road fee.
                    _logger.LogInformation(
                        "[travel-quote] per-km not_ready listing={ListingId} (no road-distance provider)",
                        request.ListingId);
                    return Result<TravelQuoteResultDto>.Success(
                        NotReady("We can't calculate distance-based travel fees online yet. Arrange the travel cost with the provider — only the service fee is charged online for now."),
                        "Travel quote computed.");

                default:
                    return Result<TravelQuoteResultDto>.Success(
                        NotReady("Travel pricing isn't available for this service yet."),
                        "Travel quote computed.");
            }
        }

        private static decimal Clamp(decimal value, decimal? min, decimal? max)
        {
            if (min.HasValue && value < min.Value) value = min.Value;
            if (max.HasValue && value > max.Value) value = max.Value;
            return value < 0 ? 0 : value;
        }
    }
}
