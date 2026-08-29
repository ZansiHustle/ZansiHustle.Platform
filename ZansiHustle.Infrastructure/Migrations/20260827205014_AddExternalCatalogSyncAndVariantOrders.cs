using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ZansiHustle.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddExternalCatalogSyncAndVariantOrders : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SkuSnapshot",
                table: "OrderItems",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "VariantId",
                table: "OrderItems",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VariantNameSnapshot",
                table: "OrderItems",
                type: "nvarchar(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExternalSourceCode",
                table: "ListingVariants",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExternalVariantId",
                table: "ListingVariants",
                type: "nvarchar(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExternalProductId",
                table: "Listings",
                type: "nvarchar(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExternalSourceCode",
                table: "Listings",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ExternalSourceUpdatedAtUtc",
                table: "Listings",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ExternalSyncRunId",
                table: "Listings",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ExternalSyncedAtUtc",
                table: "Listings",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ExternalCatalogSourceStatuses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SourceCode = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    LastActivityAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastActivityType = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    LastActivitySucceeded = table.Column<bool>(type: "bit", nullable: false),
                    LastError = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    TotalProductsUpserted = table.Column<int>(type: "int", nullable: false),
                    TotalProductsArchived = table.Column<int>(type: "int", nullable: false),
                    LastFullSyncRunId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LastFullSyncCompletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExternalCatalogSourceStatuses", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ExternalCatalogSyncRuns",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SourceCode = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    ProductsUpserted = table.Column<int>(type: "int", nullable: false),
                    VariantsUpserted = table.Column<int>(type: "int", nullable: false),
                    ProductsArchived = table.Column<int>(type: "int", nullable: false),
                    ErrorMessage = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    StartedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CompletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExternalCatalogSyncRuns", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ExternalCategoryMappings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SourceCode = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ExternalCategoryCode = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    SellerCategoryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SellerSubcategoryId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExternalCategoryMappings", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OrderItems_VariantId",
                table: "OrderItems",
                column: "VariantId");

            migrationBuilder.CreateIndex(
                name: "IX_ListingVariants_ExternalSource_ExternalVariant",
                table: "ListingVariants",
                columns: new[] { "ExternalSourceCode", "ExternalVariantId" },
                unique: true,
                filter: "[ExternalSourceCode] IS NOT NULL AND [ExternalVariantId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Listings_ExternalSource_ExternalProduct",
                table: "Listings",
                columns: new[] { "ExternalSourceCode", "ExternalProductId" },
                unique: true,
                filter: "[ExternalSourceCode] IS NOT NULL AND [ExternalProductId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Listings_ExternalSourceCode",
                table: "Listings",
                column: "ExternalSourceCode");

            migrationBuilder.CreateIndex(
                name: "IX_Listings_ExternalSyncRunId",
                table: "Listings",
                column: "ExternalSyncRunId");

            migrationBuilder.CreateIndex(
                name: "IX_ExternalCatalogSourceStatuses_SourceCode",
                table: "ExternalCatalogSourceStatuses",
                column: "SourceCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ExternalCatalogSyncRuns_SourceCode",
                table: "ExternalCatalogSyncRuns",
                column: "SourceCode");

            migrationBuilder.CreateIndex(
                name: "IX_ExternalCatalogSyncRuns_StartedAtUtc",
                table: "ExternalCatalogSyncRuns",
                column: "StartedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_ExternalCatalogSyncRuns_Status",
                table: "ExternalCatalogSyncRuns",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_ExternalCategoryMappings_SourceCode_ExternalCategoryCode",
                table: "ExternalCategoryMappings",
                columns: new[] { "SourceCode", "ExternalCategoryCode" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Listings_ExternalCatalogSyncRuns_ExternalSyncRunId",
                table: "Listings",
                column: "ExternalSyncRunId",
                principalTable: "ExternalCatalogSyncRuns",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_OrderItems_ListingVariants_VariantId",
                table: "OrderItems",
                column: "VariantId",
                principalTable: "ListingVariants",
                principalColumn: "Id",
                onDelete: ReferentialAction.NoAction);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Listings_ExternalCatalogSyncRuns_ExternalSyncRunId",
                table: "Listings");

            migrationBuilder.DropForeignKey(
                name: "FK_OrderItems_ListingVariants_VariantId",
                table: "OrderItems");

            migrationBuilder.DropTable(
                name: "ExternalCatalogSourceStatuses");

            migrationBuilder.DropTable(
                name: "ExternalCatalogSyncRuns");

            migrationBuilder.DropTable(
                name: "ExternalCategoryMappings");

            migrationBuilder.DropIndex(
                name: "IX_OrderItems_VariantId",
                table: "OrderItems");

            migrationBuilder.DropIndex(
                name: "IX_ListingVariants_ExternalSource_ExternalVariant",
                table: "ListingVariants");

            migrationBuilder.DropIndex(
                name: "IX_Listings_ExternalSource_ExternalProduct",
                table: "Listings");

            migrationBuilder.DropIndex(
                name: "IX_Listings_ExternalSourceCode",
                table: "Listings");

            migrationBuilder.DropIndex(
                name: "IX_Listings_ExternalSyncRunId",
                table: "Listings");

            migrationBuilder.DropColumn(
                name: "SkuSnapshot",
                table: "OrderItems");

            migrationBuilder.DropColumn(
                name: "VariantId",
                table: "OrderItems");

            migrationBuilder.DropColumn(
                name: "VariantNameSnapshot",
                table: "OrderItems");

            migrationBuilder.DropColumn(
                name: "ExternalSourceCode",
                table: "ListingVariants");

            migrationBuilder.DropColumn(
                name: "ExternalVariantId",
                table: "ListingVariants");

            migrationBuilder.DropColumn(
                name: "ExternalProductId",
                table: "Listings");

            migrationBuilder.DropColumn(
                name: "ExternalSourceCode",
                table: "Listings");

            migrationBuilder.DropColumn(
                name: "ExternalSourceUpdatedAtUtc",
                table: "Listings");

            migrationBuilder.DropColumn(
                name: "ExternalSyncRunId",
                table: "Listings");

            migrationBuilder.DropColumn(
                name: "ExternalSyncedAtUtc",
                table: "Listings");
        }
    }
}
