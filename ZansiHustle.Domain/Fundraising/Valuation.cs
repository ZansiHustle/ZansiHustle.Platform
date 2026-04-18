using System;

namespace ZansiHustle.Domain.Fundraising
{
    /// <summary>
    /// Admin-managed valuation snapshot used by the private investment simulator.
    /// One record is marked <see cref="IsActive"/> at a time; others are history.
    /// Never exposed to public/retail users — for founder/partner discussions only.
    /// </summary>
    public class Valuation
    {
        public Guid Id { get; set; }

        /// <summary>Human-readable label, e.g. "Seed Round Q2 2026".</summary>
        public string Label { get; set; } = string.Empty;

        /// <summary>Historical founder-era valuation. Internal context only — never surfaced in simulator output.</summary>
        public decimal InternalBaseline { get; set; }

        /// <summary>Current valuation used for new-investor calculations in the simulator.</summary>
        public decimal FundraisingValuation { get; set; }

        public decimal ScenarioConservative { get; set; }
        public decimal ScenarioModerate { get; set; }
        public decimal ScenarioAggressive { get; set; }

        /// <summary>Free-text horizon label shown next to scenarios, e.g. "3 years".</summary>
        public string? ScenarioHorizonLabel { get; set; }

        public string Currency { get; set; } = "ZAR";

        /// <summary>Only one <c>IsActive = true</c> record exists at a time (enforced by filtered unique index).</summary>
        public bool IsActive { get; set; }

        public DateTime EffectiveFromUtc { get; set; } = DateTime.UtcNow;

        public string? Notes { get; set; }

        /// <summary>Admin who created the record (for audit).</summary>
        public Guid? CreatedByUserId { get; set; }

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAtUtc { get; set; }
    }
}
