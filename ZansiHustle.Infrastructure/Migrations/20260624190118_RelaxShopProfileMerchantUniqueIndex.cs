using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ZansiHustle.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RelaxShopProfileMerchantUniqueIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UX_ShopProfiles_Merchant_Active",
                table: "ShopProfiles");

            migrationBuilder.CreateIndex(
                name: "IX_ShopProfiles_Merchant_Status",
                table: "ShopProfiles",
                columns: new[] { "MerchantId", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ShopProfiles_Merchant_Status",
                table: "ShopProfiles");

            migrationBuilder.CreateIndex(
                name: "UX_ShopProfiles_Merchant_Active",
                table: "ShopProfiles",
                column: "MerchantId",
                unique: true,
                filter: "[Status] <> 3");
        }
    }
}
