using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ZansiHustle.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddListingSourceAndShopProfileLink : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ListingSource",
                table: "Listings",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<Guid>(
                name: "ShopProfileId",
                table: "Listings",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Listings_ListingSource",
                table: "Listings",
                column: "ListingSource");

            migrationBuilder.CreateIndex(
                name: "IX_Listings_ShopProfileId",
                table: "Listings",
                column: "ShopProfileId");

            migrationBuilder.AddForeignKey(
                name: "FK_Listings_ShopProfiles_ShopProfileId",
                table: "Listings",
                column: "ShopProfileId",
                principalTable: "ShopProfiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Listings_ShopProfiles_ShopProfileId",
                table: "Listings");

            migrationBuilder.DropIndex(
                name: "IX_Listings_ListingSource",
                table: "Listings");

            migrationBuilder.DropIndex(
                name: "IX_Listings_ShopProfileId",
                table: "Listings");

            migrationBuilder.DropColumn(
                name: "ListingSource",
                table: "Listings");

            migrationBuilder.DropColumn(
                name: "ShopProfileId",
                table: "Listings");
        }
    }
}
