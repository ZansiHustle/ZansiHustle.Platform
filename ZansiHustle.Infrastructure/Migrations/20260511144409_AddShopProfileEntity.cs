using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ZansiHustle.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddShopProfileEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ShopProfiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MerchantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Slug = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    LogoUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    BannerUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ContactEmail = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ContactPhoneNumber = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    WhatsAppNumber = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    SellerCategoryId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SellerSubcategoryId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Province = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    City = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    AddressLine1 = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    SubscriptionStatus = table.Column<int>(type: "int", nullable: false),
                    EarlyAccessGrantedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EarlyAccessUntilUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SubscriptionStartedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SubscriptionEndsAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    BillingProvider = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    BillingReference = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ActivatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SuspendedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SuspensionReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShopProfiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ShopProfiles_Merchants_MerchantId",
                        column: x => x.MerchantId,
                        principalTable: "Merchants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ShopProfiles_Status_Created",
                table: "ShopProfiles",
                columns: new[] { "Status", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "UX_ShopProfiles_Merchant_Active",
                table: "ShopProfiles",
                column: "MerchantId",
                unique: true,
                filter: "[Status] <> 3");

            migrationBuilder.CreateIndex(
                name: "UX_ShopProfiles_Slug",
                table: "ShopProfiles",
                column: "Slug",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ShopProfiles");
        }
    }
}
