using ZansiHustle.Shared.Enums.ZansiDispatch;

namespace ZansiHustle.Application.Listings.Dtos
{
    /// <summary>
    /// Product delivery package details (parcel profile) surfaced to clients.
    /// Null on a product listing means the seller hasn't completed package
    /// details yet — the buyer checkout must not offer courier delivery for it,
    /// and the seller dashboard flags it. Always null for services.
    /// </summary>
    public sealed class ListingParcelDto
    {
        public ZansiDispatchItemSizeCategory? SizeCategory { get; set; }
        public decimal? WeightKg { get; set; }
        public decimal? LengthCm { get; set; }
        public decimal? WidthCm { get; set; }
        public decimal? HeightCm { get; set; }
        public bool Fragile { get; set; }
        public string? ContentsDescription { get; set; }

        /// <summary>
        /// True when weight + all three dimensions are positive — the product
        /// can be quoted accurately and booked with a courier. The mobile
        /// checkout gates delivery options / payment on this flag.
        /// </summary>
        public bool IsComplete { get; set; }
    }
}
