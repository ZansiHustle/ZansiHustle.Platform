using System;
using System.Collections.Generic;
using ZansiHustle.Domain.SellerLeads;
using ZansiHustle.Shared.Enums.Agents;

namespace ZansiHustle.Domain.Agents
{
    public class Agent
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
        public AgentStatus Status { get; set; } = AgentStatus.Active;
        public DateTime JoinedDateUtc { get; set; } = DateTime.UtcNow;
        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAtUtc { get; set; }

        public virtual ICollection<SellerLead> SellerLeads { get; set; } = new List<SellerLead>();
    }
}
