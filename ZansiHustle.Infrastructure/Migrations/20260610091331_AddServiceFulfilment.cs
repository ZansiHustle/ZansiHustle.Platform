using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ZansiHustle.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddServiceFulfilment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "AllowsHouseCall",
                table: "Listings",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "AllowsProviderLocation",
                table: "Listings",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "BufferMinutes",
                table: "Listings",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "FreeTravelRadiusKm",
                table: "Listings",
                type: "decimal(9,2)",
                precision: 9,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "FulfilmentMode",
                table: "Listings",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "LeadTimeHours",
                table: "Listings",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "MaxTravelDistanceKm",
                table: "Listings",
                type: "decimal(9,2)",
                precision: 9,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProviderAddressLine1",
                table: "Listings",
                type: "nvarchar(250)",
                maxLength: 250,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProviderAddressLine2",
                table: "Listings",
                type: "nvarchar(250)",
                maxLength: 250,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProviderCity",
                table: "Listings",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ProviderLatitude",
                table: "Listings",
                type: "decimal(9,6)",
                precision: 9,
                scale: 6,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProviderLocationName",
                table: "Listings",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ProviderLongitude",
                table: "Listings",
                type: "decimal(9,6)",
                precision: 9,
                scale: 6,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProviderPostalCode",
                table: "Listings",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProviderProvince",
                table: "Listings",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TravelFeeFlatAmount",
                table: "Listings",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TravelFeeMaximum",
                table: "Listings",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TravelFeeMinimum",
                table: "Listings",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TravelFeePerKm",
                table: "Listings",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TravelFeeType",
                table: "Listings",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AllowsHouseCall",
                table: "Listings");

            migrationBuilder.DropColumn(
                name: "AllowsProviderLocation",
                table: "Listings");

            migrationBuilder.DropColumn(
                name: "BufferMinutes",
                table: "Listings");

            migrationBuilder.DropColumn(
                name: "FreeTravelRadiusKm",
                table: "Listings");

            migrationBuilder.DropColumn(
                name: "FulfilmentMode",
                table: "Listings");

            migrationBuilder.DropColumn(
                name: "LeadTimeHours",
                table: "Listings");

            migrationBuilder.DropColumn(
                name: "MaxTravelDistanceKm",
                table: "Listings");

            migrationBuilder.DropColumn(
                name: "ProviderAddressLine1",
                table: "Listings");

            migrationBuilder.DropColumn(
                name: "ProviderAddressLine2",
                table: "Listings");

            migrationBuilder.DropColumn(
                name: "ProviderCity",
                table: "Listings");

            migrationBuilder.DropColumn(
                name: "ProviderLatitude",
                table: "Listings");

            migrationBuilder.DropColumn(
                name: "ProviderLocationName",
                table: "Listings");

            migrationBuilder.DropColumn(
                name: "ProviderLongitude",
                table: "Listings");

            migrationBuilder.DropColumn(
                name: "ProviderPostalCode",
                table: "Listings");

            migrationBuilder.DropColumn(
                name: "ProviderProvince",
                table: "Listings");

            migrationBuilder.DropColumn(
                name: "TravelFeeFlatAmount",
                table: "Listings");

            migrationBuilder.DropColumn(
                name: "TravelFeeMaximum",
                table: "Listings");

            migrationBuilder.DropColumn(
                name: "TravelFeeMinimum",
                table: "Listings");

            migrationBuilder.DropColumn(
                name: "TravelFeePerKm",
                table: "Listings");

            migrationBuilder.DropColumn(
                name: "TravelFeeType",
                table: "Listings");
        }
    }
}
