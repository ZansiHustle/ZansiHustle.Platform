using System;
using System.Collections.Generic;

namespace ZansiHustle.Application.ZansiPulse.Dtos
{
    public class TopSellerDto
    {
        public Guid SellerId { get; set; }
        public string? Name { get; set; }
        public decimal PopularityScore { get; set; }
        public decimal QualityScore { get; set; }
        public int TotalViews { get; set; }
        public int TotalFavourites { get; set; }
        public int TotalListings { get; set; }
    }

    public class TopSearchTermDto
    {
        public string SearchTerm { get; set; } = string.Empty;
        public int SearchCount { get; set; }
        public int NoResultCount { get; set; }
    }

    /// <summary>A computed supply-vs-demand gap for the dashboard / sourcing view.</summary>
    public class SupplyDemandGapDto
    {
        public Guid? CategoryId { get; set; }
        public string? CategoryName { get; set; }
        public Guid? SubCategoryId { get; set; }
        public string? Province { get; set; }
        public string? City { get; set; }
        public string? SearchTerm { get; set; }
        public decimal DemandScore { get; set; }
        public int SupplyCount { get; set; }
        public decimal GapScore { get; set; }
        public string? RecommendedAction { get; set; }
    }

    /// <summary>Basic CEO/admin dashboard rollup for a period.</summary>
    public class DashboardOverviewDto
    {
        public DateTime PeriodStart { get; set; }
        public DateTime PeriodEnd { get; set; }
        public int TotalEvents { get; set; }

        public List<TrendingCategoryDto> TopCategories { get; set; } = new();
        public List<TrendingRegionDto> TopRegions { get; set; } = new();
        public List<TrendingListingDto> TopListings { get; set; } = new();
        public List<TopSellerDto> TopSellers { get; set; } = new();
        public List<TopSearchTermDto> TopSearchTerms { get; set; } = new();
        public List<SupplyDemandGapDto> SupplyDemandGaps { get; set; } = new();
    }

    /// <summary>Result of <c>POST /api/zansipulse/admin/run-snapshot</c>.</summary>
    public class SnapshotResultDto
    {
        public Guid Id { get; set; }
        public string SnapshotType { get; set; } = string.Empty;
        public DateTime PeriodStart { get; set; }
        public DateTime PeriodEnd { get; set; }
        public int TotalEvents { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
