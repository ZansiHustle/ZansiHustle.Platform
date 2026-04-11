using ZansiHustle.Shared.Enums.AgentApplications;

namespace ZansiHustle.Application.Agents.AgentApplications.Dtos
{
    /// <summary>
    /// Request model used to review an user application.
    /// </summary>
    public class ReviewAgentApplicationRequestDto
    {
        public AgentApplicationStatus Status { get; set; }
        public string? Notes { get; set; }
        public Guid? ReviewedByUserId { get; set; }
    }
}

