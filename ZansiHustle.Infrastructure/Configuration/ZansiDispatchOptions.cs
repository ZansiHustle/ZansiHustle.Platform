namespace ZansiHustle.Infrastructure.Configuration
{
    /// <summary>
    /// Bound from the <c>ZansiDispatch</c> configuration section. Every field is
    /// optional — a deployment with NO ZansiDispatch config still binds cleanly
    /// (all nulls/false) and the API starts normally. Provider credentials are
    /// validated ONLY when that provider is <see cref="ZansiDispatchCourierGuyOptions.Enabled"/>
    /// and used (at quote/shipment time), never at startup — so a missing
    /// Courier Guy / Shiplogic key can never crash the host.
    ///
    /// Env vars (double-underscore = section nesting):
    ///   ZansiDispatch__DefaultProvider              (CourierGuy | InternalEstimate)
    ///   ZansiDispatch__FallbackToInternalEstimate   (true)
    ///   ZansiDispatch__QuoteExpiryMinutes           (5)
    ///   ZansiDispatch__CourierGuy__Enabled          (true)
    ///   ZansiDispatch__CourierGuy__BaseUrl          (https://api.shiplogic.com)
    ///   ZansiDispatch__CourierGuy__ApiKey           (secret — never logged)
    ///   ZansiDispatch__CourierGuy__SandboxMode      (true)
    ///   ZansiDispatch__CourierGuy__WebhookSecret    (optional inbound-webhook auth)
    ///   ZansiDispatch__Shiplogic__BaseUrl / ApiKey / SandboxMode
    /// </summary>
    public sealed class ZansiDispatchOptions
    {
        public const string SectionName = "ZansiDispatch";

        /// <summary>Config-level default provider. Overrides the DB setting when set.</summary>
        public string? DefaultProvider { get; set; }

        /// <summary>Fall back to the deterministic InternalEstimate when the courier can't quote. Default true.</summary>
        public bool FallbackToInternalEstimate { get; set; } = true;

        /// <summary>Quote validity window. Courier Guy rates expire after ~5 minutes.</summary>
        public int? QuoteExpiryMinutes { get; set; }

        public ZansiDispatchCourierGuyOptions CourierGuy { get; set; } = new();
        public ZansiDispatchShiplogicOptions Shiplogic { get; set; } = new();
    }

    public sealed class ZansiDispatchCourierGuyOptions
    {
        /// <summary>Master switch. When false the provider never calls the API.</summary>
        public bool Enabled { get; set; }

        public string? BaseUrl { get; set; }
        public string? ApiKey { get; set; }
        public string? AccountNumber { get; set; }
        public bool SandboxMode { get; set; }
        /// <summary>Optional shared secret to authenticate inbound Courier Guy webhooks.</summary>
        public string? WebhookSecret { get; set; }

        /// <summary>
        /// Independent KILL SWITCH for real courier shipment booking (the
        /// <c>POST /shipments</c> call that charges the courier account). Default
        /// FALSE for safety: quotes/tracking still work, but
        /// <c>create-from-quote</c> refuses to call the provider booking endpoint
        /// until this is explicitly enabled. NOTE: <see cref="SandboxMode"/> does
        /// NOT gate charges — THIS flag (plus using a sandbox/test API key) does.
        /// Env: <c>ZansiDispatch__CourierGuy__AllowShipmentBooking</c>.
        /// </summary>
        public bool AllowShipmentBooking { get; set; }

        /// <summary>True only when the minimum credentials to call the API are present.</summary>
        public bool IsConfigured =>
            !string.IsNullOrWhiteSpace(BaseUrl) && !string.IsNullOrWhiteSpace(ApiKey);
    }

    public sealed class ZansiDispatchShiplogicOptions
    {
        public bool Enabled { get; set; }
        public string? BaseUrl { get; set; }
        public string? ApiKey { get; set; }
        public bool SandboxMode { get; set; }

        public bool IsConfigured =>
            !string.IsNullOrWhiteSpace(BaseUrl) && !string.IsNullOrWhiteSpace(ApiKey);
    }
}
