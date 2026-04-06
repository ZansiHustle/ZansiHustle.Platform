using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ZansiHustle.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCateries : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SellerCategories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Slug = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SellerCategories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SellerSubcategories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SellerCategoryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Slug = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SellerSubcategories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SellerSubcategories_SellerCategories_SellerCategoryId",
                        column: x => x.SellerCategoryId,
                        principalTable: "SellerCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SellerCategories_Name",
                table: "SellerCategories",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SellerCategories_Slug",
                table: "SellerCategories",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SellerSubcategories_SellerCategoryId_Name",
                table: "SellerSubcategories",
                columns: new[] { "SellerCategoryId", "Name" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SellerSubcategories");

            migrationBuilder.DropTable(
                name: "SellerCategories");
        }
    }
}
