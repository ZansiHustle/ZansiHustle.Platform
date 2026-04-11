namespace ZansiHustle.Application.Agents.AgentApplications.Dtos
{
    /// <summary>
    /// Request model used to create a new user application.
    /// </summary>
    public class CreateAgentApplicationRequestDto
    {
        public string FullName { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public string? Email { get; set; }
        public string? Province { get; set; }
        public string? City { get; set; }
        public string? SocialHandle { get; set; }
        public string? Notes { get; set; }
    }
}

