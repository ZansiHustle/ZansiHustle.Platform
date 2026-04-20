using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ZansiHustle.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMerchantBankColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BankAccountHolder",
                table: "Merchants",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BankAccountNumber",
                table: "Merchants",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BankAccountType",
                table: "Merchants",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BankBranchCode",
                table: "Merchants",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BankName",
                table: "Merchants",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "BankUpdatedAtUtc",
                table: "Merchants",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsBankVerified",
                table: "Merchants",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BankAccountHolder",
                table: "Merchants");

            migrationBuilder.DropColumn(
                name: "BankAccountNumber",
                table: "Merchants");

            migrationBuilder.DropColumn(
                name: "BankAccountType",
                table: "Merchants");

            migrationBuilder.DropColumn(
                name: "BankBranchCode",
                table: "Merchants");

            migrationBuilder.DropColumn(
                name: "BankName",
                table: "Merchants");

            migrationBuilder.DropColumn(
                name: "BankUpdatedAtUtc",
                table: "Merchants");

            migrationBuilder.DropColumn(
                name: "IsBankVerified",
                table: "Merchants");
        }
    }
}
