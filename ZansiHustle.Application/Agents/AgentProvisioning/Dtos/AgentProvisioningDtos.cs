using System;

namespace ZansiHustle.Application.Agents.AgentProvisioning.Dtos
{
    /// <summary>
    /// Request body for admin-side agent creation. Portal sends a single
    /// `fullName`; we split on first whitespace to populate the
    /// IdentityUser FirstName/LastName columns. Email is required and
    /// becomes the user's username.
    /// </summary>
    public class CreateAgentRequest
    {
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public string? Province { get; set; }
        public string? City { get; set; }
        public string? SocialHandle { get; set; }
        public string? Notes { get; set; }
        /// <summary>Portal encoding: 1=Active, 3=Suspended, 4=Inactive (default Active).</summary>
        public int? Status { get; set; }
    }

    public class UpdateAgentRequest
    {
        /// <summary>Portal encoding: 1=Active, 3=Suspended, 4=Inactive.</summary>
        public int? Status { get; set; }
        public string? Notes { get; set; }
    }

    /// <summary>
    /// Full agent details returned on create / reset-password. The
    /// `initialPassword` field is ONLY populated on those two paths and
    /// never on list / get-by-id responses — we never persist plaintext.
    /// </summary>
    public class AgentDetailsDto
    {
        public Guid Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public string? Province { get; set; }
        public string? City { get; set; }
        public string? SocialHandle { get; set; }
        public string? Notes { get; set; }
        /// <summary>Portal encoding: 1=Active, 3=Suspended, 4=Inactive.</summary>
        public int Status { get; set; }
        public DateTime JoinedDateUtc { get; set; }
        public DateTime? UpdatedAtUtc { get; set; }

        /// <summary>
        /// Plaintext temp password, present ONLY in the response to the
        /// create or reset-password calls. Never stored in the DB, never
        /// returned on list / get-by-id. Admin copies this out-of-band
        /// to the agent; if lost, admin clicks "Reset password" to get a
        /// fresh one (the old one is invalidated server-side).
        /// </summary>
        public string? InitialPassword { get; set; }

        // ── Source-of-truth earnings ──────────────────────────────────
        // Populated by AgentProvisioningService against the live
        // AgentPayouts ledger + SellerLeads.AssignedUserId == agent.
        // Frontend used to hardcode `paidOut: 0` and compute
        // `earningsOwed = approved × R10` locally; both values now come
        // from the server so the admin drawer and the agent self-view
        // see the same numbers.

        /// <summary>Approved leads × commission rate. Rand.</summary>
        public decimal TotalEarned { get; set; }

        /// <summary>SUM(AgentPayouts.Amount) for this agent. Rand.</summary>
        public decimal TotalPaidOut { get; set; }

        /// <summary>TotalEarned − TotalPaidOut, clamped at 0. Rand.</summary>
        public decimal Outstanding { get; set; }

        public int SubmittedLeadsCount { get; set; }
        public int ApprovedLeadsCount { get; set; }
        public int RejectedLeadsCount { get; set; }
    }
}
