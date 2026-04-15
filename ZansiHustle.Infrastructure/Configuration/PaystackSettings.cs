namespace ZansiHustle.Infrastructure.Configuration;

/// <summary>
/// Paystack provider credentials and defaults.
/// Bound from the <c>Paystack</c> configuration section. Never commit real keys —
/// supply them via environment variables or user-secrets
/// (e.g. <c>Paystack__SecretKey</c>).
/// </summary>
public sealed class PaystackSettings
{
    public const string SectionName = "Paystack";

    /// <summary>Secret key used for server-to-server API calls and webhook HMAC verification.</summary>
    public string SecretKey { get; set; } = string.Empty;

    /// <summary>Public key; safe to return to mobile clients for native SDK flows.</summary>
    public string PublicKey { get; set; } = string.Empty;

    /// <summary>Paystack REST base URL (default <c>https://api.paystack.co</c>).</summary>
    public string BaseUrl { get; set; } = "https://api.paystack.co";

    /// <summary>Default return URL after checkout. May be a deep link (e.g. <c>zansihustle://payments/return</c>).</summary>
    public string? CallbackUrl { get; set; }

    /// <summary>Default currency for the tenant. ZAR for SA.</summary>
    public string Currency { get; set; } = "ZAR";

    public bool HasCredentials()
        => !string.IsNullOrWhiteSpace(SecretKey);
}
