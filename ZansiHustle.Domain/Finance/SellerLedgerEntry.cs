using System;
using ZansiHustle.Shared.Enums.Finance;

namespace ZansiHustle.Domain.Finance
{
    /// <summary>
    /// Append-only seller proceeds book. ONE SellerNetCredit row per eligible
    /// paid order/booking captures the full fee breakdown at recognition time:
    ///   SellerNet = EligibleBase − GatewayFee − PlatformFee.
    /// Delivery is recorded separately as a pass-through and is NEVER seller
    /// earnings. Money columns are decimal(18,2). Seller funds are kept strictly
    /// separate from platform funds (see <see cref="PlatformLedgerEntry"/>).
    /// </summary>
    public class SellerLedgerEntry
    {
        public Guid Id { get; set; }

        /// <summary>Seller/provider user id (owning Merchant.OwnerUserId).</summary>
        public Guid SellerUserId { get; set; }

        public Guid? MerchantId { get; set; }
        public Guid? OrderId { get; set; }
        public Guid? ServiceBookingId { get; set; }

        public LedgerSourceType SourceType { get; set; }
        public SellerLedgerEntryType EntryType { get; set; }

        /// <summary>Order.Total (GMV) at recognition — context only.</summary>
        public decimal GrossAmount { get; set; }

        /// <summary>Order.Subtotal — the fee base (EXCLUDES delivery).</summary>
        public decimal EligibleBaseAmount { get; set; }

        /// <summary>Gateway (Ozow) fee — 3% of eligible base. A cost.</summary>
        public decimal GatewayFeeAmount { get; set; }

        /// <summary>ZansiHustle platform fee — 5% of eligible base. Revenue.</summary>
        public decimal PlatformFeeAmount { get; set; }

        /// <summary>Seller net = EligibleBase − GatewayFee − PlatformFee.</summary>
        public decimal SellerNetAmount { get; set; }

        /// <summary>Delivery fee recorded for context — pass-through, NOT seller earnings.</summary>
        public decimal DeliveryFeeAmount { get; set; }

        public string Currency { get; set; } = "ZAR";

        public SellerLedgerStatus Status { get; set; } = SellerLedgerStatus.Pending;

        /// <summary>When the underlying economic event occurred (order completed/created).</summary>
        public DateTime OccurredAtUtc { get; set; }

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

        public string? Notes { get; set; }

        /// <summary>True when produced by the reconcile/backfill job.</summary>
        public bool IsBackfilled { get; set; }

        /// <summary>Groups all rows produced by a single reconcile run.</summary>
        public Guid? BackfillBatchId { get; set; }
    }
}
