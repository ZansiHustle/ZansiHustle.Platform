using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ZansiHustle.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSellerAndShopVisibility : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "VisibilityPauseReason",
                table: "ShopProfiles",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "VisibilityPausedAtUtc",
                table: "ShopProfiles",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "VisibilityStatus",
                table: "ShopProfiles",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<DateTime>(
                name: "VisibilityUpdatedAtUtc",
                table: "ShopProfiles",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SellerPauseReason",
                table: "Merchants",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "SellerPausedAtUtc",
                table: "Merchants",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SellerVisibility",
                table: "Merchants",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<DateTime>(
                name: "SellerVisibilityUpdatedAtUtc",
                table: "Merchants",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ShopProfiles_VisibilityStatus",
                table: "ShopProfiles",
                column: "VisibilityStatus");

            migrationBuilder.CreateIndex(
                name: "IX_Merchants_SellerVisibility",
                table: "Merchants",
                column: "SellerVisibility");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ShopProfiles_VisibilityStatus",
                table: "ShopProfiles");

            migrationBuilder.DropIndex(
                name: "IX_Merchants_SellerVisibility",
                table: "Merchants");

            migrationBuilder.DropColumn(
                name: "VisibilityPauseReason",
                table: "ShopProfiles");

            migrationBuilder.DropColumn(
                name: "VisibilityPausedAtUtc",
                table: "ShopProfiles");

            migrationBuilder.DropColumn(
                name: "VisibilityStatus",
                table: "ShopProfiles");

            migrationBuilder.DropColumn(
                name: "VisibilityUpdatedAtUtc",
                table: "ShopProfiles");

            migrationBuilder.DropColumn(
                name: "SellerPauseReason",
                table: "Merchants");

            migrationBuilder.DropColumn(
                name: "SellerPausedAtUtc",
                table: "Merchants");

            migrationBuilder.DropColumn(
                name: "SellerVisibility",
                table: "Merchants");

            migrationBuilder.DropColumn(
                name: "SellerVisibilityUpdatedAtUtc",
                table: "Merchants");
        }
    }
}
