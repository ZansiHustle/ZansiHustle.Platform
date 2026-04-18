using System;
using ZansiHustle.Shared.Enums.Fundraising;

namespace ZansiHustle.Domain.Fundraising
{
    /// <summary>
    /// Cap-table entry tracking a founder, strategic partner, or cash investor.
    /// Intentionally avoids "share" terminology — this is an ownership record
    /// for private fundraising discussions, not a regulated share register.
    /// </summary>
    public class Stakeholder
    {
        public Guid Id { get; set; }

        public string FullName { get; set; } = string.Empty;

        public StakeholderType Type { get; set; }

        public string? Email { get; set; }
        public string? Phone { get; set; }

        /// <summary>Ownership percentage (e.g. 12.5000 for 12.5%).</summary>
        public decimal PercentageOwned { get; set; }

        /// <summary>Cash amount invested. Null for founders who contributed sweat equity.</summary>
        public decimal? AmountInvested { get; set; }

        /// <summary>The valuation under which this stakeholder entered the cap table.</summary>
        public decimal PricingBasisValuation { get; set; }

        public DateTime EntryDateUtc { get; set; } = DateTime.UtcNow;

        /// <summary>Optional contract number or URL reference.</summary>
        public string? AgreementReference { get; set; }

        public string? Notes { get; set; }

        public bool IsActive { get; set; } = true;

        public Guid? CreatedByUserId { get; set; }

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAtUtc { get; set; }
    }
}
