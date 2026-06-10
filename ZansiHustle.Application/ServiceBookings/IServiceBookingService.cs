using System.Threading.Tasks;
using ZansiHustle.Application.ServiceBookings.Dtos;
using ZansiHustle.Shared.Results;

namespace ZansiHustle.Application.ServiceBookings
{
    public interface IServiceBookingService
    {
        /// <summary>
        /// Compute a travel-fee quote for a house-call service from the
        /// listing's server-side fulfilment configuration. Honest by design:
        /// distance-based (PerKilometre) pricing returns "not_ready" until a
        /// road-distance provider is wired — it never fabricates a distance.
        /// </summary>
        Task<Result<TravelQuoteResultDto>> QuoteTravelAsync(TravelQuoteRequestDto request);

        /// <summary>
        /// Compute bookable availability for a service over a date range: slots
        /// generated from the seller's availability MINUS existing active
        /// bookings for the provider (so two buyers can't take the same slot).
        /// Honest by design — never invents slots a seller didn't enable, and
        /// flags when only a permissive fallback was available.
        /// </summary>
        Task<Result<AvailabilityResponseDto>> GetAvailabilityAsync(AvailabilityRequestDto request);
    }
}
