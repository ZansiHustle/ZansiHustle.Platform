using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ZansiHustle.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddListings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Listings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    Slug = table.Column<string>(type: "nvarchar(220)", maxLength: 220, nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    MerchantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", maxLength: 5000, nullable: true),
                    Price = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Currency = table.Column<string>(type: "nvarchar(8)", maxLength: 8, nullable: false),
                    SellerCategoryId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SellerSubcategoryId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Province = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    City = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    Images = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsFeatured = table.Column<bool>(type: "bit", nullable: false),
                    IsBoosted = table.Column<bool>(type: "bit", nullable: false),
                    Rating = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    ReviewCount = table.Column<int>(type: "int", nullable: false),
                    Stock = table.Column<int>(type: "int", nullable: true),
                    Condition = table.Column<int>(type: "int", nullable: true),
                    DeliveryOptions = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PricingModel = table.Column<int>(type: "int", nullable: true),
                    ServiceArea = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    Turnaround = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    Availability = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BookingMethods = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Listings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Listings_Merchants_MerchantId",
                        column: x => x.MerchantId,
                        principalTable: "Merchants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Listings_SellerCategories_SellerCategoryId",
                        column: x => x.SellerCategoryId,
                        principalTable: "SellerCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Listings_SellerSubcategories_SellerSubcategoryId",
                        column: x => x.SellerSubcategoryId,
                        principalTable: "SellerSubcategories",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Listings_City",
                table: "Listings",
                column: "City");

            migrationBuilder.CreateIndex(
                name: "IX_Listings_Code",
                table: "Listings",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Listings_CreatedAtUtc",
                table: "Listings",
                column: "CreatedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_Listings_IsBoosted",
                table: "Listings",
                column: "IsBoosted");

            migrationBuilder.CreateIndex(
                name: "IX_Listings_IsFeatured",
                table: "Listings",
                column: "IsFeatured");

            migrationBuilder.CreateIndex(
                name: "IX_Listings_MerchantId",
                table: "Listings",
                column: "MerchantId");

            migrationBuilder.CreateIndex(
                name: "IX_Listings_Price",
                table: "Listings",
                column: "Price");

            migrationBuilder.CreateIndex(
                name: "IX_Listings_Province",
                table: "Listings",
                column: "Province");

            migrationBuilder.CreateIndex(
                name: "IX_Listings_SellerCategoryId",
                table: "Listings",
                column: "SellerCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Listings_SellerSubcategoryId",
                table: "Listings",
                column: "SellerSubcategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Listings_Slug",
                table: "Listings",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Listings_Status",
                table: "Listings",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Listings_Type",
                table: "Listings",
                column: "Type");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Listings");
        }
    }
}
