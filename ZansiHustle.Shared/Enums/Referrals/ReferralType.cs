namespace ZansiHustle.Shared.Enums.Referrals
{
    /// <summary>
    /// What kind of relationship this referral records. The taxonomy is
    /// deliberately broad so the same standalone referral system can credit
    /// agents for any kind of join (seller signup, buyer signup, partner
    /// invite) without splitting tables per channel.
    /// </summary>
    public enum ReferralType
    {
        Merchant = 1,
        Buyer = 2,
        Partner = 3,
        Other = 99
    }
}
