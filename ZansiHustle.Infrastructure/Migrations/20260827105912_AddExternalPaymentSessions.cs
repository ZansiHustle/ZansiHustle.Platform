using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ZansiHustle.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddExternalPaymentSessions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ExternalPaymentSessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ShopCode = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ShopName = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    ExternalOrderId = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    ExternalOrderNumber = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Currency = table.Column<string>(type: "nvarchar(8)", maxLength: 8, nullable: false),
                    CustomerEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ReturnUrl = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    CallbackUrl = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    Provider = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    ProviderReference = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    ProviderAuthorizationUrl = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ProviderAccessCode = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    FailureReason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    IsTest = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PaidAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CallbackDeliveredAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CallbackAttemptCount = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    LastCallbackError = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExternalPaymentSessions", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ExternalPaymentSessions_ActiveShopOrder",
                table: "ExternalPaymentSessions",
                columns: new[] { "ShopCode", "ExternalOrderId" },
                unique: true,
                filter: "[Status] <> 5 AND [Status] <> 6 AND [Status] <> 7");

            migrationBuilder.CreateIndex(
                name: "IX_ExternalPaymentSessions_CreatedAtUtc",
                table: "ExternalPaymentSessions",
                column: "CreatedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_ExternalPaymentSessions_IsTest",
                table: "ExternalPaymentSessions",
                column: "IsTest");

            migrationBuilder.CreateIndex(
                name: "IX_ExternalPaymentSessions_ProviderReference",
                table: "ExternalPaymentSessions",
                column: "ProviderReference",
                unique: true,
                filter: "[ProviderReference] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ExternalPaymentSessions_ShopCode",
                table: "ExternalPaymentSessions",
                column: "ShopCode");

            migrationBuilder.CreateIndex(
                name: "IX_ExternalPaymentSessions_Status",
                table: "ExternalPaymentSessions",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ExternalPaymentSessions");
        }
    }
}
