using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ZansiHustle.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddEvents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EventPlans",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EventType = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    EventDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    GuestCount = table.Column<int>(type: "int", nullable: true),
                    LocationArea = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    BudgetTotal = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    Currency = table.Column<string>(type: "nvarchar(8)", maxLength: 8, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventPlans", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EventTypeTemplates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EventType = table.Column<int>(type: "int", nullable: false),
                    CategorySlug = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    DisplayLabel = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Priority = table.Column<int>(type: "int", nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventTypeTemplates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EventPlanItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EventPlanId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SellerSubcategoryId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CategorySlug = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    CategoryLabel = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Priority = table.Column<int>(type: "int", nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    ListingId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    EstimatedCost = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventPlanItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EventPlanItems_EventPlans_EventPlanId",
                        column: x => x.EventPlanId,
                        principalTable: "EventPlans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EventPlanItems_Listings_ListingId",
                        column: x => x.ListingId,
                        principalTable: "Listings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_EventPlanItems_SellerSubcategories_SellerSubcategoryId",
                        column: x => x.SellerSubcategoryId,
                        principalTable: "SellerSubcategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EventPlanItems_EventPlanId",
                table: "EventPlanItems",
                column: "EventPlanId");

            migrationBuilder.CreateIndex(
                name: "IX_EventPlanItems_EventPlanId_CategorySlug",
                table: "EventPlanItems",
                columns: new[] { "EventPlanId", "CategorySlug" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EventPlanItems_ListingId",
                table: "EventPlanItems",
                column: "ListingId");

            migrationBuilder.CreateIndex(
                name: "IX_EventPlanItems_SellerSubcategoryId",
                table: "EventPlanItems",
                column: "SellerSubcategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_EventPlans_Code",
                table: "EventPlans",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EventPlans_CreatedAtUtc",
                table: "EventPlans",
                column: "CreatedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_EventPlans_EventType",
                table: "EventPlans",
                column: "EventType");

            migrationBuilder.CreateIndex(
                name: "IX_EventPlans_Status",
                table: "EventPlans",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_EventPlans_UserId",
                table: "EventPlans",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_EventTypeTemplates_EventType",
                table: "EventTypeTemplates",
                column: "EventType");

            migrationBuilder.CreateIndex(
                name: "IX_EventTypeTemplates_EventType_CategorySlug",
                table: "EventTypeTemplates",
                columns: new[] { "EventType", "CategorySlug" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EventPlanItems");

            migrationBuilder.DropTable(
                name: "EventTypeTemplates");

            migrationBuilder.DropTable(
                name: "EventPlans");
        }
    }
}
