using System;
using System.Collections.Generic;

namespace ZansiHustle.Application.Agents.AgentPayouts.Dtos
{
    /// <summary>
    /// Request body for `POST /api/agents/{agentId}/payouts`. The
    /// admin/Marketplace Growth user is asserting that a real-world
    /// payment has been made to the agent and asks the system to
    /// record it in the ledger. The system does NOT initiate any
    /// transfer — wording everywhere uses "record" deliberately.
    /// </summary>
    public class RecordAgentPayoutRequest
    {
        /// <summary>Rand. Must be greater than zero. Must not exceed current outstanding.</summary>
        public decimal Amount { get; set; }

        /// <summary>
        /// When the external payment occurred. Optional — when omitted
        /// the service stamps the current UTC. Lets admins record
        /// historic payouts ("I paid them yesterday over EFT").
        /// </summary>
        public DateTime? PaidAtUtc { get; set; }

        /// <summary>External payment reference (EFT ref, receipt no, …).</summary>
        public string? PaymentReference { get; set; }

        /// <summary>Free-form. Today: EFT / Cash / Other.</summary>
        public string? PaymentMethod { get; set; }

        public string? Note { get; set; }
    }

    /// <summary>
    /// One payout row, denormalised for the admin drawer / agent
    /// self-view. `RecordedByName` is the admin who entered it —
    /// resolved from the Identity user table at read time so payout
    /// history survives admin renames.
    /// </summary>
    public class AgentPayoutDto
    {
        public Guid Id { get; set; }
        public Guid AgentUserId { get; set; }
        public decimal Amount { get; set; }
        public DateTime PaidAtUtc { get; set; }
        public DateTime RecordedAtUtc { get; set; }
        public Guid RecordedByUserId { get; set; }
        public string? RecordedByName { get; set; }
        public string? PaymentReference { get; set; }
        public string? PaymentMethod { get; set; }
        public string? Note { get; set; }
    }

    /// <summary>
    /// Source-of-truth earnings snapshot for an agent. Returned by
    /// `GET /api/agents/{id}/earnings-summary` and as a sub-object on
    /// the record-payout response so the portal can swap UI values
    /// without a follow-up GET.
    /// </summary>
    public class AgentEarningsSummaryDto
    {
        public Guid AgentUserId { get; set; }

        public int ApprovedLeadsCount { get; set; }
        public int SubmittedLeadsCount { get; set; }
        public int RejectedLeadsCount { get; set; }

        /// <summary>Configured rate the totals were computed against.</summary>
        public decimal CommissionPerLead { get; set; }

        public decimal TotalEarned { get; set; }
        public decimal TotalPaidOut { get; set; }
        public decimal Outstanding { get; set; }
    }

    /// <summary>
    /// Combined response from the record-payout endpoint. Returning
    /// the fresh summary alongside the inserted payout lets the portal
    /// update the drawer's earnings card immediately without firing a
    /// second GET.
    /// </summary>
    public class RecordAgentPayoutResponseDto
    {
        public AgentPayoutDto Payout { get; set; } = new();
        public AgentEarningsSummaryDto Summary { get; set; } = new();
    }

    /// <summary>
    /// Convenience pair returned by the list endpoints in case the
    /// portal wants both the history rows and the current totals in
    /// one round-trip. The summary mirrors what `GET … /earnings-summary`
    /// would return.
    /// </summary>
    public class AgentPayoutHistoryDto
    {
        public AgentEarningsSummaryDto Summary { get; set; } = new();
        public List<AgentPayoutDto> Payouts { get; set; } = new();
    }
}
