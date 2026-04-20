namespace ZansiHustle.Application.Merchants.Dtos
{
    /// <summary>
    /// Request payload for the seller-facing "update my bank details"
    /// endpoint. Intentionally narrow — no merchant-profile fields — so
    /// saving bank details never accidentally clobbers shop information
    /// (which is a concern on the full UpdateMyMerchant endpoint because
    /// that one does full-replace writes).
    /// </summary>
    public class UpdateMyBankRequestDto
    {
        public string? BankName { get; set; }
        public string? BankAccountHolder { get; set; }
        public string? BankAccountNumber { get; set; }
        public string? BankAccountType { get; set; }
        public string? BankBranchCode { get; set; }
    }
}
