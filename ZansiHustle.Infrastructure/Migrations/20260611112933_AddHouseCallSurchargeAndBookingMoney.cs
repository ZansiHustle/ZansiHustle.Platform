using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ZansiHustle.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddHouseCallSurchargeAndBookingMoney : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "BaseServiceAmount",
                table: "ServiceBookings",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "HouseCallSurcharge",
                table: "ServiceBookings",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "TravelFee",
                table: "ServiceBookings",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "HouseCallSurchargeAmount",
                table: "Listings",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BaseServiceAmount",
                table: "ServiceBookings");

            migrationBuilder.DropColumn(
                name: "HouseCallSurcharge",
                table: "ServiceBookings");

            migrationBuilder.DropColumn(
                name: "TravelFee",
                table: "ServiceBookings");

            migrationBuilder.DropColumn(
                name: "HouseCallSurchargeAmount",
                table: "Listings");
        }
    }
}
