namespace ZansiHustle.Infrastructure.Configuration
{
    /// <summary>
    /// UAT/dev observability switch for the delivery-quote flow. When enabled,
    /// ZansiDispatch emits structured <c>[DispatchQuoteDebug]</c> logs for the
    /// incoming request, provider outbound/inbound payloads, and the mapped
    /// response — all keyed by correlation id. OFF by default; never enable in
    /// production. Sensitive fields are redacted regardless of this flag.
    ///
    /// Bind from the root <c>DispatchDebug</c> section, so it can be toggled with
    /// the environment variable <c>DispatchDebug__Enabled=true</c>.
    /// </summary>
    public sealed class DispatchDebugOptions
    {
        public const string SectionName = "DispatchDebug";

        /// <summary>Master switch for the quote debug logs. Default false.</summary>
        public bool Enabled { get; set; }
    }
}
