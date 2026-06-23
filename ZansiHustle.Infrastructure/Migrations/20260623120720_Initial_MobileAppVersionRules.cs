using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ZansiHustle.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Initial_MobileAppVersionRules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MobileAppVersionRules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Platform = table.Column<int>(type: "int", nullable: false),
                    Channel = table.Column<int>(type: "int", nullable: false),
                    LatestVersion = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    LatestBuildNumber = table.Column<int>(type: "int", nullable: false),
                    MinimumSupportedVersion = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    MinimumSupportedBuildNumber = table.Column<int>(type: "int", nullable: false),
                    UpdateRequired = table.Column<bool>(type: "bit", nullable: false),
                    UpdateAvailable = table.Column<bool>(type: "bit", nullable: false),
                    IsEnabled = table.Column<bool>(type: "bit", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Message = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    PrimaryButtonText = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    SecondaryButtonText = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    StoreUrl = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    ReleaseNotes = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MobileAppVersionRules", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MobileAppVersionRules_IsEnabled",
                table: "MobileAppVersionRules",
                column: "IsEnabled");

            migrationBuilder.CreateIndex(
                name: "IX_MobileAppVersionRules_Platform_Channel",
                table: "MobileAppVersionRules",
                columns: new[] { "Platform", "Channel" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MobileAppVersionRules");
        }
    }
}
