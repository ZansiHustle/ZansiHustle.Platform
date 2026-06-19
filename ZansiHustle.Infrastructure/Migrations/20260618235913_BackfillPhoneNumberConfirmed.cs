using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ZansiHustle.Infrastructure.Migrations
{
    /// <summary>
    /// Data-only migration. Login now gates on <c>PhoneNumberConfirmed</c>, but
    /// signup OTP verification never persisted it before this release — so every
    /// EXISTING account has it false. Grandfather them all to true here so the
    /// new gate never locks out a user who registered before the fix. Accounts
    /// created AFTER this migration runs default to false (IdentityUser default)
    /// and must verify their OTP, which is exactly the intended behaviour.
    /// </summary>
    public partial class BackfillPhoneNumberConfirmed : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "UPDATE [AspNetUsers] SET [PhoneNumberConfirmed] = 1 WHERE [PhoneNumberConfirmed] = 0;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Intentionally a no-op: reversing the backfill (un-verifying every
            // account) would re-introduce the lock-out this migration removes.
        }
    }
}
