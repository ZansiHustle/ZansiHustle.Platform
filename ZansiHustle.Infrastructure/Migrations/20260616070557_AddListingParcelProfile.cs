using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ZansiHustle.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddListingParcelProfile : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PackageContentsDescription",
                table: "Listings",
                type: "nvarchar(250)",
                maxLength: 250,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "PackageFragile",
                table: "Listings",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PackageHeightCm",
                table: "Listings",
                type: "decimal(9,2)",
                precision: 9,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PackageLengthCm",
                table: "Listings",
                type: "decimal(9,2)",
                precision: 9,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PackageSizeCategory",
                table: "Listings",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PackageWeightKg",
                table: "Listings",
                type: "decimal(9,3)",
                precision: 9,
                scale: 3,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PackageWidthCm",
                table: "Listings",
                type: "decimal(9,2)",
                precision: 9,
                scale: 2,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PackageContentsDescription",
                table: "Listings");

            migrationBuilder.DropColumn(
                name: "PackageFragile",
                table: "Listings");

            migrationBuilder.DropColumn(
                name: "PackageHeightCm",
                table: "Listings");

            migrationBuilder.DropColumn(
                name: "PackageLengthCm",
                table: "Listings");

            migrationBuilder.DropColumn(
                name: "PackageSizeCategory",
                table: "Listings");

            migrationBuilder.DropColumn(
                name: "PackageWeightKg",
                table: "Listings");

            migrationBuilder.DropColumn(
                name: "PackageWidthCm",
                table: "Listings");
        }
    }
}
