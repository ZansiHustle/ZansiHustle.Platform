using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ZansiHustle.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class DropAgentsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SellerLeads_Agents_AgentId",
                table: "SellerLeads");

            migrationBuilder.DropTable(
                name: "Agents");

            migrationBuilder.RenameColumn(
                name: "AgentId",
                table: "SellerLeads",
                newName: "UserId");

            migrationBuilder.RenameIndex(
                name: "IX_SellerLeads_AgentId",
                table: "SellerLeads",
                newName: "IX_SellerLeads_UserId");

            migrationBuilder.AddColumn<Guid>(
                name: "AssignedUserId",
                table: "SellerLeads",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_SellerLeads_AssignedUserId",
                table: "SellerLeads",
                column: "AssignedUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_SellerLeads_Users_UserId",
                table: "SellerLeads",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SellerLeads_Users_UserId",
                table: "SellerLeads");

            migrationBuilder.DropIndex(
                name: "IX_SellerLeads_AssignedUserId",
                table: "SellerLeads");

            migrationBuilder.DropColumn(
                name: "AssignedUserId",
                table: "SellerLeads");

            migrationBuilder.RenameColumn(
                name: "UserId",
                table: "SellerLeads",
                newName: "AgentId");

            migrationBuilder.RenameIndex(
                name: "IX_SellerLeads_UserId",
                table: "SellerLeads",
                newName: "IX_SellerLeads_AgentId");

            migrationBuilder.CreateTable(
                name: "Agents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    City = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    FullName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    JoinedDateUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    PhoneNumber = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    Province = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    SocialHandle = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Agents", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Agents_Code",
                table: "Agents",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Agents_Email",
                table: "Agents",
                column: "Email");

            migrationBuilder.CreateIndex(
                name: "IX_Agents_PhoneNumber",
                table: "Agents",
                column: "PhoneNumber");

            migrationBuilder.CreateIndex(
                name: "IX_Agents_Status",
                table: "Agents",
                column: "Status");

            migrationBuilder.AddForeignKey(
                name: "FK_SellerLeads_Agents_AgentId",
                table: "SellerLeads",
                column: "AgentId",
                principalTable: "Agents",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
