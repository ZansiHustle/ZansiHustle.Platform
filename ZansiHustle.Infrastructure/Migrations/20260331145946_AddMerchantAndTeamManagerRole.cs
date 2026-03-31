using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ZansiHustle.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMerchantAndTeamManagerRole : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Merchants",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Type = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    KycStatus = table.Column<int>(type: "int", nullable: false),
                    IsPayoutEligible = table.Column<bool>(type: "bit", nullable: false),
                    OwnerUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ContactEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ContactPhoneNumber = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    Province = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    City = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    AddressLine1 = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    WebsiteUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    LogoUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    BannerUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    FollowersCount = table.Column<int>(type: "int", nullable: false),
                    Rating = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    TotalOrders = table.Column<int>(type: "int", nullable: false),
                    TotalRevenue = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Merchants", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Merchants_City",
                table: "Merchants",
                column: "City");

            migrationBuilder.CreateIndex(
                name: "IX_Merchants_Code",
                table: "Merchants",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Merchants_ContactEmail",
                table: "Merchants",
                column: "ContactEmail");

            migrationBuilder.CreateIndex(
                name: "IX_Merchants_ContactPhoneNumber",
                table: "Merchants",
                column: "ContactPhoneNumber");

            migrationBuilder.CreateIndex(
                name: "IX_Merchants_IsPayoutEligible",
                table: "Merchants",
                column: "IsPayoutEligible");

            migrationBuilder.CreateIndex(
                name: "IX_Merchants_KycStatus",
                table: "Merchants",
                column: "KycStatus");

            migrationBuilder.CreateIndex(
                name: "IX_Merchants_Name",
                table: "Merchants",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_Merchants_OwnerUserId",
                table: "Merchants",
                column: "OwnerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Merchants_Province",
                table: "Merchants",
                column: "Province");

            migrationBuilder.CreateIndex(
                name: "IX_Merchants_Status",
                table: "Merchants",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Merchants_Type",
                table: "Merchants",
                column: "Type");

            migrationBuilder.InsertData(
                table: "Roles",
                columns: new[] { "Id", "Name", "NormalizedName", "ConcurrencyStamp" },
                values: new object[,]
                {
                    { "F22A9D2B-9A6E-4A77-9E62-7C7EAA1A1011", "TeamManager", "TEAMMANAGER", null }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Merchants");

            migrationBuilder.DeleteData(
                        table: "Roles",
                        keyColumn: "Id",
                        keyValue: "F22A9D2B-9A6E-4A77-9E62-7C7EAA1A1011");
        }
    }
}
