using System;
using ZansiHustle.Shared.Enums.Listings;
using ZansiHustle.Shared.Enums.ZansiPulse;

namespace ZansiHustle.Application.ZansiPulse.Dtos
{
    /// <summary>Filter for <c>GET /api/zansipulse/trending/listings</c>.</summary>
    public class TrendingListingFilterDto
    {
        public string? Province { get; set; }
        public string? City { get; set; }
        public Guid? CategoryId { get; set; }
        /// <summary>When set, only listings with engagement inside the period window are considered.</summary>
        public ZansiPulsePeriodType? Period { get; set; }
        public int Take { get; set; } = 20;
    }

    public class TrendingListingDto
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
        // Card context — populated so mobile/portal can render a full
        // trending card (category chip + seller reference) without a
        // follow-up listing fetch.
        public Guid? SellerCategoryId { get; set; }
        public string? CategoryName { get; set; }
        public Guid MerchantId { get; set; }
        public string? MerchantName { get; set; }
        public decimal TrendingScore { get; set; }
        public int TotalViews { get; set; }
        public int TotalFavourites { get; set; }
    }

    public class TrendingCategoryDto
    {
        public Guid CategoryId { get; set; }
        public string? CategoryName { get; set; }
        public decimal TrendingScore { get; set; }
        public int TotalViews { get; set; }
        public int TotalSearches { get; set; }
        public int TotalFavourites { get; set; }
        public int TotalMessages { get; set; }
    }

    public class TrendingRegionDto
    {
        public string Province { get; set; } = string.Empty;
        public string? City { get; set; }
        public decimal TrendingScore { get; set; }
        public int TotalViews { get; set; }
        public int TotalSearches { get; set; }
        public int TotalFavourites { get; set; }
        public int TotalMessages { get; set; }
    }
}
