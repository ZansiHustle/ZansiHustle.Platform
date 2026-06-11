namespace ZansiHustle.Application.ServiceBookings.Dtos
{
    /// <summary>
    /// Server-computed travel-fee quote. <see cref="Status"/> is:
    ///   • "ok"        — fee is computed exactly (None / FlatFee).
    ///   • "not_ready" — distance-based pricing isn't wired yet (PerKilometre
    ///                   has no road-distance provider) or the provider hasn't
    ///                   configured fulfilment. NEVER a fabricated fee.
    ///
    /// NOTE: <see cref="Total"/> is service fee + travel fee for DISPLAY. It is
    /// NOT what the order charges today — order payment is service fee only until
    /// travel-to-order integration lands. The client must not treat Total as the
    /// payable online amount.
    /// </summary>
    public class TravelQuoteResultDto
    {
        public string Status { get; set; } = "ok";
        public decimal? DistanceKm { get; set; }
        public int? DurationMinutes { get; set; }
        public decimal TravelFee { get; set; }
        public decimal ServiceFee { get; set; }
        public decimal Total { get; set; }
        public string Currency { get; set; } = "ZAR";
        public string? Message { get; set; }

        /// <summary>
        /// Stable machine-readable reason for a non-ok status. One of:
        /// FULFILMENT_NOT_CONFIGURED, PROVIDER_LOCATION_MISSING, PROVIDER_GEO_MISSING,
        /// TRAVEL_FEE_NOT_CONFIGURED, ROUTE_PROVIDER_NOT_CONFIGURED, DESTINATION_GEO_MISSING.
        /// Null when Status == "ok".
        /// </summary>
        public string? ReasonCode { get; set; }

        /// <summary>Safe customer-facing message (mirrors <see cref="Message"/>).</summary>
        public string? UserMessage { get; set; }

        /// <summary>Internal diagnostic — shown ONLY in dev logs, never to the buyer.</summary>
        public string? DebugMessage { get; set; }
    }
}
