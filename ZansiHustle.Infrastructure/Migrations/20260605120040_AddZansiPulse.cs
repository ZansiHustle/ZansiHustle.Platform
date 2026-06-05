using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ZansiHustle.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddZansiPulse : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ZansiPulseCategoryMetrics",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CategoryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SubCategoryId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    PeriodType = table.Column<int>(type: "int", nullable: false),
                    PeriodStart = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PeriodEnd = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TotalViews = table.Column<int>(type: "int", nullable: false),
                    TotalSearches = table.Column<int>(type: "int", nullable: false),
                    TotalFavourites = table.Column<int>(type: "int", nullable: false),
                    TotalMessages = table.Column<int>(type: "int", nullable: false),
                    TrendingScore = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ZansiPulseCategoryMetrics", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ZansiPulseEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    EventType = table.Column<int>(type: "int", nullable: false),
                    ListingId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SellerId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ShopId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CategoryId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SubCategoryId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SearchTerm = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Province = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    City = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    Price = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    Weight = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    MetadataJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ZansiPulseEvents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ZansiPulseListingMetrics",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ListingId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TotalViews = table.Column<int>(type: "int", nullable: false),
                    TotalDetailOpens = table.Column<int>(type: "int", nullable: false),
                    TotalFavourites = table.Column<int>(type: "int", nullable: false),
                    TotalShares = table.Column<int>(type: "int", nullable: false),
                    TotalMessages = table.Column<int>(type: "int", nullable: false),
                    TotalReports = table.Column<int>(type: "int", nullable: false),
                    TrendingScore = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    LastEngagementAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ZansiPulseListingMetrics", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ZansiPulseRecommendationLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RecommendationType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Source = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ListingIdsJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ShopIdsJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SellerIdsJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CategoryIdsJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RequestMetadataJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ZansiPulseRecommendationLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ZansiPulseRegionMetrics",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Province = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    City = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    PeriodType = table.Column<int>(type: "int", nullable: false),
                    PeriodStart = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PeriodEnd = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TotalViews = table.Column<int>(type: "int", nullable: false),
                    TotalSearches = table.Column<int>(type: "int", nullable: false),
                    TotalFavourites = table.Column<int>(type: "int", nullable: false),
                    TotalMessages = table.Column<int>(type: "int", nullable: false),
                    TrendingScore = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ZansiPulseRegionMetrics", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ZansiPulseSearchTermMetrics",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SearchTerm = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Province = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    City = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    CategoryId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SearchCount = table.Column<int>(type: "int", nullable: false),
                    ResultCount = table.Column<int>(type: "int", nullable: false),
                    ClickThroughCount = table.Column<int>(type: "int", nullable: false),
                    NoResultCount = table.Column<int>(type: "int", nullable: false),
                    PeriodType = table.Column<int>(type: "int", nullable: false),
                    PeriodStart = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PeriodEnd = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ZansiPulseSearchTermMetrics", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ZansiPulseSellerMetrics",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SellerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TotalListings = table.Column<int>(type: "int", nullable: false),
                    TotalViews = table.Column<int>(type: "int", nullable: false),
                    TotalFavourites = table.Column<int>(type: "int", nullable: false),
                    TotalMessages = table.Column<int>(type: "int", nullable: false),
                    TotalReports = table.Column<int>(type: "int", nullable: false),
                    ResponsivenessScore = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    TrustScore = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    PopularityScore = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    QualityScore = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ZansiPulseSellerMetrics", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ZansiPulseSettings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Key = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Value = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ZansiPulseSettings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ZansiPulseShopMetrics",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ShopId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TotalViews = table.Column<int>(type: "int", nullable: false),
                    TotalFavourites = table.Column<int>(type: "int", nullable: false),
                    TotalShares = table.Column<int>(type: "int", nullable: false),
                    TotalMessages = table.Column<int>(type: "int", nullable: false),
                    TotalReports = table.Column<int>(type: "int", nullable: false),
                    PopularityScore = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    QualityScore = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ZansiPulseShopMetrics", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ZansiPulseSnapshots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SnapshotType = table.Column<int>(type: "int", nullable: false),
                    PeriodStart = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PeriodEnd = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TotalEvents = table.Column<int>(type: "int", nullable: false),
                    TopCategoriesJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TopRegionsJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TopListingsJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TopSellersJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TopSearchTermsJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SupplyDemandGapsJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ZansiPulseSnapshots", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ZansiPulseSupplyDemandGaps",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CategoryId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SubCategoryId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Province = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    City = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    SearchTerm = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    DemandScore = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    SupplyCount = table.Column<int>(type: "int", nullable: false),
                    GapScore = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    RecommendedAction = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    PeriodStart = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PeriodEnd = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ZansiPulseSupplyDemandGaps", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ZansiPulseUserInterestScores",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CategoryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SubCategoryId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Score = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    Source = table.Column<int>(type: "int", nullable: false),
                    LastUpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ZansiPulseUserInterestScores", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ZansiPulseCategoryMetrics_CategoryId_SubCategoryId_PeriodType_PeriodStart",
                table: "ZansiPulseCategoryMetrics",
                columns: new[] { "CategoryId", "SubCategoryId", "PeriodType", "PeriodStart" },
                unique: true,
                filter: "[SubCategoryId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ZansiPulseCategoryMetrics_PeriodType_PeriodStart",
                table: "ZansiPulseCategoryMetrics",
                columns: new[] { "PeriodType", "PeriodStart" });

            migrationBuilder.CreateIndex(
                name: "IX_ZansiPulseEvents_CategoryId",
                table: "ZansiPulseEvents",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_ZansiPulseEvents_CreatedAt",
                table: "ZansiPulseEvents",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ZansiPulseEvents_EventType",
                table: "ZansiPulseEvents",
                column: "EventType");

            migrationBuilder.CreateIndex(
                name: "IX_ZansiPulseEvents_EventType_CreatedAt",
                table: "ZansiPulseEvents",
                columns: new[] { "EventType", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ZansiPulseEvents_ListingId",
                table: "ZansiPulseEvents",
                column: "ListingId");

            migrationBuilder.CreateIndex(
                name: "IX_ZansiPulseEvents_Province_City",
                table: "ZansiPulseEvents",
                columns: new[] { "Province", "City" });

            migrationBuilder.CreateIndex(
                name: "IX_ZansiPulseEvents_SellerId",
                table: "ZansiPulseEvents",
                column: "SellerId");

            migrationBuilder.CreateIndex(
                name: "IX_ZansiPulseEvents_ShopId",
                table: "ZansiPulseEvents",
                column: "ShopId");

            migrationBuilder.CreateIndex(
                name: "IX_ZansiPulseEvents_UserId",
                table: "ZansiPulseEvents",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ZansiPulseListingMetrics_ListingId",
                table: "ZansiPulseListingMetrics",
                column: "ListingId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ZansiPulseListingMetrics_TrendingScore",
                table: "ZansiPulseListingMetrics",
                column: "TrendingScore");

            migrationBuilder.CreateIndex(
                name: "IX_ZansiPulseRecommendationLogs_CreatedAt",
                table: "ZansiPulseRecommendationLogs",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ZansiPulseRecommendationLogs_RecommendationType",
                table: "ZansiPulseRecommendationLogs",
                column: "RecommendationType");

            migrationBuilder.CreateIndex(
                name: "IX_ZansiPulseRecommendationLogs_UserId",
                table: "ZansiPulseRecommendationLogs",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ZansiPulseRegionMetrics_PeriodType_PeriodStart",
                table: "ZansiPulseRegionMetrics",
                columns: new[] { "PeriodType", "PeriodStart" });

            migrationBuilder.CreateIndex(
                name: "IX_ZansiPulseRegionMetrics_Province_City_PeriodType_PeriodStart",
                table: "ZansiPulseRegionMetrics",
                columns: new[] { "Province", "City", "PeriodType", "PeriodStart" },
                unique: true,
                filter: "[City] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ZansiPulseSearchTermMetrics_PeriodType_PeriodStart",
                table: "ZansiPulseSearchTermMetrics",
                columns: new[] { "PeriodType", "PeriodStart" });

            migrationBuilder.CreateIndex(
                name: "IX_ZansiPulseSearchTermMetrics_SearchTerm",
                table: "ZansiPulseSearchTermMetrics",
                column: "SearchTerm");

            migrationBuilder.CreateIndex(
                name: "IX_ZansiPulseSearchTermMetrics_SearchTerm_Province_City_CategoryId_PeriodType_PeriodStart",
                table: "ZansiPulseSearchTermMetrics",
                columns: new[] { "SearchTerm", "Province", "City", "CategoryId", "PeriodType", "PeriodStart" },
                unique: true,
                filter: "[Province] IS NOT NULL AND [City] IS NOT NULL AND [CategoryId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ZansiPulseSellerMetrics_PopularityScore",
                table: "ZansiPulseSellerMetrics",
                column: "PopularityScore");

            migrationBuilder.CreateIndex(
                name: "IX_ZansiPulseSellerMetrics_SellerId",
                table: "ZansiPulseSellerMetrics",
                column: "SellerId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ZansiPulseSettings_Key",
                table: "ZansiPulseSettings",
                column: "Key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ZansiPulseShopMetrics_PopularityScore",
                table: "ZansiPulseShopMetrics",
                column: "PopularityScore");

            migrationBuilder.CreateIndex(
                name: "IX_ZansiPulseShopMetrics_ShopId",
                table: "ZansiPulseShopMetrics",
                column: "ShopId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ZansiPulseSnapshots_CreatedAt",
                table: "ZansiPulseSnapshots",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ZansiPulseSnapshots_SnapshotType_PeriodStart_PeriodEnd",
                table: "ZansiPulseSnapshots",
                columns: new[] { "SnapshotType", "PeriodStart", "PeriodEnd" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ZansiPulseSupplyDemandGaps_CategoryId_Province_City",
                table: "ZansiPulseSupplyDemandGaps",
                columns: new[] { "CategoryId", "Province", "City" });

            migrationBuilder.CreateIndex(
                name: "IX_ZansiPulseSupplyDemandGaps_GapScore",
                table: "ZansiPulseSupplyDemandGaps",
                column: "GapScore");

            migrationBuilder.CreateIndex(
                name: "IX_ZansiPulseSupplyDemandGaps_PeriodStart_PeriodEnd",
                table: "ZansiPulseSupplyDemandGaps",
                columns: new[] { "PeriodStart", "PeriodEnd" });

            migrationBuilder.CreateIndex(
                name: "IX_ZansiPulseUserInterestScores_UserId",
                table: "ZansiPulseUserInterestScores",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ZansiPulseUserInterestScores_UserId_CategoryId_SubCategoryId",
                table: "ZansiPulseUserInterestScores",
                columns: new[] { "UserId", "CategoryId", "SubCategoryId" },
                unique: true,
                filter: "[SubCategoryId] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ZansiPulseCategoryMetrics");

            migrationBuilder.DropTable(
                name: "ZansiPulseEvents");

            migrationBuilder.DropTable(
                name: "ZansiPulseListingMetrics");

            migrationBuilder.DropTable(
                name: "ZansiPulseRecommendationLogs");

            migrationBuilder.DropTable(
                name: "ZansiPulseRegionMetrics");

            migrationBuilder.DropTable(
                name: "ZansiPulseSearchTermMetrics");

            migrationBuilder.DropTable(
                name: "ZansiPulseSellerMetrics");

            migrationBuilder.DropTable(
                name: "ZansiPulseSettings");

            migrationBuilder.DropTable(
                name: "ZansiPulseShopMetrics");

            migrationBuilder.DropTable(
                name: "ZansiPulseSnapshots");

            migrationBuilder.DropTable(
                name: "ZansiPulseSupplyDemandGaps");

            migrationBuilder.DropTable(
                name: "ZansiPulseUserInterestScores");
        }
    }
}
