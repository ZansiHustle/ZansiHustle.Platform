namespace ZansiHustle.Application.Admin.Support.Dtos
{
    /// <summary>
    /// One row in the admin disputes table. Disputes are not modeled as a
    /// distinct aggregate today — the DTO stays in place so wiring a real
    /// Dispute entity later is a repository swap, no UI churn.
    /// </summary>
    public class AdminDisputeListItemDto
    {
        public string Id { get; set; } = string.Empty;
        public string Order { get; set; } = string.Empty;
        public string Buyer { get; set; } = string.Empty;
        public string Seller { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "ZAR";
        public string Status { get; set; } = "pending_evidence";
        public string Type { get; set; } = string.Empty;
        public string Date { get; set; } = string.Empty;
    }
}
