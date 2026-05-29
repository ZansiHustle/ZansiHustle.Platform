using System;
using ZansiHustle.Domain.Identity;

namespace ZansiHustle.Domain.Agents.AgentPayouts
{
    /// <summary>
    /// One recorded payout to an agent. The ledger row pattern: a
    /// payout is created when an admin/Marketplace Growth user records
    /// that money has actually been transferred to the agent (out-of-
    /// band, e.g. EFT). The system DOES NOT initiate bank transfers —
    /// it just keeps the auditable history.
    ///
    /// Source of truth for accounting:
    ///   • TotalEarned   = approved agent-submitted SellerLeads × commission rate
    ///   • TotalPaidOut  = SUM(AgentPayout.Amount) WHERE AgentUserId == agent
    ///   • Outstanding   = TotalEarned − TotalPaidOut
    ///
    /// Why no Status / IsReversed column on day 1: every committed
    /// payout is final. Mistakes are handled by recording an OFFSET
    /// payout (negative isn't supported today — first iteration blocks
    /// overpayment so we never have to reverse). A proper void/reverse
    /// flow with audit trail is a deliberate follow-up — keeping it out
    /// of v1 means we can't ship a feature that silently rewrites
    /// history.
    /// </summary>
    public class AgentPayout
    {
        public Guid Id { get; set; }

        /// <summary>
        /// The agent paid. FK to the Identity User table — agents are
        /// IdentityUsers in the "Agent" role, not a separate entity
        /// (see AgentProvisioningService).
        /// </summary>
        public Guid AgentUserId { get; set; }
        public virtual User? AgentUser { get; set; }

        /// <summary>
        /// Amount in ZAR. Stored as decimal(18,2) — the standard
        /// money column shape used elsewhere in the system.
        /// Always > 0 (validated at the service layer).
        /// </summary>
        public decimal Amount { get; set; }

        /// <summary>
        /// When the actual external payment occurred. Defaults to
        /// RecordedAtUtc if the admin doesn't supply a date — the
        /// common case is "I just paid them; record it now."
        /// </summary>
        public DateTime PaidAtUtc { get; set; }

        /// <summary>
        /// When the row was inserted. Distinct from PaidAtUtc so the
        /// audit log can show "paid yesterday, recorded today" cases
        /// honestly.
        /// </summary>
        public DateTime RecordedAtUtc { get; set; }

        /// <summary>
        /// Which admin / Marketplace Growth user recorded this payout.
        /// FK to User. Surfaced in the admin drawer's history list so
        /// disputes can be traced back to a person.
        /// </summary>
        public Guid RecordedByUserId { get; set; }
        public virtual User? RecordedByUser { get; set; }

        /// <summary>
        /// External payment reference (EFT reference, transaction id,
        /// receipt number). Free-form; not unique because the admin
        /// could legitimately record two payouts with the same
        /// reference if the bank reused it.
        /// </summary>
        public string? PaymentReference { get; set; }

        /// <summary>
        /// EFT / Cash / Other — free-form string for the first
        /// iteration. We don't enum this yet because the operations
        /// playbook around methods is still evolving.
        /// </summary>
        public string? PaymentMethod { get; set; }

        /// <summary>Free-form admin note attached to the payout.</summary>
        public string? Note { get; set; }
    }
}
