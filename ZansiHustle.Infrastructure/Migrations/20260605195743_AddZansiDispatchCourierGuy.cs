using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ZansiHustle.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddZansiDispatchCourierGuy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "LabelUrl",
                table: "ZansiDispatchShipments",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LabelUrlExpiresAt",
                table: "ZansiDispatchShipments",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProviderShipmentId",
                table: "ZansiDispatchShipments",
                type: "nvarchar(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RawProviderResponseJson",
                table: "ZansiDispatchShipments",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ServiceLevelCode",
                table: "ZansiDispatchShipments",
                type: "nvarchar(60)",
                maxLength: 60,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ServiceLevelName",
                table: "ZansiDispatchShipments",
                type: "nvarchar(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ShortTrackingReference",
                table: "ZansiDispatchShipments",
                type: "nvarchar(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "BuyerAddressType",
                table: "ZansiDispatchQuotes",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BuyerCountry",
                table: "ZansiDispatchQuotes",
                type: "nvarchar(4)",
                maxLength: 4,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "BuyerLat",
                table: "ZansiDispatchQuotes",
                type: "decimal(18,7)",
                precision: 18,
                scale: 7,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "BuyerLng",
                table: "ZansiDispatchQuotes",
                type: "decimal(18,7)",
                precision: 18,
                scale: 7,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BuyerLocalArea",
                table: "ZansiDispatchQuotes",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BuyerPostalCode",
                table: "ZansiDispatchQuotes",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BuyerStreetAddress",
                table: "ZansiDispatchQuotes",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "DeclaredValue",
                table: "ZansiDispatchQuotes",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ParcelDescription",
                table: "ZansiDispatchQuotes",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SellerAddressType",
                table: "ZansiDispatchQuotes",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SellerCountry",
                table: "ZansiDispatchQuotes",
                type: "nvarchar(4)",
                maxLength: 4,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "SellerLat",
                table: "ZansiDispatchQuotes",
                type: "decimal(18,7)",
                precision: 18,
                scale: 7,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "SellerLng",
                table: "ZansiDispatchQuotes",
                type: "decimal(18,7)",
                precision: 18,
                scale: 7,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SellerLocalArea",
                table: "ZansiDispatchQuotes",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SellerPostalCode",
                table: "ZansiDispatchQuotes",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SellerStreetAddress",
                table: "ZansiDispatchQuotes",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "SubmittedHeightCm",
                table: "ZansiDispatchQuotes",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "SubmittedLengthCm",
                table: "ZansiDispatchQuotes",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "SubmittedWidthCm",
                table: "ZansiDispatchQuotes",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProviderServiceLevelId",
                table: "ZansiDispatchQuoteOptions",
                type: "nvarchar(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ServiceLevelCode",
                table: "ZansiDispatchQuoteOptions",
                type: "nvarchar(60)",
                maxLength: 60,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ServiceLevelName",
                table: "ZansiDispatchQuoteOptions",
                type: "nvarchar(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TotalAmount",
                table: "ZansiDispatchQuoteOptions",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "VatAmount",
                table: "ZansiDispatchQuoteOptions",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "StatusCode",
                table: "ZansiDispatchProviderRequestLogs",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ZansiDispatchShipmentEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ShipmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProviderType = table.Column<int>(type: "int", nullable: false),
                    ProviderEventId = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    ProviderStatus = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    InternalStatus = table.Column<int>(type: "int", nullable: false),
                    Message = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Location = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    EventTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RawEventJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ZansiDispatchShipmentEvents", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ZansiDispatchShipments_ProviderShipmentId",
                table: "ZansiDispatchShipments",
                column: "ProviderShipmentId");

            migrationBuilder.CreateIndex(
                name: "IX_ZansiDispatchShipments_ShortTrackingReference",
                table: "ZansiDispatchShipments",
                column: "ShortTrackingReference");

            migrationBuilder.CreateIndex(
                name: "IX_ZansiDispatchShipments_TrackingNumber",
                table: "ZansiDispatchShipments",
                column: "TrackingNumber");

            migrationBuilder.CreateIndex(
                name: "IX_ZansiDispatchShipmentEvents_ShipmentId",
                table: "ZansiDispatchShipmentEvents",
                column: "ShipmentId");

            migrationBuilder.CreateIndex(
                name: "IX_ZansiDispatchShipmentEvents_ShipmentId_EventTime",
                table: "ZansiDispatchShipmentEvents",
                columns: new[] { "ShipmentId", "EventTime" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ZansiDispatchShipmentEvents");

            migrationBuilder.DropIndex(
                name: "IX_ZansiDispatchShipments_ProviderShipmentId",
                table: "ZansiDispatchShipments");

            migrationBuilder.DropIndex(
                name: "IX_ZansiDispatchShipments_ShortTrackingReference",
                table: "ZansiDispatchShipments");

            migrationBuilder.DropIndex(
                name: "IX_ZansiDispatchShipments_TrackingNumber",
                table: "ZansiDispatchShipments");

            migrationBuilder.DropColumn(
                name: "LabelUrl",
                table: "ZansiDispatchShipments");

            migrationBuilder.DropColumn(
                name: "LabelUrlExpiresAt",
                table: "ZansiDispatchShipments");

            migrationBuilder.DropColumn(
                name: "ProviderShipmentId",
                table: "ZansiDispatchShipments");

            migrationBuilder.DropColumn(
                name: "RawProviderResponseJson",
                table: "ZansiDispatchShipments");

            migrationBuilder.DropColumn(
                name: "ServiceLevelCode",
                table: "ZansiDispatchShipments");

            migrationBuilder.DropColumn(
                name: "ServiceLevelName",
                table: "ZansiDispatchShipments");

            migrationBuilder.DropColumn(
                name: "ShortTrackingReference",
                table: "ZansiDispatchShipments");

            migrationBuilder.DropColumn(
                name: "BuyerAddressType",
                table: "ZansiDispatchQuotes");

            migrationBuilder.DropColumn(
                name: "BuyerCountry",
                table: "ZansiDispatchQuotes");

            migrationBuilder.DropColumn(
                name: "BuyerLat",
                table: "ZansiDispatchQuotes");

            migrationBuilder.DropColumn(
                name: "BuyerLng",
                table: "ZansiDispatchQuotes");

            migrationBuilder.DropColumn(
                name: "BuyerLocalArea",
                table: "ZansiDispatchQuotes");

            migrationBuilder.DropColumn(
                name: "BuyerPostalCode",
                table: "ZansiDispatchQuotes");

            migrationBuilder.DropColumn(
                name: "BuyerStreetAddress",
                table: "ZansiDispatchQuotes");

            migrationBuilder.DropColumn(
                name: "DeclaredValue",
                table: "ZansiDispatchQuotes");

            migrationBuilder.DropColumn(
                name: "ParcelDescription",
                table: "ZansiDispatchQuotes");

            migrationBuilder.DropColumn(
                name: "SellerAddressType",
                table: "ZansiDispatchQuotes");

            migrationBuilder.DropColumn(
                name: "SellerCountry",
                table: "ZansiDispatchQuotes");

            migrationBuilder.DropColumn(
                name: "SellerLat",
                table: "ZansiDispatchQuotes");

            migrationBuilder.DropColumn(
                name: "SellerLng",
                table: "ZansiDispatchQuotes");

            migrationBuilder.DropColumn(
                name: "SellerLocalArea",
                table: "ZansiDispatchQuotes");

            migrationBuilder.DropColumn(
                name: "SellerPostalCode",
                table: "ZansiDispatchQuotes");

            migrationBuilder.DropColumn(
                name: "SellerStreetAddress",
                table: "ZansiDispatchQuotes");

            migrationBuilder.DropColumn(
                name: "SubmittedHeightCm",
                table: "ZansiDispatchQuotes");

            migrationBuilder.DropColumn(
                name: "SubmittedLengthCm",
                table: "ZansiDispatchQuotes");

            migrationBuilder.DropColumn(
                name: "SubmittedWidthCm",
                table: "ZansiDispatchQuotes");

            migrationBuilder.DropColumn(
                name: "ProviderServiceLevelId",
                table: "ZansiDispatchQuoteOptions");

            migrationBuilder.DropColumn(
                name: "ServiceLevelCode",
                table: "ZansiDispatchQuoteOptions");

            migrationBuilder.DropColumn(
                name: "ServiceLevelName",
                table: "ZansiDispatchQuoteOptions");

            migrationBuilder.DropColumn(
                name: "TotalAmount",
                table: "ZansiDispatchQuoteOptions");

            migrationBuilder.DropColumn(
                name: "VatAmount",
                table: "ZansiDispatchQuoteOptions");

            migrationBuilder.DropColumn(
                name: "StatusCode",
                table: "ZansiDispatchProviderRequestLogs");
        }
    }
}
