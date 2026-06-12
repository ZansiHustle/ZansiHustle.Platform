using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ZansiHustle.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AllowWalletPaymentReapplyIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_WalletTransactions_Type_ReferenceType_ReferenceId",
                table: "WalletTransactions");

            migrationBuilder.CreateIndex(
                name: "IX_WalletTransactions_Type_ReferenceType_ReferenceId",
                table: "WalletTransactions",
                columns: new[] { "Type", "ReferenceType", "ReferenceId" },
                unique: true,
                filter: "[ReferenceId] IS NOT NULL AND [Type] NOT IN (100, 101)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_WalletTransactions_Type_ReferenceType_ReferenceId",
                table: "WalletTransactions");

            migrationBuilder.CreateIndex(
                name: "IX_WalletTransactions_Type_ReferenceType_ReferenceId",
                table: "WalletTransactions",
                columns: new[] { "Type", "ReferenceType", "ReferenceId" },
                unique: true,
                filter: "[ReferenceId] IS NOT NULL");
        }
    }
}
