using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ZansiHustle.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMoreUserRoles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Roles",
                columns: new[] { "Id", "Name", "NormalizedName", "ConcurrencyStamp" },
                values: new object[,]
                {
                    { "F22A9D2B-9A6E-4A77-9E62-7C7EAA1A1001", "Affiliate", "AFFILIATE", null },
                    { "F22A9D2B-9A6E-4A77-9E62-7C7EAA1A1002", "Merchant", "MERCHANT", null },
                    { "F22A9D2B-9A6E-4A77-9E62-7C7EAA1A1003", "MarketplaceSeller", "MARKETPLACESELLER", null },
                    { "F22A9D2B-9A6E-4A77-9E62-7C7EAA1A1004", "Agent", "AGENT", null },
                    { "F22A9D2B-9A6E-4A77-9E62-7C7EAA1A1005", "TeamMember", "TEAMMEMBER", null },
                    { "F22A9D2B-9A6E-4A77-9E62-7C7EAA1A1006", "MarketplaceGrowthAssociate", "MARKETPLACEGROWTHASSOCIATE", null },
                    { "F22A9D2B-9A6E-4A77-9E62-7C7EAA1A1007", "SocialMediaManager", "SOCIALMEDIAMANAGER", null },
                    { "F22A9D2B-9A6E-4A77-9E62-7C7EAA1A1008", "ContentCreator", "CONTENTCREATOR", null },
                    { "F22A9D2B-9A6E-4A77-9E62-7C7EAA1A1009", "Partner", "PARTNER", null },
                    { "F22A9D2B-9A6E-4A77-9E62-7C7EAA1A1010", "MarketingManager", "MARKETINGMANAGER", null }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: "F22A9D2B-9A6E-4A77-9E62-7C7EAA1A1001");

                    migrationBuilder.DeleteData(
                        table: "Roles",
                        keyColumn: "Id",
                        keyValue: "F22A9D2B-9A6E-4A77-9E62-7C7EAA1A1002");

                    migrationBuilder.DeleteData(
                        table: "Roles",
                        keyColumn: "Id",
                        keyValue: "F22A9D2B-9A6E-4A77-9E62-7C7EAA1A1003");

                    migrationBuilder.DeleteData(
                        table: "Roles",
                        keyColumn: "Id",
                        keyValue: "F22A9D2B-9A6E-4A77-9E62-7C7EAA1A1004");

                    migrationBuilder.DeleteData(
                        table: "Roles",
                        keyColumn: "Id",
                        keyValue: "F22A9D2B-9A6E-4A77-9E62-7C7EAA1A1005");

                    migrationBuilder.DeleteData(
                        table: "Roles",
                        keyColumn: "Id",
                        keyValue: "F22A9D2B-9A6E-4A77-9E62-7C7EAA1A1006");

                    migrationBuilder.DeleteData(
                        table: "Roles",
                        keyColumn: "Id",
                        keyValue: "F22A9D2B-9A6E-4A77-9E62-7C7EAA1A1007");

                    migrationBuilder.DeleteData(
                        table: "Roles",
                        keyColumn: "Id",
                        keyValue: "F22A9D2B-9A6E-4A77-9E62-7C7EAA1A1008");

                    migrationBuilder.DeleteData(
                        table: "Roles",
                        keyColumn: "Id",
                        keyValue: "F22A9D2B-9A6E-4A77-9E62-7C7EAA1A1009");

                    migrationBuilder.DeleteData(
                        table: "Roles",
                        keyColumn: "Id",
                        keyValue: "F22A9D2B-9A6E-4A77-9E62-7C7EAA1A1010");
        }
    }
}
