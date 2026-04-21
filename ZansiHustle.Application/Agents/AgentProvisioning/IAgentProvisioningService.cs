using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ZansiHustle.Application.Agents.AgentProvisioning.Dtos;
using ZansiHustle.Shared.Results;

namespace ZansiHustle.Application.Agents.AgentProvisioning
{
    /// <summary>
    /// Admin provisioning of real User accounts with the "Agent" role.
    /// Replaces the earlier AgentApplication-backed shim for the
    /// /api/agents endpoint set.
    ///
    /// An "agent" here is an IdentityUser in the Agent role — the same
    /// account the agent uses to sign in. Self-applied agents still go
    /// through AgentApplicationService; this service is purely the
    /// admin-created path.
    /// </summary>
    public interface IAgentProvisioningService
    {
        Task<Result<List<AgentDetailsDto>>> GetAllAsync();
        Task<Result<AgentDetailsDto>> GetByIdAsync(Guid id);

        /// <summary>
        /// Creates a User in the Agent role with a generated temp
        /// password. Returns the plaintext password ONCE in
        /// `AgentDetailsDto.InitialPassword` so the admin can share it
        /// with the new agent. Fails cleanly if the email is taken.
        /// </summary>
        Task<Result<AgentDetailsDto>> CreateAsync(CreateAgentRequest request);

        Task<Result<AgentDetailsDto>> UpdateStatusAsync(Guid id, UpdateAgentRequest request);

        /// <summary>
        /// Soft-deactivates the agent (IsActive=false, status=Inactive).
        /// We don't hard-delete User rows because of downstream FK
        /// relationships (SellerLeads.AssignedUserId etc.).
        /// </summary>
        Task<Result> DeactivateAsync(Guid id);

        /// <summary>
        /// Generates a fresh temp password for the agent, invalidating
        /// the previous one. Returns the new plaintext once for admin to
        /// relay. Used when the admin didn't capture the original
        /// credentials before closing the drawer.
        /// </summary>
        Task<Result<AgentDetailsDto>> ResetPasswordAsync(Guid id);
    }
}
