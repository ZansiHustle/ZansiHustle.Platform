using System;

namespace ZansiHustle.Application.Fundraising.Dtos
{
    public class FundraisingSummaryDto
    {
        public Guid? ActiveValuationId { get; set; }
        public string? ActiveValuationLabel { get; set; }
        public decimal? FundraisingValuation { get; set; }
        public string Currency { get; set; } = "ZAR";

        public int StakeholderCount { get; set; }
        public int FounderCount { get; set; }
        public int PartnerCount { get; set; }
        public int InvestorCount { get; set; }

        /// <summary>Sum of <c>PercentageOwned</c> across active stakeholders.</summary>
        public decimal TotalPercentageAllocated { get; set; }

        /// <summary>Remaining ownership available (100 - allocated).</summary>
        public decimal RemainingPercentage { get; set; }

        /// <summary>Sum of <c>AmountInvested</c> across active stakeholders.</summary>
        public decimal TotalAmountInvested { get; set; }
    }
}
