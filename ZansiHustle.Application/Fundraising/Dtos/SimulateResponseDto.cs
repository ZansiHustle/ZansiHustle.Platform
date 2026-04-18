using System;

namespace ZansiHustle.Application.Fundraising.Dtos
{
    /// <summary>
    /// Result of an investment simulation. All projected values are scenario
    /// outputs — clients MUST render them with "Projected", "Scenario", and
    /// "Not guaranteed" labels. Internal baseline is deliberately excluded.
    /// </summary>
    public class SimulateResponseDto
    {
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "ZAR";

        /// <summary>Valuation snapshot used (always the currently active record).</summary>
        public Guid ValuationId { get; set; }
        public string ValuationLabel { get; set; } = string.Empty;
        public decimal FundraisingValuation { get; set; }
        public string? ScenarioHorizonLabel { get; set; }

        /// <summary>Ownership % at the current fundraising valuation, e.g. 10.00 for 10%.</summary>
        public decimal OwnershipPercentage { get; set; }

        /// <summary>What the investor pays today (tautology, kept for clarity).</summary>
        public decimal ImpliedValueAtEntry { get; set; }

        public ScenarioProjectionDto Conservative { get; set; } = new();
        public ScenarioProjectionDto Moderate { get; set; } = new();
        public ScenarioProjectionDto Aggressive { get; set; } = new();

        /// <summary>Canonical disclaimer — clients should render this verbatim.</summary>
        public string Disclaimer { get; set; } = string.Empty;
    }

    public class ScenarioProjectionDto
    {
        public decimal ScenarioValuation { get; set; }

        /// <summary>Projected value of the stake at this scenario valuation.</summary>
        public decimal ProjectedStakeValue { get; set; }

        /// <summary>Projected multiple on investment (e.g. 5.0 for 5×).</summary>
        public decimal ProjectedMultiple { get; set; }
    }
}
