using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ZansiHustle.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddShipmentActionLog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ZansiDispatchShipmentActions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ShipmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ActorUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Actor = table.Column<int>(type: "int", nullable: false),
                    ActionType = table.Column<int>(type: "int", nullable: false),
                    OldShipmentStatus = table.Column<int>(type: "int", nullable: true),
                    NewShipmentStatus = table.Column<int>(type: "int", nullable: true),
                    ProviderStatusBefore = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    ProviderStatusAfter = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    Reason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    SafeProviderResponseJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CorrelationId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ZansiDispatchShipmentActions", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ZansiDispatchShipmentActions_OrderId",
                table: "ZansiDispatchShipmentActions",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_ZansiDispatchShipmentActions_ShipmentId",
                table: "ZansiDispatchShipmentActions",
                column: "ShipmentId");

            migrationBuilder.CreateIndex(
                name: "IX_ZansiDispatchShipmentActions_ShipmentId_CreatedAtUtc",
                table: "ZansiDispatchShipmentActions",
                columns: new[] { "ShipmentId", "CreatedAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ZansiDispatchShipmentActions");
        }
    }
}
