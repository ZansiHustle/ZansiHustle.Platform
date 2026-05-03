using System.Collections.Generic;

namespace ZansiHustle.Infrastructure.Configuration;

/// <summary>
/// Ozow provider credentials and defaults.
/// Bound from the <c>Ozow</c> configuration section. Never commit real keys —
/// supply them via environment variables or hosting-platform secret stores
/// (e.g. <c>Ozow__PrivateKey</c>, <c>Ozow__ApiKey</c>). The base
/// <c>appsettings.json</c> intentionally ships with empty credential values;
/// startup validation surfaces the specific missing field names.
/// </summary>
public sealed class OzowSettings
{
    public const string SectionName = "Ozow";

    /// <summary>Ozow REST base URL (default Ozow staging API).</summary>
    public string BaseUrl { get; set; } = "https://stagingapi.ozow.com";

    /// <summary>Ozow API key — passed in the <c>ApiKey</c> header on every request.</summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>Merchant site code issued by Ozow (binds the request to a specific merchant configuration).</summary>
    public string SiteCode { get; set; } = string.Empty;

    /// <summary>Private key used as the trailing salt in the SHA512 hashCheck. Never log or echo.</summary>
    public string PrivateKey { get; set; } = string.Empty;

    /// <summary>When true, requests Ozow to create the transaction in test mode.</summary>
    public bool IsTest { get; set; } = true;

    /// <summary>Country code Ozow expects (currently always "ZA").</summary>
    public string CountryCode { get; set; } = "ZA";

    /// <summary>Currency code Ozow expects (currently always "ZAR").</summary>
    public string CurrencyCode { get; set; } = "ZAR";

    /// <summary>URL Ozow redirects the user to on a successful checkout. May be a deep link.</summary>
    public string SuccessUrl { get; set; } = string.Empty;

    /// <summary>URL Ozow redirects the user to on cancellation.</summary>
    public string CancelUrl { get; set; } = string.Empty;

    /// <summary>URL Ozow redirects the user to on error.</summary>
    public string ErrorUrl { get; set; } = string.Empty;

    /// <summary>Server-to-server webhook URL — Ozow POSTs the TransactionNotificationResponse here.</summary>
    public string NotifyUrl { get; set; } = string.Empty;

    /// <summary>
    /// Optional app deep-link base used by the redirect-return endpoints
    /// (<c>/api/payments/ozow/return/...</c>) to send the buyer back into
    /// the mobile app after the bank flow. Example:
    /// <c>zansihustle://payments/return</c>. When empty, the return
    /// endpoints serve a plain fallback message instead of redirecting.
    /// </summary>
    public string AppReturnDeepLink { get; set; } = string.Empty;

    /// <summary>
    /// UAT controlled-testing switch. When true, payment initiation:
    ///   • caps the charged amount at <see cref="UatTestAmount"/> (default R10)
    ///   • prefixes the payment Code / TransactionReference with "UAT-TEST-"
    ///   • sets <c>Payment.IsTest = true</c> on the local row
    ///   • emits a "TEST PAYMENT" log line so test traffic is visible
    /// Intended for the deployed UAT environment where Ozow may be hitting
    /// real banking infrastructure but the team needs safe, identifiable
    /// transactions. Flip OFF for production.
    /// </summary>
    public bool UatTestMode { get; set; } = false;

    /// <summary>Amount charged when <see cref="UatTestMode"/> is on (ZAR).</summary>
    public decimal UatTestAmount { get; set; } = 10m;

    /// <summary>
    /// Returns true only when the credentials needed to talk to Ozow are
    /// present AND the inbound webhook URL is set. Missing NotifyUrl means
    /// Ozow has nowhere to deliver the TransactionNotificationResponse, so
    /// the integration is effectively broken even if PostPaymentRequest
    /// succeeds — treat as not-configured.
    /// </summary>
    public bool HasCredentials()
        => !string.IsNullOrWhiteSpace(ApiKey)
           && !string.IsNullOrWhiteSpace(SiteCode)
           && !string.IsNullOrWhiteSpace(PrivateKey)
           && !string.IsNullOrWhiteSpace(NotifyUrl);

    /// <summary>
    /// Returns the names of any required configuration fields that are
    /// missing, in env-var form. Used by the startup config reporter and
    /// can be surfaced in error responses for ops visibility.
    /// </summary>
    public IReadOnlyList<string> GetMissingFieldEnvVars()
    {
        var missing = new List<string>();
        if (string.IsNullOrWhiteSpace(ApiKey))     missing.Add("Ozow__ApiKey");
        if (string.IsNullOrWhiteSpace(SiteCode))   missing.Add("Ozow__SiteCode");
        if (string.IsNullOrWhiteSpace(PrivateKey)) missing.Add("Ozow__PrivateKey");
        if (string.IsNullOrWhiteSpace(NotifyUrl))  missing.Add("Ozow__NotifyUrl");
        return missing;
    }
}
