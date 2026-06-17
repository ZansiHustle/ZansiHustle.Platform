namespace ZansiHustle.Application.Seller.Earnings.Dtos
{
    /// <summary>
    /// Clean three-number seller finance summary for the dashboard card.
    /// Server-authoritative, ALL-TIME. Never includes cancelled/refunded orders
    /// as earned, never double-counts wallet + external, and only counts a payout
    /// as "paid out" when it's a COMPLETED seller withdrawal/payout.
    /// </summary>
    public sealed class SellerFinanceSummaryDto
    {
        /// <summary>All-time NET earnings from completed/eligible product orders +
        /// service bookings (after platform fee — currently 0). Excludes
        /// cancelled/refunded and not-yet-eligible (paid-but-unfulfilled) amounts.</summary>
        public decimal TotalEarned { get; set; }

        /// <summary>Total already paid out via COMPLETED seller withdrawals/payouts.
        /// 0 until a seller payout flow exists (never faked).</summary>
        public decimal PaidOut { get; set; }

        /// <summary>Earned but not yet paid out (= TotalEarned − PaidOut).</summary>
        public decimal PendingPayout { get; set; }

        public string Currency { get; set; } = "ZAR";
    }
}
