using System;
using ZansiHustle.Shared.Enums.Merchants;

namespace ZansiHustle.Domain.Merchants
{
    /// <summary>
    /// A recorded seller fulfilment incident — the foundation for holding sellers
    /// accountable when they accept an order but fail to deliver on it (no stock,
    /// no-show pickup, repeated delays, …).
    ///
    /// V1 is intentionally conservative: an incident is RECORDED as
    /// <see cref="SellerIncidentStatus.PendingReview"/> and does NOT automatically
    /// charge the seller. An admin reviews and explicitly applies (or waives) it.
    /// When applied, the amount is meant to reduce the seller's future payout —
    /// the seller payout/earnings may legitimately go negative. The CUSTOMER is
    /// never debited; customer refunds are handled separately on the order.
    /// Append-only history; all timestamps are UTC.
    /// </summary>
    public class SellerIncident
    {
        public Guid Id { get; set; }

        /// <summary>The merchant (shop) the incident is recorded against.</summary>
        public Guid MerchantId { get; set; }
        public Merchant? Merchant { get; set; }

        /// <summary>The order the incident relates to, when applicable.</summary>
        public Guid? OrderId { get; set; }

        public SellerIncidentType IncidentType { get; set; }
        public SellerIncidentSeverity Severity { get; set; } = SellerIncidentSeverity.Medium;

        /// <summary>Penalty amount earmarked against the seller's payout (0 until decided).</summary>
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "ZAR";

        public SellerIncidentStatus Status { get; set; } = SellerIncidentStatus.PendingReview;

        /// <summary>Why the incident was recorded.</summary>
        public string Reason { get; set; } = string.Empty;
        /// <summary>Free-text admin notes added on review.</summary>
        public string? AdminNotes { get; set; }

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
        /// <summary>When an admin applied the penalty (null until Applied).</summary>
        public DateTime? AppliedAtUtc { get; set; }
    }
}
