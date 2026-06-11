using ZansiHustle.Shared.Enums.Listings;

namespace ZansiHustle.Application.Listings.Dtos
{
    /// <summary>
    /// Provider's per-service location, surfaced on the buyer "Visit provider"
    /// card. <see cref="Summary"/> is a pre-composed one-line label for display.
    /// </summary>
    public class ServiceProviderLocationDto
    {
        public string? Name { get; set; }
        /// <summary>One-line display label composed from the address parts.</summary>
        public string? Summary { get; set; }
        public string? AddressLine1 { get; set; }
        public string? AddressLine2 { get; set; }
        public string? City { get; set; }
        public string? Province { get; set; }
        public string? PostalCode { get; set; }
        public decimal? Latitude { get; set; }
        public decimal? Longitude { get; set; }
    }

    /// <summary>
    /// Resolved service-fulfilment configuration exposed on <see cref="ListingDto"/>
    /// for service listings. Null on the DTO means "provider hasn't configured
    /// fulfilment" — the mobile buyer flow then falls back safely and flags it as
    /// unconfirmed. The mobile resolver reads this object as <c>service.fulfilment</c>.
    /// </summary>
    public class ServiceFulfilmentDto
    {
        public ServiceFulfilmentMode Mode { get; set; }
        public bool AllowsHouseCall { get; set; }
        public bool AllowsProviderLocation { get; set; }
        public ServiceProviderLocationDto? ProviderLocation { get; set; }

        public ServiceTravelFeeType TravelFeeType { get; set; }
        public decimal? TravelFeePerKm { get; set; }
        public decimal? TravelFeeFlatAmount { get; set; }
        public decimal? FreeTravelRadiusKm { get; set; }
        public decimal? MaxTravelDistanceKm { get; set; }
        public decimal? TravelFeeMinimum { get; set; }
        public decimal? TravelFeeMaximum { get; set; }

        /// <summary>Extra charged when the buyer picks a house call (on top of
        /// the service price). Null/0 = none. Collected upfront by the platform.</summary>
        public decimal? HouseCallSurchargeAmount { get; set; }

        public int? LeadTimeHours { get; set; }
        public int? BufferMinutes { get; set; }

        /// <summary>
        /// Seller-set default booking duration (minutes). Drives the buyer
        /// booking flow's displayed duration + the payload it sends. Null →
        /// the 60-minute fallback (legacy services).
        /// </summary>
        public int? EstimatedDurationMinutes { get; set; }
    }
}
