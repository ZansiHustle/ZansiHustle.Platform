using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ZansiHustle.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDispatchBookingFailureTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "BookingAttemptCount",
                table: "ZansiDispatchShipments",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "FailureReason",
                table: "ZansiDispatchShipments",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastBookingAttemptAtUtc",
                table: "ZansiDispatchShipments",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BookingAttemptCount",
                table: "ZansiDispatchShipments");

            migrationBuilder.DropColumn(
                name: "FailureReason",
                table: "ZansiDispatchShipments");

            migrationBuilder.DropColumn(
                name: "LastBookingAttemptAtUtc",
                table: "ZansiDispatchShipments");
        }
    }
}
