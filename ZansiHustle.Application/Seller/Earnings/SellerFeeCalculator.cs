using System;

namespace ZansiHustle.Application.Seller.Earnings
{
    /// <summary>
    /// SINGLE source of truth for the marketplace fee split. Used by BOTH the
    /// reconcile/backfill ledger job and the live seller-earnings math so the
    /// books and the dashboard can never drift.
    ///
    /// Business rule (decimal-only, never float/double):
    ///   gatewayFee  = 3% of eligible base  (Ozow — a COST, not platform profit)
    ///   platformFee = 5% of eligible base  (ZansiHustle revenue)
    ///   sellerNet   = eligibleBase − gatewayFee − platformFee   (≈ 92%)
    /// Delivery/courier fee is handled separately and is NEVER seller earnings.
    ///
    /// eligibleBase MUST be Order.Subtotal (which already EXCLUDES delivery).
    /// Rounding is half-away-from-zero to 2 decimals on each fee independently.
    /// </summary>
    public static class SellerFeeCalculator
    {
        public const decimal GatewayFeeRate = 0.03m;
        public const decimal PlatformFeeRate = 0.05m;

        /// <summary>Immutable fee breakdown for an eligible base amount.</summary>
        public readonly record struct FeeBreakdown(
            decimal GatewayFee, decimal PlatformFee, decimal SellerNet);

        /// <summary>
        /// Computes the fee split for <paramref name="eligibleBase"/> (= Order.Subtotal).
        /// </summary>
        public static FeeBreakdown Compute(decimal eligibleBase)
        {
            var gatewayFee = Math.Round(
                eligibleBase * GatewayFeeRate, 2, MidpointRounding.AwayFromZero);
            var platformFee = Math.Round(
                eligibleBase * PlatformFeeRate, 2, MidpointRounding.AwayFromZero);
            var sellerNet = eligibleBase - gatewayFee - platformFee;
            return new FeeBreakdown(gatewayFee, platformFee, sellerNet);
        }
    }
}
