using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ZansiHustle.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddQuoteCurationAndSellerPickup : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SellerPickupNote",
                table: "ZansiDispatchShipments",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SellerPickupPreference",
                table: "ZansiDispatchShipments",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "SellerRequestedPickupDate",
                table: "ZansiDispatchShipments",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsCheckoutVisible",
                table: "ZansiDispatchQuoteOptions",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsRecommended",
                table: "ZansiDispatchQuoteOptions",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SellerPickupNote",
                table: "ZansiDispatchShipments");

            migrationBuilder.DropColumn(
                name: "SellerPickupPreference",
                table: "ZansiDispatchShipments");

            migrationBuilder.DropColumn(
                name: "SellerRequestedPickupDate",
                table: "ZansiDispatchShipments");

            migrationBuilder.DropColumn(
                name: "IsCheckoutVisible",
                table: "ZansiDispatchQuoteOptions");

            migrationBuilder.DropColumn(
                name: "IsRecommended",
                table: "ZansiDispatchQuoteOptions");
        }
    }
}
