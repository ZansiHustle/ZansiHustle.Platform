using System;
using ZansiHustle.Shared.Enums.Agents;

namespace ZansiHustle.Application.Agents.Dtos
{
    /// <summary>
    /// Detailed agent DTO for detail screens.
    /// </summary>
    public class AgentDetailsDto
    {
        public Guid Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public string? Email { get; set; }
        public string? Province { get; set; }
        public string? City { get; set; }
        public string? SocialHandle { get; set; }
        public string? Notes { get; set; }
        public AgentStatus Status { get; set; }
        public DateTime JoinedDateUtc { get; set; }
        public DateTime CreatedAtUtc { get; set; }
        public DateTime? UpdatedAtUtc { get; set; }
    }
}
