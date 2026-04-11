using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZansiHustle.Application.Agents.AgentMappings.Dtos
{
    public class AgentMappingDto
    {
        public string AffiliateCode { get; set; } = string.Empty;
        public Guid UserId { get; set; }
        public string UserFullName { get; set; } = string.Empty;
    }
}
