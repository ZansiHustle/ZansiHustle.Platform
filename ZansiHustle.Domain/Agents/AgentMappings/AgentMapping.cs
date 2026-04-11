using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZansiHustle.Domain.Agents.AgentMappings
{
    public class AgentMapping
    {
        public string AffiliateCode { get; set; } = string.Empty; // e.g., "120"
        public Guid UserId { get; set; }
        public string UserFullName { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAtUtc { get; set; }
        public DateTime? UpdatedAtUtc { get; set; }
    }
}
