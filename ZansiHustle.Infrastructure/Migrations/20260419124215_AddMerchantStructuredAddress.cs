using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ZansiHustle.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMerchantStructuredAddress : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Country",
                table: "Merchants",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CountryCode",
                table: "Merchants",
                type: "nvarchar(2)",
                maxLength: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FormattedAddress",
                table: "Merchants",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GooglePlaceId",
                table: "Merchants",
                type: "nvarchar(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Latitude",
                table: "Merchants",
                type: "decimal(9,6)",
                precision: 9,
                scale: 6,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Longitude",
                table: "Merchants",
                type: "decimal(9,6)",
                precision: 9,
                scale: 6,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PostalCode",
                table: "Merchants",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Suburb",
                table: "Merchants",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Merchants_GooglePlaceId",
                table: "Merchants",
                column: "GooglePlaceId");

            migrationBuilder.CreateIndex(
                name: "IX_Merchants_Latitude_Longitude",
                table: "Merchants",
                columns: new[] { "Latitude", "Longitude" });

            migrationBuilder.CreateIndex(
                name: "IX_Merchants_PostalCode",
                table: "Merchants",
                column: "PostalCode");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Merchants_GooglePlaceId",
                table: "Merchants");

            migrationBuilder.DropIndex(
                name: "IX_Merchants_Latitude_Longitude",
                table: "Merchants");

            migrationBuilder.DropIndex(
                name: "IX_Merchants_PostalCode",
                table: "Merchants");

            migrationBuilder.DropColumn(
                name: "Country",
                table: "Merchants");

            migrationBuilder.DropColumn(
                name: "CountryCode",
                table: "Merchants");

            migrationBuilder.DropColumn(
                name: "FormattedAddress",
                table: "Merchants");

            migrationBuilder.DropColumn(
                name: "GooglePlaceId",
                table: "Merchants");

            migrationBuilder.DropColumn(
                name: "Latitude",
                table: "Merchants");

            migrationBuilder.DropColumn(
                name: "Longitude",
                table: "Merchants");

            migrationBuilder.DropColumn(
                name: "PostalCode",
                table: "Merchants");

            migrationBuilder.DropColumn(
                name: "Suburb",
                table: "Merchants");
        }
    }
}
