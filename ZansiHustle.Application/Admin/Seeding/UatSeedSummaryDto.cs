using System.Collections.Generic;

namespace ZansiHustle.Application.Admin.Seeding
{
    /// <summary>
    /// Report returned by the UAT seeder showing how many entities were created
    /// vs skipped (already existed), plus the canonical lists of what now lives
    /// in the database after the run.
    /// </summary>
    public class UatSeedSummaryDto
    {
        public int CreatedUsers { get; set; }
        public int SkippedUsers { get; set; }

        public int CreatedMerchants { get; set; }
        public int SkippedMerchants { get; set; }

        public int CreatedListings { get; set; }
        public int SkippedListings { get; set; }

        public int CreatedOrders { get; set; }
        public int SkippedOrders { get; set; }

        public List<string> SellerEmails { get; set; } = new();
        public List<string> BuyerEmails { get; set; } = new();
        public List<string> Merchants { get; set; } = new();

        /// <summary>The UAT password assigned to every seeded user.</summary>
        public string Password { get; set; } = string.Empty;
    }
}
