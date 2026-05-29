namespace ZansiHustle.Application.Agents.AgentPayouts
{
    /// <summary>
    /// Bound from the `Agents` config section. Currently a single
    /// rate (`CommissionPerApprovedLead`), but the dedicated class
    /// gives us a clean place to add tiers, per-lead-type rates, or
    /// minimum-payout thresholds later without rippling through the
    /// service signature.
    ///
    /// Frontend historically hardcoded R10 in
    /// `ZansiHustlePortal/src/lib/agentEarnings.js` —
    /// `AGENT_RATE_PER_APPROVED_LEAD = 10`. The backend default here
    /// matches that value so existing earnings reads don't change
    /// meaning when the source-of-truth moves from frontend constant
    /// to backend config.
    /// </summary>
    public class AgentEarningsSettings
    {
        public const string SectionName = "Agents";

        /// <summary>
        /// Rand per approved lead. Default R10. Override in
        /// appsettings.{env}.json or via env var
        /// `Agents__CommissionPerApprovedLead`.
        /// </summary>
        public decimal CommissionPerApprovedLead { get; set; } = 10m;
    }
}
