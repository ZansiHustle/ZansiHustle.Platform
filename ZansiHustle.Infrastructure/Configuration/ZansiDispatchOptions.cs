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
        /// KILL SWITCH for CREATING NEW courier shipments only (the
        /// <c>POST /shipments</c> booking call that charges the courier account):
        /// create-from-quote, auto-book after acceptance, and retry-booking.
        /// Default FALSE for safety. This DOES NOT gate risk-reducing operations
        /// on an ALREADY-BOOKED shipment — provider cancellation, status/tracking
        /// refresh, label reads — nor internal cancellation of unbooked shipments;
        /// those are governed by <see cref="AllowProviderCancellation"/> /
        /// <see cref="AllowProviderStatusRefresh"/> (both default TRUE). NOTE:
        /// <see cref="SandboxMode"/> does NOT gate charges — THIS flag (plus a
        /// sandbox/test API key) does.
        /// Env: <c>ZansiDispatch__CourierGuy__AllowShipmentBooking</c>.
        /// </summary>
        public bool AllowShipmentBooking { get; set; }

        /// <summary>
        /// Whether the platform may CANCEL an already-booked courier shipment via
        /// the provider. Default TRUE — cancelling an existing booking is a
        /// risk-REDUCING action, so we must not get stuck with a booked shipment
        /// ops can't cancel just because new bookings are disabled. Independent of
        /// <see cref="AllowShipmentBooking"/>. Set false only to freeze ALL
        /// provider cancellation (then a booked shipment's cancel is parked as
        /// NeedsAttention instead of silently cancelled internally).
        /// Env: <c>ZansiDispatch__CourierGuy__AllowProviderCancellation</c>.
        /// </summary>
        public bool AllowProviderCancellation { get; set; } = true;

        /// <summary>
        /// Whether the platform may poll the provider for live status/tracking.
        /// Default TRUE — reading status is non-billable and never creates a
        /// booking. Independent of <see cref="AllowShipmentBooking"/>.
        /// Env: <c>ZansiDispatch__CourierGuy__AllowProviderStatusRefresh</c>.
        /// </summary>
        public bool AllowProviderStatusRefresh { get; set; } = true;

        /// <summary>
        /// When true, the backend AUTOMATICALLY books the courier shipment once
        /// the seller ACCEPTS a paid product order (no Swagger/manual call).
        /// Default FALSE for safety. This is independent of — and gated behind —
        /// <see cref="AllowShipmentBooking"/>: auto-book never bypasses the kill
        /// switch or any create-from-quote guard. Booking still happens only
        /// after seller acceptance (never in the payment webhook), and a failed
        /// auto-book marks the shipment <c>NeedsAttention</c> (it never fails the
        /// seller-accept). Env:
        /// <c>ZansiDispatch__CourierGuy__AutoBookAfterSellerAcceptance</c>.
        /// </summary>
        public bool AutoBookAfterSellerAcceptance { get; set; }

        // NOTE: checkout quote CURATION (allowed codes / preferred / outlier /
        // advanced / high-fee warning) is OPERATIONAL POLICY, not a secret — it
        // lives in DB-managed ZansiDispatchSettings (ops-editable from the
        // ZansiDispatch portal), NOT here. Env/appsettings keeps only credentials,
        // base URL, and hard safety switches.

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
