using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ZansiHustle.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddShipmentProviderExpectedDates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "ActualWeightKg",
                table: "ZansiDispatchShipments",
                type: "decimal(18,3)",
                precision: 18,
                scale: 3,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "BaseRate",
                table: "ZansiDispatchShipments",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ChargedWeightKg",
                table: "ZansiDispatchShipments",
                type: "decimal(18,3)",
                precision: 18,
                scale: 3,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ExpectedCollectionDate",
                table: "ZansiDispatchShipments",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ExpectedDeliveryFrom",
                table: "ZansiDispatchShipments",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ExpectedDeliveryTo",
                table: "ZansiDispatchShipments",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PackageTrackingReference",
                table: "ZansiDispatchShipments",
                type: "nvarchar(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProviderStatusMessage",
                table: "ZansiDispatchShipments",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ShipmentBookedEmailSentAtUtc",
                table: "ZansiDispatchShipments",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "VolumetricWeightKg",
                table: "ZansiDispatchShipments",
                type: "decimal(18,3)",
                precision: 18,
                scale: 3,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ActualWeightKg",
                table: "ZansiDispatchShipments");

            migrationBuilder.DropColumn(
                name: "BaseRate",
                table: "ZansiDispatchShipments");

            migrationBuilder.DropColumn(
                name: "ChargedWeightKg",
                table: "ZansiDispatchShipments");

            migrationBuilder.DropColumn(
                name: "ExpectedCollectionDate",
                table: "ZansiDispatchShipments");

            migrationBuilder.DropColumn(
                name: "ExpectedDeliveryFrom",
                table: "ZansiDispatchShipments");

            migrationBuilder.DropColumn(
                name: "ExpectedDeliveryTo",
                table: "ZansiDispatchShipments");

            migrationBuilder.DropColumn(
                name: "PackageTrackingReference",
                table: "ZansiDispatchShipments");

            migrationBuilder.DropColumn(
                name: "ProviderStatusMessage",
                table: "ZansiDispatchShipments");

            migrationBuilder.DropColumn(
                name: "ShipmentBookedEmailSentAtUtc",
                table: "ZansiDispatchShipments");

            migrationBuilder.DropColumn(
                name: "VolumetricWeightKg",
                table: "ZansiDispatchShipments");
        }
    }
}
