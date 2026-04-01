namespace ZansiHustle.Application.Merchants.Dtos
{
    /// <summary>
    /// Request model used to update merchant payout eligibility.
    /// </summary>
    public class UpdateMerchantPayoutEligibilityRequestDto
    {
        public bool Eligible { get; set; }
    }
}
