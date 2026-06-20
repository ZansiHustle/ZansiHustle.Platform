using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ZansiHustle.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddShopThemeBackgroundMode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ThemeBackgroundMode",
                table: "ShopProfiles",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "light");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ThemeBackgroundMode",
                table: "ShopProfiles");
        }
    }
}
