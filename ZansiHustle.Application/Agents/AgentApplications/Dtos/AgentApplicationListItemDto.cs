using System;
using ZansiHustle.Shared.Enums.AgentApplications;

namespace ZansiHustle.Application.Agents.AgentApplications.Dtos
{
    /// <summary>
    /// Lightweight user application DTO for list screens.
    /// </summary>
    public class AgentApplicationListItemDto
    {
        public Guid Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public string? Email { get; set; }
        public string? Province { get; set; }
        public string? City { get; set; }
        public AgentApplicationStatus Status { get; set; }
        public DateTime SubmittedAtUtc { get; set; }
    }
}

