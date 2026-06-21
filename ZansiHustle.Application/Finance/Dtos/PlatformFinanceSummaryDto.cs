namespace ZansiHustle.Application.Finance.Dtos
{
    /// <summary>
    /// Platform-wide finance roll-up read from the append-only ledgers. Seller
    /// funds (<see cref="SellerNetPayable"/>) are kept SEPARATE from platform
    /// funds. The gateway fee is a COST, not profit, so net retained == platform
    /// fee revenue only.
    /// </summary>
    public sealed class PlatformFinanceSummaryDto
    {
        public string Currency { get; set; } = "ZAR";

        /// <summary>Sum of PlatformFeeRevenue entries (ZansiHustle revenue, 5%).</summary>
        public decimal PlatformFeeRevenue { get; set; }

        /// <summary>Sum of GatewayFeeCost entries (Ozow, 3%) — a cost, not profit.</summary>
        public decimal GatewayFeeCost { get; set; }

        /// <summary>Sum of DeliveryFeePassThrough entries — pass-through, not revenue.</summary>
        public decimal DeliveryFeePassThrough { get; set; }

        /// <summary>Seller net owed (Pending + Available SellerNetCredit). Seller funds.</summary>
        public decimal SellerNetPayable { get; set; }

        /// <summary>What the platform actually keeps. == PlatformFeeRevenue
        /// (gateway fee is a cost, not platform profit).</summary>
        public decimal NetPlatformRetained { get; set; }
    }
}
