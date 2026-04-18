using System;

namespace ZansiHustle.Application.Fundraising.Dtos
{
    public class UpdateValuationRequestDto
    {
        public string? Label { get; set; }
        public decimal? InternalBaseline { get; set; }
        public decimal? FundraisingValuation { get; set; }
        public decimal? ScenarioConservative { get; set; }
        public decimal? ScenarioModerate { get; set; }
        public decimal? ScenarioAggressive { get; set; }
        public string? ScenarioHorizonLabel { get; set; }
        public string? Notes { get; set; }
        public DateTime? EffectiveFromUtc { get; set; }
    }
}
