using System;
using ZansiHustle.Shared.Enums.Fundraising;

namespace ZansiHustle.Application.Fundraising.Dtos
{
    public class UpdateStakeholderRequestDto
    {
        public string? FullName { get; set; }
        public StakeholderType? Type { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public decimal? PercentageOwned { get; set; }
        public decimal? AmountInvested { get; set; }
        public decimal? PricingBasisValuation { get; set; }
        public DateTime? EntryDateUtc { get; set; }
        public string? AgreementReference { get; set; }
        public string? Notes { get; set; }
        public bool? IsActive { get; set; }
    }
}
