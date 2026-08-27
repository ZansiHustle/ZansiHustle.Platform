using System.Collections.Generic;

namespace ZansiHustle.Application.Payments.External
{
    /// <summary>
    /// Registration record for one approved external shop (e.g. "zansitech").
    /// Bound from configuration — never accept these values from an inbound
    /// request. Lives in the Application project (not Infrastructure) so
    /// <see cref="ExternalShopPaymentService"/> can depend on it directly,
    /// mirroring the existing <c>MockCheckoutSettings</c> pattern.
    /// </summary>
    public sealed class ExternalShopOptions
    {
        public bool Enabled { get; set; }

        /// <summary>Authoritative display name — always wins over a caller-supplied ShopName.</summary>
        public string ShopName { get; set; } = string.Empty;

        /// <summary>Server-to-server secret. Never committed — set via env var, e.g. ExternalShops__zansitech__SharedSecret.</summary>
        public string SharedSecret { get; set; } = string.Empty;

        /// <summary>Exact hostnames (no scheme/path) the shop's returnUrl may target.</summary>
        public List<string> AllowedReturnHosts { get; set; } = new();

        /// <summary>Exact hostnames (no scheme/path) the shop's callbackUrl may target.</summary>
        public List<string> AllowedCallbackHosts { get; set; } = new();
    }

    /// <summary>
    /// Bound from the "ExternalShops" configuration section — a map of
    /// lowercase shopCode → <see cref="ExternalShopOptions"/>. Add a new shop
    /// by adding a new key; no code change required.
    /// </summary>
    public sealed class ExternalShopsOptions : Dictionary<string, ExternalShopOptions>
    {
        public const string SectionName = "ExternalShops";
    }
}
