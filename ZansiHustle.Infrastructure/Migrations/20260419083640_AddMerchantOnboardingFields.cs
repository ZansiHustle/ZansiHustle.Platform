using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ZansiHustle.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMerchantOnboardingFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "IdNumber",
                table: "Merchants",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReferralCode",
                table: "Merchants",
                type: "nvarchar(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ReferrerUserId",
                table: "Merchants",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SocialHandle",
                table: "Merchants",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "WhatsAppNumber",
                table: "Merchants",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Merchants_ReferralCode",
                table: "Merchants",
                column: "ReferralCode");

            migrationBuilder.CreateIndex(
                name: "IX_Merchants_ReferrerUserId",
                table: "Merchants",
                column: "ReferrerUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Merchants_ReferralCode",
                table: "Merchants");

            migrationBuilder.DropIndex(
                name: "IX_Merchants_ReferrerUserId",
                table: "Merchants");

            migrationBuilder.DropColumn(
                name: "IdNumber",
                table: "Merchants");

            migrationBuilder.DropColumn(
                name: "ReferralCode",
                table: "Merchants");

            migrationBuilder.DropColumn(
                name: "ReferrerUserId",
                table: "Merchants");

            migrationBuilder.DropColumn(
                name: "SocialHandle",
                table: "Merchants");

            migrationBuilder.DropColumn(
                name: "WhatsAppNumber",
                table: "Merchants");
        }
    }
}
