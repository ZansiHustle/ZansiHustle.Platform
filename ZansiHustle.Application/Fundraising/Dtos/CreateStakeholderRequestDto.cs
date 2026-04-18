using System;
using ZansiHustle.Shared.Enums.Fundraising;

namespace ZansiHustle.Application.Fundraising.Dtos
{
    public class CreateStakeholderRequestDto
    {
        public string FullName { get; set; } = string.Empty;
        public StakeholderType Type { get; set; } = StakeholderType.Investor;
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public decimal PercentageOwned { get; set; }
        public decimal? AmountInvested { get; set; }
        public decimal PricingBasisValuation { get; set; }
        public DateTime? EntryDateUtc { get; set; }
        public string? AgreementReference { get; set; }
        public string? Notes { get; set; }
    }
}
