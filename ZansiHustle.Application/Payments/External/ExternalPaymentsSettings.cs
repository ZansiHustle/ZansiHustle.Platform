namespace ZansiHustle.Application.Payments.External
{
    /// <summary>
    /// Cross-shop configuration for the External Shop Payments feature.
    /// Bound from the "ExternalPayments" configuration section.
    /// </summary>
    public sealed class ExternalPaymentsSettings
    {
        public const string SectionName = "ExternalPayments";

        /// <summary>
        /// This ZansiHustle API's own externally-reachable base URL, e.g.
        /// "https://uatapi.zansihustle.com" or "https://api.zansihustle.com"
        /// — no trailing slash required. Used to build the ZansiHustle-owned
        /// Ozow SuccessUrl/CancelUrl/ErrorUrl/NotifyUrl for external sessions
        /// (never the shop's own URLs — Ozow must always return to
        /// ZansiHustle first). Required before any shop can be enabled.
        /// </summary>
        public string ApiBaseUrl { get; set; } = string.Empty;

        /// <summary>
        /// A RedirectCreated/Processing session older than this with no
        /// terminal resolution is opportunistically marked Expired the next
        /// time it's read (status poll, webhook, or browser return) — there
        /// is no background sweep. Default 24h.
        /// </summary>
        public int SessionExpiryHours { get; set; } = 24;

        /// <summary>HTTP timeout for delivering the signed callback to a shop's CallbackUrl.</summary>
        public int CallbackTimeoutSeconds { get; set; } = 15;
    }
}
