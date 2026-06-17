namespace ZansiHustle.Application.Payments
{
    /// <summary>
    /// Dev/UAT-only switch for the mock checkout flow. Bound from the
    /// <c>Payments</c> configuration section. When <see cref="MockCheckoutEnabled"/>
    /// is <c>true</c>, the <c>POST /api/payments/mock/order-success</c> endpoint
    /// can settle an order WITHOUT contacting Ozow — by running the SAME internal
    /// paid-transition path (<c>AdvanceOrderOnPaidAsync</c>) the real Ozow webhook
    /// uses. Defaults to <c>false</c>; the base appsettings.json ships it off.
    ///
    /// SECURITY: this is the real gatekeeper. The mobile app's
    /// <c>EXPO_PUBLIC_MOCK_PAYMENTS</c> flag only changes UI behaviour — the
    /// backend will refuse the mock endpoint (404) whenever this is false, and
    /// the controller additionally hard-blocks it in Production regardless of
    /// this value. NEVER set <c>Payments__MockCheckoutEnabled=true</c> in
    /// production.
    /// </summary>
    public sealed class MockCheckoutSettings
    {
        public const string SectionName = "Payments";

        /// <summary>When true, the mock order-success endpoint is active (dev/UAT only).</summary>
        public bool MockCheckoutEnabled { get; set; } = false;
    }
}
