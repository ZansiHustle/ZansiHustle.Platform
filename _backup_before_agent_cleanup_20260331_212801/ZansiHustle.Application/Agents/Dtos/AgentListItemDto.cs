using System;
using ZansiHustle.Shared.Enums.Agents;

namespace ZansiHustle.Application.Agents.Dtos
{
    /// <summary>
    /// Lightweight agent DTO for list screens.
    /// </summary>
    public class AgentListItemDto
    {
        public Guid Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public string? Email { get; set; }
        public string? Province { get; set; }
        public string? City { get; set; }
        public AgentStatus Status { get; set; }
        public DateTime JoinedDateUtc { get; set; }
        public DateTime CreatedAtUtc { get; set; }
    }
}
