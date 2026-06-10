using System;

namespace ZansiHustle.Application.ServiceBookings.Dtos
{
    /// <summary>
    /// Buyer request for a travel-fee quote on a house-call service. The server
    /// is the source of truth — the client never sends a fee, only its location.
    /// </summary>
    public class TravelQuoteRequestDto
    {
        public Guid ListingId { get; set; }
        public decimal? BuyerLatitude { get; set; }
        public decimal? BuyerLongitude { get; set; }
        public string? BuyerAddress { get; set; }
    }
}
