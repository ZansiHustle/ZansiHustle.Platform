using System;

namespace ZansiHustle.Application.Engagement.Dtos
{
    /// <summary>
    /// Lightweight row for the buyer's "Saved → Shops" tab. Mirrors a
    /// narrower subset of <c>ShopProfilePublicDto</c> — enough for the
    /// card UI (logo, name, location, rating, followers) without
    /// dragging the about-tab enrichment fields through every list
    /// query.
    /// </summary>
    public sealed class FollowedShopDto
    {
        public Guid Id { get; set; }
        public Guid MerchantId { get; set; }
        public string Slug { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? LogoUrl { get; set; }
        public string? BannerUrl { get; set; }
        public string? Province { get; set; }
        public string? City { get; set; }
        public decimal? Rating { get; set; }
        public int ReviewCount { get; set; }
        public int FollowersCount { get; set; }
        /// <summary>
        /// Always <c>true</c> on this DTO since it's only returned
        /// from the "my followed shops" endpoint, but kept for
        /// symmetry with the public DTO and to make optimistic-
        /// update clients trivial to write.
        /// </summary>
        public bool IsFollowedByMe { get; set; } = true;
        public DateTime FollowedAtUtc { get; set; }
    }
}
