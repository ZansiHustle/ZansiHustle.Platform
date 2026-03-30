using System;
using ZansiHustle.Shared.Enums.AgentApplications;

namespace ZansiHustle.Application.AgentApplications.Dtos
{
    /// <summary>
    /// Detailed agent application DTO for detail screens.
    /// </summary>
    public class AgentApplicationDetailsDto
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
        public AgentApplicationStatus Status { get; set; }
        public DateTime SubmittedAtUtc { get; set; }
        public DateTime? ReviewedAtUtc { get; set; }
        public Guid? ReviewedByUserId { get; set; }
        public DateTime CreatedAtUtc { get; set; }
        public DateTime? UpdatedAtUtc { get; set; }
    }
}
