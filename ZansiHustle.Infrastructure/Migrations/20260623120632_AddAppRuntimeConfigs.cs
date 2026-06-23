using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ZansiHustle.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAppRuntimeConfigs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AppRuntimeConfigs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Key = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Category = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    ValueType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    BooleanValue = table.Column<bool>(type: "bit", nullable: false),
                    StringValue = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    NumberValue = table.Column<double>(type: "float", nullable: true),
                    JsonValue = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DisabledTitle = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: true),
                    DisabledMessage = table.Column<string>(type: "nvarchar(600)", maxLength: 600, nullable: true),
                    IsPublic = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppRuntimeConfigs", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AppRuntimeConfigs_Category",
                table: "AppRuntimeConfigs",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_AppRuntimeConfigs_IsPublic",
                table: "AppRuntimeConfigs",
                column: "IsPublic");

            migrationBuilder.CreateIndex(
                name: "IX_AppRuntimeConfigs_Key",
                table: "AppRuntimeConfigs",
                column: "Key",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AppRuntimeConfigs");
        }
    }
}
