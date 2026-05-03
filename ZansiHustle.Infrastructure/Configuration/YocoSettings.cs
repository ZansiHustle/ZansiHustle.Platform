using System.Collections.Generic;

namespace ZansiHustle.Infrastructure.Configuration;

/// <summary>
/// Yoco provider credentials and defaults.
/// Bound from the <c>Yoco</c> configuration section. Never commit real keys —
/// supply them via environment variables or hosting-platform secret stores
/// (e.g. <c>Yoco__SecretKey</c>, <c>Yoco__WebhookSigningSecret</c>). Yoco is
/// the card-payment provider, paired with Ozow (Instant EFT). The two are
/// independently routed and configured.
/// </summary>
public sealed class YocoSettings
{
    public const string SectionName = "Yoco";

    /// <summary>Yoco REST base URL (default Yoco production API).</summary>
    public string BaseUrl { get; set; } = "https://payments.yoco.com/api";

    /// <summary>Yoco secret key (sk_live_... / sk_test_...). Sent as Bearer token on every API call.</summary>
    public string SecretKey { get; set; } = string.Empty;

    /// <summary>
    /// Per-webhook signing secret (whsec_...). Used to HMAC-verify inbound
    /// webhook requests per the Standard Webhooks spec. Each webhook
    /// registered in the Yoco dashboard has its own secret — store the one
    /// for our /api/payments/webhook/yoco endpoint here.
    /// </summary>
    public string WebhookSigningSecret { get; set; } = string.Empty;

    /// <summary>Currency code Yoco accepts (currently always "ZAR").</summary>
    public string CurrencyCode { get; set; } = "ZAR";

    /// <summary>URL Yoco redirects the buyer to after a successful card charge.</summary>
    public string SuccessUrl { get; set; } = string.Empty;

    /// <summary>URL Yoco redirects the buyer to on cancellation.</summary>
    public string CancelUrl { get; set; } = string.Empty;

    /// <summary>URL Yoco redirects the buyer to on a failed/declined charge.</summary>
    public string FailureUrl { get; set; } = string.Empty;

    /// <summary>
    /// Optional app deep-link base for the redirect-return endpoints. Same
    /// purpose and shape as Ozow:AppReturnDeepLink — when present, the
    /// Yoco return endpoints (if/when added) deep-link the buyer back into
    /// the mobile app instead of serving an HTML fallback.
    /// </summary>
    public string AppReturnDeepLink { get; set; } = string.Empty;

    /// <summary>
    /// UAT controlled-testing switch. When true, payment initiation:
    ///   • caps the charged amount at <see cref="UatTestAmount"/> (default R10)
    ///   • prefixes the payment Code with "UAT-TEST-"
    ///   • sets <c>Payment.IsTest = true</c>
    ///   • emits a "TEST PAYMENT" log line
    /// Mirrors Ozow:UatTestMode for consistent per-environment safety.
    /// </summary>
    public bool UatTestMode { get; set; } = false;

    /// <summary>Amount charged when <see cref="UatTestMode"/> is on (ZAR).</summary>
    public decimal UatTestAmount { get; set; } = 10m;

    /// <summary>
    /// Returns true only when both the API key AND the webhook signing
    /// secret are present. Without the signing secret we cannot trust any
    /// inbound webhook, so the integration is effectively broken even if
    /// /checkouts calls succeed — treat as not-configured.
    /// </summary>
    public bool HasCredentials()
        => !string.IsNullOrWhiteSpace(SecretKey)
           && !string.IsNullOrWhiteSpace(WebhookSigningSecret);

    /// <summary>
    /// Returns the names of any required configuration fields that are
    /// missing, in env-var form. Used by the startup config reporter.
    /// </summary>
    public IReadOnlyList<string> GetMissingFieldEnvVars()
    {
        var missing = new List<string>();
        if (string.IsNullOrWhiteSpace(SecretKey))            missing.Add("Yoco__SecretKey");
        if (string.IsNullOrWhiteSpace(WebhookSigningSecret)) missing.Add("Yoco__WebhookSigningSecret");
        return missing;
    }
}
