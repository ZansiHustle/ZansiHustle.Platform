namespace ZansiHustle.Application.Admin.Payments.Dtos
{
    /// <summary>
    /// One row in the admin payout queue — the money the platform owes each
    /// merchant for realized sales. Shape mirrors the portal grid directly.
    ///
    /// Today the list is returned empty: the platform does not yet persist
    /// Payout records, so there is no authoritative "who has been paid"
    /// ledger to render. The endpoint and DTO stay in place so wiring a
    /// Payout entity later is a drop-in change with no UI churn.
    /// </summary>
    public class AdminPayoutListItemDto
    {
        public string Id { get; set; } = string.Empty;
        public string Seller { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "ZAR";
        public string Method { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string Date { get; set; } = string.Empty;
    }
}
