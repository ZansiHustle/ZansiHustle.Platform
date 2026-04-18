namespace ZansiHustle.Application.Admin.Payments.Dtos
{
    /// <summary>
    /// Payment KPIs for the admin Payments page. Revenue numbers mirror the
    /// Analytics module's definitions so the two dashboards can't disagree:
    /// "paid" means <c>PaymentStatus.Paid</c>.
    ///
    /// <see cref="PendingPayouts"/> is reserved — the platform doesn't yet
    /// track merchant payout settlements, so the backend returns 0 today.
    /// </summary>
    public class PaymentsKpisDto
    {
        // --- fields the existing Payments page already reads ---
        public decimal TotalGMV { get; set; }
        public decimal NetRevenue { get; set; }
        public decimal PendingPayouts { get; set; }

        // --- explicit payment-status tracking (new) ---
        public decimal TotalRevenue { get; set; }     // same as TotalGMV
        public decimal PendingPayments { get; set; }  // gross total of orders awaiting capture
        public decimal FailedPayments { get; set; }   // gross total of orders whose capture failed
        public decimal Refunded { get; set; }         // gross total of refunded orders

        // --- counts, for the UI's per-bucket tiles if/when they wire them ---
        public int PaidCount { get; set; }
        public int PendingCount { get; set; }
        public int FailedCount { get; set; }
        public int RefundedCount { get; set; }
    }
}
