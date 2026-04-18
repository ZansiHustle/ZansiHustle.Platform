namespace ZansiHustle.Application.Admin.Support.Dtos
{
    /// <summary>
    /// One row in the admin tickets table. Covers the user-required fields
    /// (status, priority, user, date) plus the contextual fields the
    /// TicketQueue / TicketDetail / Escalations pages already render.
    /// </summary>
    public class AdminTicketListItemDto
    {
        public string Id { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;

        /// <summary>Customer (buyer) display name or email.</summary>
        public string Customer { get; set; } = string.Empty;
        public string? CustomerEmail { get; set; }

        /// <summary>Merchant/seller name when the ticket is about a specific shop; null otherwise.</summary>
        public string? Seller { get; set; }

        public string Priority { get; set; } = "low";
        public string Status { get; set; } = "open";
        public string Category { get; set; } = string.Empty;

        /// <summary>ISO yyyy-MM-dd open date.</summary>
        public string Date { get; set; } = string.Empty;

        public bool SlaBreached { get; set; }
        public bool Escalated { get; set; }

        public string? Agent { get; set; }
        public string? AgentId { get; set; }
    }
}
