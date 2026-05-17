using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ZansiHustle.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddEngagementSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "FollowersCount",
                table: "ShopProfiles",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "SavesCount",
                table: "Merchants",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "LikeCount",
                table: "MarketplaceListings",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "LikeCount",
                table: "Listings",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "ListingLikes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ListingId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ListingLikes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ListingLikes_Listings_ListingId",
                        column: x => x.ListingId,
                        principalTable: "Listings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ListingLikes_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "MarketplaceListingLikes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MarketplaceListingId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MarketplaceListingLikes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MarketplaceListingLikes_MarketplaceListings_MarketplaceListingId",
                        column: x => x.MarketplaceListingId,
                        principalTable: "MarketplaceListings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MarketplaceListingLikes_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ShopFollows",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ShopProfileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShopFollows", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ShopFollows_ShopProfiles_ShopProfileId",
                        column: x => x.ShopProfileId,
                        principalTable: "ShopProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ShopFollows_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "StoreSaves",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MerchantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StoreSaves", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StoreSaves_Merchants_MerchantId",
                        column: x => x.MerchantId,
                        principalTable: "Merchants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_StoreSaves_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_ListingLikes_ListingId",
                table: "ListingLikes",
                column: "ListingId");

            migrationBuilder.CreateIndex(
                name: "IX_ListingLikes_UserId",
                table: "ListingLikes",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ListingLikes_UserId_ListingId",
                table: "ListingLikes",
                columns: new[] { "UserId", "ListingId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MarketplaceListingLikes_MarketplaceListingId",
                table: "MarketplaceListingLikes",
                column: "MarketplaceListingId");

            migrationBuilder.CreateIndex(
                name: "IX_MarketplaceListingLikes_UserId",
                table: "MarketplaceListingLikes",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_MarketplaceListingLikes_UserId_MarketplaceListingId",
                table: "MarketplaceListingLikes",
                columns: new[] { "UserId", "MarketplaceListingId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ShopFollows_ShopProfileId",
                table: "ShopFollows",
                column: "ShopProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_ShopFollows_UserId",
                table: "ShopFollows",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ShopFollows_UserId_ShopProfileId",
                table: "ShopFollows",
                columns: new[] { "UserId", "ShopProfileId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StoreSaves_MerchantId",
                table: "StoreSaves",
                column: "MerchantId");

            migrationBuilder.CreateIndex(
                name: "IX_StoreSaves_UserId",
                table: "StoreSaves",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_StoreSaves_UserId_MerchantId",
                table: "StoreSaves",
                columns: new[] { "UserId", "MerchantId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ListingLikes");

            migrationBuilder.DropTable(
                name: "MarketplaceListingLikes");

            migrationBuilder.DropTable(
                name: "ShopFollows");

            migrationBuilder.DropTable(
                name: "StoreSaves");

            migrationBuilder.DropColumn(
                name: "FollowersCount",
                table: "ShopProfiles");

            migrationBuilder.DropColumn(
                name: "SavesCount",
                table: "Merchants");

            migrationBuilder.DropColumn(
                name: "LikeCount",
                table: "MarketplaceListings");

            migrationBuilder.DropColumn(
                name: "LikeCount",
                table: "Listings");
        }
    }
}
