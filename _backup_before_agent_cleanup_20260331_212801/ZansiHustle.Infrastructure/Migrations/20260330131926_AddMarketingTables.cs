using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ZansiHustle.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMarketingTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AgentApplications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    FullName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    PhoneNumber = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Province = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    City = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    SocialHandle = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    SubmittedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ReviewedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReviewedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AgentApplications", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Agents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    FullName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    PhoneNumber = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Province = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    City = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    SocialHandle = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    JoinedDateUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Agents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BudgetTransactions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    TransactionDateUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TransactionType = table.Column<int>(type: "int", nullable: false),
                    Category = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Reference = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    RecordedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RelatedEntityType = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    RelatedEntityId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BudgetTransactions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Campaigns",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CampaignType = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    PrimaryPlatform = table.Column<int>(type: "int", nullable: true),
                    Budget = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    StartDateUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDateUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Campaigns", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Influencers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    FullName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Niche = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    Province = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    City = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    Rate = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    PhoneNumber = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(3000)", maxLength: 3000, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    AddedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Influencers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Podcasts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    HostName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Country = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    Region = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    Niche = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    AudienceSize = table.Column<long>(type: "bigint", nullable: true),
                    WebsiteUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ContactEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    MediaKitUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    AllowsGuestAppearance = table.Column<bool>(type: "bit", nullable: false),
                    AllowsSponsoredSegments = table.Column<bool>(type: "bit", nullable: false),
                    QuotedPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    OutreachStatus = table.Column<int>(type: "int", nullable: false),
                    ResponseStatus = table.Column<int>(type: "int", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(3000)", maxLength: 3000, nullable: true),
                    AddedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Podcasts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SellerLeads",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ContactName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    BusinessName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    LeadType = table.Column<int>(type: "int", nullable: false),
                    Category = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    Subcategory = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    PhoneNumber = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Province = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    City = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    SocialHandleOrLink = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    SourceType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    VerificationStatus = table.Column<int>(type: "int", nullable: false),
                    ApprovalStatus = table.Column<int>(type: "int", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(3000)", maxLength: 3000, nullable: true),
                    SubmittedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ReviewedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReviewedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AgentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ConvertedSellerId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SellerLeads", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SellerLeads_Agents_AgentId",
                        column: x => x.AgentId,
                        principalTable: "Agents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "CampaignMetricSnapshots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CampaignId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SnapshotDateUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Platform = table.Column<int>(type: "int", nullable: true),
                    Reach = table.Column<long>(type: "bigint", nullable: false),
                    Engagements = table.Column<long>(type: "bigint", nullable: false),
                    Impressions = table.Column<long>(type: "bigint", nullable: false),
                    Clicks = table.Column<long>(type: "bigint", nullable: false),
                    Conversions = table.Column<long>(type: "bigint", nullable: false),
                    Spend = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CampaignMetricSnapshots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CampaignMetricSnapshots_Campaigns_CampaignId",
                        column: x => x.CampaignId,
                        principalTable: "Campaigns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ContentTasks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Priority = table.Column<int>(type: "int", nullable: false),
                    DueDateUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CampaignId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AssignedToUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContentTasks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ContentTasks_Campaigns_CampaignId",
                        column: x => x.CampaignId,
                        principalTable: "Campaigns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "InfluencerPlatformAccounts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InfluencerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Platform = table.Column<int>(type: "int", nullable: false),
                    Handle = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    Url = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    FollowersCount = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InfluencerPlatformAccounts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InfluencerPlatformAccounts_Influencers_InfluencerId",
                        column: x => x.InfluencerId,
                        principalTable: "Influencers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PodcastAdFormats",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PodcastId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FormatName = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PodcastAdFormats", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PodcastAdFormats_Podcasts_PodcastId",
                        column: x => x.PodcastId,
                        principalTable: "Podcasts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AgentApplications_Code",
                table: "AgentApplications",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AgentApplications_Email",
                table: "AgentApplications",
                column: "Email");

            migrationBuilder.CreateIndex(
                name: "IX_AgentApplications_PhoneNumber",
                table: "AgentApplications",
                column: "PhoneNumber");

            migrationBuilder.CreateIndex(
                name: "IX_AgentApplications_Status",
                table: "AgentApplications",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Agents_Code",
                table: "Agents",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Agents_Email",
                table: "Agents",
                column: "Email");

            migrationBuilder.CreateIndex(
                name: "IX_Agents_PhoneNumber",
                table: "Agents",
                column: "PhoneNumber");

            migrationBuilder.CreateIndex(
                name: "IX_Agents_Status",
                table: "Agents",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_BudgetTransactions_Category",
                table: "BudgetTransactions",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_BudgetTransactions_Code",
                table: "BudgetTransactions",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BudgetTransactions_RelatedEntityType_RelatedEntityId",
                table: "BudgetTransactions",
                columns: new[] { "RelatedEntityType", "RelatedEntityId" });

            migrationBuilder.CreateIndex(
                name: "IX_BudgetTransactions_TransactionDateUtc",
                table: "BudgetTransactions",
                column: "TransactionDateUtc");

            migrationBuilder.CreateIndex(
                name: "IX_BudgetTransactions_TransactionType",
                table: "BudgetTransactions",
                column: "TransactionType");

            migrationBuilder.CreateIndex(
                name: "IX_CampaignMetricSnapshots_CampaignId",
                table: "CampaignMetricSnapshots",
                column: "CampaignId");

            migrationBuilder.CreateIndex(
                name: "IX_CampaignMetricSnapshots_Platform",
                table: "CampaignMetricSnapshots",
                column: "Platform");

            migrationBuilder.CreateIndex(
                name: "IX_CampaignMetricSnapshots_SnapshotDateUtc",
                table: "CampaignMetricSnapshots",
                column: "SnapshotDateUtc");

            migrationBuilder.CreateIndex(
                name: "IX_Campaigns_Code",
                table: "Campaigns",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Campaigns_Name",
                table: "Campaigns",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_Campaigns_PrimaryPlatform",
                table: "Campaigns",
                column: "PrimaryPlatform");

            migrationBuilder.CreateIndex(
                name: "IX_Campaigns_Status",
                table: "Campaigns",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_ContentTasks_CampaignId",
                table: "ContentTasks",
                column: "CampaignId");

            migrationBuilder.CreateIndex(
                name: "IX_ContentTasks_DueDateUtc",
                table: "ContentTasks",
                column: "DueDateUtc");

            migrationBuilder.CreateIndex(
                name: "IX_ContentTasks_Priority",
                table: "ContentTasks",
                column: "Priority");

            migrationBuilder.CreateIndex(
                name: "IX_ContentTasks_Status",
                table: "ContentTasks",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_InfluencerPlatformAccounts_InfluencerId",
                table: "InfluencerPlatformAccounts",
                column: "InfluencerId");

            migrationBuilder.CreateIndex(
                name: "IX_InfluencerPlatformAccounts_Platform",
                table: "InfluencerPlatformAccounts",
                column: "Platform");

            migrationBuilder.CreateIndex(
                name: "IX_Influencers_Code",
                table: "Influencers",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Influencers_Email",
                table: "Influencers",
                column: "Email");

            migrationBuilder.CreateIndex(
                name: "IX_Influencers_Niche",
                table: "Influencers",
                column: "Niche");

            migrationBuilder.CreateIndex(
                name: "IX_Influencers_PhoneNumber",
                table: "Influencers",
                column: "PhoneNumber");

            migrationBuilder.CreateIndex(
                name: "IX_Influencers_Status",
                table: "Influencers",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_PodcastAdFormats_PodcastId",
                table: "PodcastAdFormats",
                column: "PodcastId");

            migrationBuilder.CreateIndex(
                name: "IX_Podcasts_Code",
                table: "Podcasts",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Podcasts_ContactEmail",
                table: "Podcasts",
                column: "ContactEmail");

            migrationBuilder.CreateIndex(
                name: "IX_Podcasts_Name",
                table: "Podcasts",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_Podcasts_OutreachStatus",
                table: "Podcasts",
                column: "OutreachStatus");

            migrationBuilder.CreateIndex(
                name: "IX_Podcasts_ResponseStatus",
                table: "Podcasts",
                column: "ResponseStatus");

            migrationBuilder.CreateIndex(
                name: "IX_SellerLeads_AgentId",
                table: "SellerLeads",
                column: "AgentId");

            migrationBuilder.CreateIndex(
                name: "IX_SellerLeads_ApprovalStatus",
                table: "SellerLeads",
                column: "ApprovalStatus");

            migrationBuilder.CreateIndex(
                name: "IX_SellerLeads_BusinessName",
                table: "SellerLeads",
                column: "BusinessName");

            migrationBuilder.CreateIndex(
                name: "IX_SellerLeads_Code",
                table: "SellerLeads",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SellerLeads_ContactName",
                table: "SellerLeads",
                column: "ContactName");

            migrationBuilder.CreateIndex(
                name: "IX_SellerLeads_Email",
                table: "SellerLeads",
                column: "Email");

            migrationBuilder.CreateIndex(
                name: "IX_SellerLeads_LeadType",
                table: "SellerLeads",
                column: "LeadType");

            migrationBuilder.CreateIndex(
                name: "IX_SellerLeads_PhoneNumber",
                table: "SellerLeads",
                column: "PhoneNumber");

            migrationBuilder.CreateIndex(
                name: "IX_SellerLeads_VerificationStatus",
                table: "SellerLeads",
                column: "VerificationStatus");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AgentApplications");

            migrationBuilder.DropTable(
                name: "BudgetTransactions");

            migrationBuilder.DropTable(
                name: "CampaignMetricSnapshots");

            migrationBuilder.DropTable(
                name: "ContentTasks");

            migrationBuilder.DropTable(
                name: "InfluencerPlatformAccounts");

            migrationBuilder.DropTable(
                name: "PodcastAdFormats");

            migrationBuilder.DropTable(
                name: "SellerLeads");

            migrationBuilder.DropTable(
                name: "Campaigns");

            migrationBuilder.DropTable(
                name: "Influencers");

            migrationBuilder.DropTable(
                name: "Podcasts");

            migrationBuilder.DropTable(
                name: "Agents");
        }
    }
}
