namespace ZansiHustle.Application.Admin.Affiliates.Dtos
{
    /// <summary>
    /// One row in the admin Affiliates table. Shape matches the portal grid
    /// 1:1 — no client-side mapping needed.
    /// </summary>
    public class AdminAffiliateListItemDto
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;

        public int Clicks { get; set; }
        public int Referrals { get; set; }
        public int Conversions { get; set; }

        public decimal Revenue { get; set; }
        public decimal Commission { get; set; }
        public string Currency { get; set; } = "ZAR";

        public string Status { get; set; } = "inactive";
    }
}
