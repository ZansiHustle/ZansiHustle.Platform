using System;
using ZansiHustle.Shared.Enums.Listings;

namespace ZansiHustle.Application.ZansiPulse.Dtos
{
    /// <summary>A personalized listing recommendation with its computed score.</summary>
    public class RecommendedListingDto
    {
        public Guid ListingId { get; set; }
        /// <summary>Product (1) or Service (2) — lets the client route to the correct detail screen.</summary>
        public ListingType Type { get; set; }
        public string Title { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string Currency { get; set; } = "ZAR";
        public string? Province { get; set; }
        public string? City { get; set; }
        public string? ImageUrl { get; set; }
        public Guid? SellerCategoryId { get; set; }
        public string? CategoryName { get; set; }
        public Guid MerchantId { get; set; }
        public string? MerchantName { get; set; }

        /// <summary>Deterministic blended recommendation score (higher = better match).</summary>
        public decimal Score { get; set; }
    }

    /// <summary>A personalized shop recommendation with its computed score.</summary>
    public class RecommendedShopDto
    {
        public Guid ShopId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? LogoUrl { get; set; }
        public string? Province { get; set; }
        public string? City { get; set; }
        public decimal? Rating { get; set; }
        public int FollowersCount { get; set; }
        public decimal Score { get; set; }
    }
}
