using System;

namespace ZansiHustle.Application.Fundraising.Dtos
{
    public class CreateValuationRequestDto
    {
        public string Label { get; set; } = string.Empty;
        public decimal InternalBaseline { get; set; }
        public decimal FundraisingValuation { get; set; }
        public decimal ScenarioConservative { get; set; }
        public decimal ScenarioModerate { get; set; }
        public decimal ScenarioAggressive { get; set; }
        public string? ScenarioHorizonLabel { get; set; }
        public string? Notes { get; set; }
        public DateTime? EffectiveFromUtc { get; set; }

        /// <summary>If true, this valuation is activated immediately (deactivates the current one).</summary>
        public bool ActivateImmediately { get; set; }
    }
}
