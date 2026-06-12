namespace ZansiHustle.Shared.Enums.Payments;

/// <summary>
/// Upstream payment processor. Persisted as a short string ("Paystack", "Ozow"…)
/// so new providers can be added without migrations.
/// </summary>
public static class PaymentProvider
{
    public const string Paystack = "Paystack";
    public const string Ozow = "Ozow";
    public const string Yoco = "Yoco";

    /// <summary>Internal "provider" for a payment settled entirely from wallet
    /// balance (no external gateway). Used so the full-wallet paid path reuses the
    /// same Payment row + paid-transition machinery.</summary>
    public const string Wallet = "Wallet";
}
