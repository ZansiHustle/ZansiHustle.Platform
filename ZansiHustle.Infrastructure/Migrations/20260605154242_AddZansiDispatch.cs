using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ZansiHustle.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddZansiDispatch : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "DeliveryFee",
                table: "Orders",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DeliveryQuoteOptionId",
                table: "Orders",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ZansiDispatchLedgerEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ShipmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    OrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    EntryType = table.Column<int>(type: "int", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Direction = table.Column<int>(type: "int", nullable: false),
                    BalanceImpact = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Reference = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ZansiDispatchLedgerEntries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ZansiDispatchProviderRequestLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProviderType = table.Column<int>(type: "int", nullable: false),
                    Operation = table.Column<int>(type: "int", nullable: false),
                    RequestJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ResponseJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsSuccess = table.Column<bool>(type: "bit", nullable: false),
                    ErrorMessage = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    DurationMs = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ZansiDispatchProviderRequestLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ZansiDispatchQuotes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ListingId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ShopId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    MerchantId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    BuyerProvince = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    BuyerCity = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    BuyerAddressSummary = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    SellerProvince = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    SellerCity = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    SellerAddressSummary = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ItemSizeCategory = table.Column<int>(type: "int", nullable: true),
                    EstimatedWeightKg = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: true),
                    DistanceKm = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ZansiDispatchQuotes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ZansiDispatchSettings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Key = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Value = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ZansiDispatchSettings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ZansiDispatchShipments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    QuoteId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    QuoteOptionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MerchantId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ShopId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ProviderType = table.Column<int>(type: "int", nullable: false),
                    ServiceLevel = table.Column<int>(type: "int", nullable: false),
                    QuotedDeliveryFee = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    ActualCourierCost = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    SurplusAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    DeficitAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    NetAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    ReconciliationStatus = table.Column<int>(type: "int", nullable: false),
                    TrackingNumber = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    ProviderShipmentReference = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CourierReference = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    PickupAddressSummary = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    DropoffAddressSummary = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    PickupScheduledAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeliveredAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ZansiDispatchShipments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ZansiDispatchQuoteOptions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    QuoteId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProviderType = table.Column<int>(type: "int", nullable: false),
                    ProviderQuoteReference = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ServiceLevel = table.Column<int>(type: "int", nullable: false),
                    Label = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    QuotedAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Currency = table.Column<string>(type: "nvarchar(8)", maxLength: 8, nullable: false),
                    EstimatedDeliveryDaysMin = table.Column<int>(type: "int", nullable: true),
                    EstimatedDeliveryDaysMax = table.Column<int>(type: "int", nullable: true),
                    EstimateBreakdownJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RawProviderResponseJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsSelected = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ZansiDispatchQuoteOptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ZansiDispatchQuoteOptions_ZansiDispatchQuotes_QuoteId",
                        column: x => x.QuoteId,
                        principalTable: "ZansiDispatchQuotes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ZansiDispatchLedgerEntries_CreatedAt",
                table: "ZansiDispatchLedgerEntries",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ZansiDispatchLedgerEntries_EntryType",
                table: "ZansiDispatchLedgerEntries",
                column: "EntryType");

            migrationBuilder.CreateIndex(
                name: "IX_ZansiDispatchLedgerEntries_OrderId",
                table: "ZansiDispatchLedgerEntries",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_ZansiDispatchLedgerEntries_ShipmentId",
                table: "ZansiDispatchLedgerEntries",
                column: "ShipmentId");

            migrationBuilder.CreateIndex(
                name: "IX_ZansiDispatchProviderRequestLogs_CreatedAt",
                table: "ZansiDispatchProviderRequestLogs",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ZansiDispatchProviderRequestLogs_Operation",
                table: "ZansiDispatchProviderRequestLogs",
                column: "Operation");

            migrationBuilder.CreateIndex(
                name: "IX_ZansiDispatchProviderRequestLogs_ProviderType",
                table: "ZansiDispatchProviderRequestLogs",
                column: "ProviderType");

            migrationBuilder.CreateIndex(
                name: "IX_ZansiDispatchQuoteOptions_QuoteId",
                table: "ZansiDispatchQuoteOptions",
                column: "QuoteId");

            migrationBuilder.CreateIndex(
                name: "IX_ZansiDispatchQuoteOptions_QuoteId_IsSelected",
                table: "ZansiDispatchQuoteOptions",
                columns: new[] { "QuoteId", "IsSelected" });

            migrationBuilder.CreateIndex(
                name: "IX_ZansiDispatchQuotes_CreatedAt",
                table: "ZansiDispatchQuotes",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ZansiDispatchQuotes_MerchantId",
                table: "ZansiDispatchQuotes",
                column: "MerchantId");

            migrationBuilder.CreateIndex(
                name: "IX_ZansiDispatchQuotes_OrderId",
                table: "ZansiDispatchQuotes",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_ZansiDispatchQuotes_Status",
                table: "ZansiDispatchQuotes",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_ZansiDispatchQuotes_UserId",
                table: "ZansiDispatchQuotes",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ZansiDispatchSettings_Key",
                table: "ZansiDispatchSettings",
                column: "Key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ZansiDispatchShipments_CreatedAt",
                table: "ZansiDispatchShipments",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ZansiDispatchShipments_MerchantId",
                table: "ZansiDispatchShipments",
                column: "MerchantId");

            migrationBuilder.CreateIndex(
                name: "IX_ZansiDispatchShipments_OrderId",
                table: "ZansiDispatchShipments",
                column: "OrderId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ZansiDispatchShipments_ReconciliationStatus",
                table: "ZansiDispatchShipments",
                column: "ReconciliationStatus");

            migrationBuilder.CreateIndex(
                name: "IX_ZansiDispatchShipments_Status",
                table: "ZansiDispatchShipments",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_ZansiDispatchShipments_UserId",
                table: "ZansiDispatchShipments",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ZansiDispatchLedgerEntries");

            migrationBuilder.DropTable(
                name: "ZansiDispatchProviderRequestLogs");

            migrationBuilder.DropTable(
                name: "ZansiDispatchQuoteOptions");

            migrationBuilder.DropTable(
                name: "ZansiDispatchSettings");

            migrationBuilder.DropTable(
                name: "ZansiDispatchShipments");

            migrationBuilder.DropTable(
                name: "ZansiDispatchQuotes");

            migrationBuilder.DropColumn(
                name: "DeliveryFee",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "DeliveryQuoteOptionId",
                table: "Orders");
        }
    }
}
