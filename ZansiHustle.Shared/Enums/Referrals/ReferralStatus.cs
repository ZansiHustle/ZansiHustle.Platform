namespace ZansiHustle.Shared.Enums.Referrals
{
    /// <summary>
    /// Lifecycle of a single referral relationship. A row is created at
    /// Joined; downstream systems (e.g. seller approval, first buyer order)
    /// flip it to Converted so commission engines can pick it up.
    /// </summary>
    public enum ReferralStatus
    {
        Joined = 1,
        Converted = 2,
        Cancelled = 3
    }
}
