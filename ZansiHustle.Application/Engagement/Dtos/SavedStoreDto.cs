using System;
using ZansiHustle.Shared.Enums.Merchants;

namespace ZansiHustle.Application.Engagement.Dtos
{
    /// <summary>
    /// Lightweight row for the buyer's "Saved → Stores" tab. Narrower
    /// than <c>MerchantPublicDto</c> — enough to render a card and
    /// route to the Store profile screen.
    /// </summary>
    public sealed class SavedStoreDto
    {
        public Guid Id { get; set; }
        public string Slug { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public MerchantType Type { get; set; }
        public string? Category { get; set; }
        public string? Province { get; set; }
        public string? City { get; set; }
        public string? Suburb { get; set; }
        public string? FormattedAddress { get; set; }
        public decimal? Latitude { get; set; }
        public decimal? Longitude { get; set; }
        public string? LogoUrl { get; set; }
        public string? BannerUrl { get; set; }
        public decimal? Rating { get; set; }
        public int ReviewCount { get; set; }
        public int SavesCount { get; set; }
        public bool IsSavedByMe { get; set; } = true;
        public DateTime SavedAtUtc { get; set; }
    }
}
