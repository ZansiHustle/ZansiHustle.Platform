using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ZansiHustle.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddReferralSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AffiliateProfiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReferralCode = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    ClickCount = table.Column<int>(type: "int", nullable: false),
                    JoinCount = table.Column<int>(type: "int", nullable: false),
                    ConversionCount = table.Column<int>(type: "int", nullable: false),
                    CommissionRate = table.Column<decimal>(type: "decimal(6,4)", precision: 6, scale: 4, nullable: true),
                    Tier = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AffiliateProfiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AffiliateProfiles_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ReferralClicks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AffiliateProfileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReferralCode = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    LandingPath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    UserAgent = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IpHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    ConvertedUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ClickedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReferralClicks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReferralClicks_AffiliateProfiles_AffiliateProfileId",
                        column: x => x.AffiliateProfileId,
                        principalTable: "AffiliateProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserReferrals",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AffiliateProfileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReferrerUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReferredUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReferralCodeUsed = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    ReferralType = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    SourcePath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    JoinedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ConvertedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserReferrals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserReferrals_AffiliateProfiles_AffiliateProfileId",
                        column: x => x.AffiliateProfileId,
                        principalTable: "AffiliateProfiles",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_AffiliateProfiles_IsActive",
                table: "AffiliateProfiles",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_AffiliateProfiles_ReferralCode",
                table: "AffiliateProfiles",
                column: "ReferralCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AffiliateProfiles_UserId",
                table: "AffiliateProfiles",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReferralClicks_AffiliateProfileId",
                table: "ReferralClicks",
                column: "AffiliateProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_ReferralClicks_ClickedAtUtc",
                table: "ReferralClicks",
                column: "ClickedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_ReferralClicks_ReferralCode",
                table: "ReferralClicks",
                column: "ReferralCode");

            migrationBuilder.CreateIndex(
                name: "IX_UserReferrals_AffiliateProfileId",
                table: "UserReferrals",
                column: "AffiliateProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_UserReferrals_ReferralCodeUsed",
                table: "UserReferrals",
                column: "ReferralCodeUsed");

            migrationBuilder.CreateIndex(
                name: "IX_UserReferrals_ReferredUserId_ReferralType",
                table: "UserReferrals",
                columns: new[] { "ReferredUserId", "ReferralType" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserReferrals_ReferrerUserId",
                table: "UserReferrals",
                column: "ReferrerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserReferrals_Status",
                table: "UserReferrals",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ReferralClicks");

            migrationBuilder.DropTable(
                name: "UserReferrals");

            migrationBuilder.DropTable(
                name: "AffiliateProfiles");
        }
    }
}
