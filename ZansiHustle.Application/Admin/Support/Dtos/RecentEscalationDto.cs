namespace ZansiHustle.Application.Admin.Support.Dtos
{
    /// <summary>
    /// A recent ticket escalation event — the Escalations page and the
    /// SupportDashboard both render a short feed of these.
    /// </summary>
    public class RecentEscalationDto
    {
        public string Id { get; set; } = string.Empty;
        public string TicketId { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string From { get; set; } = string.Empty;
        public string To { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
        public string Timestamp { get; set; } = string.Empty;
    }
}
