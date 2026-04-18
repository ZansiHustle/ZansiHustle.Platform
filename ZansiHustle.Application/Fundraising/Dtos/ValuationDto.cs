using System;

namespace ZansiHustle.Application.Fundraising.Dtos
{
    /// <summary>Full valuation record. Internal baseline is included for admin views only.</summary>
    public class ValuationDto
    {
        public Guid Id { get; set; }
        public string Label { get; set; } = string.Empty;

        public decimal InternalBaseline { get; set; }
        public decimal FundraisingValuation { get; set; }

        public decimal ScenarioConservative { get; set; }
        public decimal ScenarioModerate { get; set; }
        public decimal ScenarioAggressive { get; set; }
        public string? ScenarioHorizonLabel { get; set; }

        public string Currency { get; set; } = "ZAR";
        public bool IsActive { get; set; }
        public DateTime EffectiveFromUtc { get; set; }
        public string? Notes { get; set; }

        public DateTime CreatedAtUtc { get; set; }
        public DateTime? UpdatedAtUtc { get; set; }
    }

    /// <summary>
    /// Partner-safe view: excludes <c>InternalBaseline</c> and non-active valuations.
    /// Used on read endpoints that serve the Partner role.
    /// </summary>
    public class ActiveValuationPublicDto
    {
        public Guid Id { get; set; }
        public string Label { get; set; } = string.Empty;

        public decimal FundraisingValuation { get; set; }

        public decimal ScenarioConservative { get; set; }
        public decimal ScenarioModerate { get; set; }
        public decimal ScenarioAggressive { get; set; }
        public string? ScenarioHorizonLabel { get; set; }

        public string Currency { get; set; } = "ZAR";
        public DateTime EffectiveFromUtc { get; set; }
    }
}
