namespace ZansiHustle.Application.Agents.Dtos
{
    /// <summary>
    /// Request model used to update an existing agent.
    /// </summary>
    public class UpdateAgentRequestDto
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
