using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ZansiHustle.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SellerLeadConfigChange : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SellerLeads_Users_UserId",
                table: "SellerLeads");

            migrationBuilder.DropIndex(
                name: "IX_SellerLeads_UserId",
                table: "SellerLeads");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "SellerLeads");

            migrationBuilder.AddForeignKey(
                name: "FK_SellerLeads_Users_AssignedUserId",
                table: "SellerLeads",
                column: "AssignedUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SellerLeads_Users_AssignedUserId",
                table: "SellerLeads");

            migrationBuilder.AddColumn<Guid>(
                name: "UserId",
                table: "SellerLeads",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_SellerLeads_UserId",
                table: "SellerLeads",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_SellerLeads_Users_UserId",
                table: "SellerLeads",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id");
        }
    }
}
