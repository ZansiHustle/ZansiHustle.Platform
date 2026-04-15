using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ZansiHustle.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddShopSlugAndCategoryToMerchant : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ReviewCount",
                table: "Merchants",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "SellerCategoryId",
                table: "Merchants",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SellerSubcategoryId",
                table: "Merchants",
                type: "uniqueidentifier",
                nullable: true);

            // Step 1: add Slug as NULLable so existing rows do not violate the unique index.
            migrationBuilder.AddColumn<string>(
                name: "Slug",
                table: "Merchants",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            // Step 2: backfill slugs for existing rows. Use the lowercased Id (guid) as
            // a guaranteed-unique seed; admins can rename slugs later if desired.
            migrationBuilder.Sql(
                "UPDATE [Merchants] SET [Slug] = LOWER(CAST([Id] AS NVARCHAR(50))) WHERE [Slug] IS NULL OR [Slug] = '';");

            // Step 3: enforce NOT NULL after backfill.
            migrationBuilder.AlterColumn<string>(
                name: "Slug",
                table: "Merchants",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200,
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Merchants_SellerCategoryId",
                table: "Merchants",
                column: "SellerCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Merchants_SellerSubcategoryId",
                table: "Merchants",
                column: "SellerSubcategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Merchants_Slug",
                table: "Merchants",
                column: "Slug",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Merchants_SellerCategories_SellerCategoryId",
                table: "Merchants",
                column: "SellerCategoryId",
                principalTable: "SellerCategories",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            // NoAction (default) avoids the "multiple cascade paths" error on SQL Server
            // because SellerCategory → SellerSubcategory already cascades, and an
            // additional cascade into Merchants would create a cycle.
            migrationBuilder.AddForeignKey(
                name: "FK_Merchants_SellerSubcategories_SellerSubcategoryId",
                table: "Merchants",
                column: "SellerSubcategoryId",
                principalTable: "SellerSubcategories",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Merchants_SellerCategories_SellerCategoryId",
                table: "Merchants");

            migrationBuilder.DropForeignKey(
                name: "FK_Merchants_SellerSubcategories_SellerSubcategoryId",
                table: "Merchants");

            migrationBuilder.DropIndex(
                name: "IX_Merchants_SellerCategoryId",
                table: "Merchants");

            migrationBuilder.DropIndex(
                name: "IX_Merchants_SellerSubcategoryId",
                table: "Merchants");

            migrationBuilder.DropIndex(
                name: "IX_Merchants_Slug",
                table: "Merchants");

            migrationBuilder.DropColumn(
                name: "ReviewCount",
                table: "Merchants");

            migrationBuilder.DropColumn(
                name: "SellerCategoryId",
                table: "Merchants");

            migrationBuilder.DropColumn(
                name: "SellerSubcategoryId",
                table: "Merchants");

            migrationBuilder.DropColumn(
                name: "Slug",
                table: "Merchants");
        }
    }
}
