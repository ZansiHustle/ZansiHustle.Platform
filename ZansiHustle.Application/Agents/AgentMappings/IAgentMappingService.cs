using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZansiHustle.Application.Agents.AgentMappings.Dtos;

namespace ZansiHustle.Application.Agents.AgentMappings
{
    public interface IAgentMappingService
    {
        Task<AgentMappingDto?> GetByAffiliateCodeAsync(string affiliateCode);
    }
}
