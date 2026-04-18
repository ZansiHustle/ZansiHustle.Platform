namespace ZansiHustle.Application.Admin.Support.Dtos
{
    /// <summary>
    /// A support agent (internal team member assigned to tickets). No Agent
    /// role is formally modeled yet — the DTO captures what the UI needs so
    /// the AgentWorkload / TicketDetail pages can be backed by a real query
    /// once the role is introduced.
    /// </summary>
    public class AdminSupportAgentDto
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string Avatar { get; set; } = string.Empty;
        public int Open { get; set; }
        public int InProgress { get; set; }
        public int Resolved { get; set; }
        public string AvgResponse { get; set; } = "0h";
        public bool Online { get; set; }
    }
}
