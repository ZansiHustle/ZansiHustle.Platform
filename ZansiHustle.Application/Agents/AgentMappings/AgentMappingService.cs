using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZansiHustle.Application.Agents.AgentMappings.Dtos;

namespace ZansiHustle.Application.Agents.AgentMappings
{
    public class AgentMappingService : IAgentMappingService
    {
        private readonly Dictionary<string, AgentMappingDto> _agentMappings = new(StringComparer.OrdinalIgnoreCase)
        {
            { "120", new AgentMappingDto
              {
                  AffiliateCode = "120",
                  UserId = Guid.Parse("3f42edc3-b7c2-441c-920a-dc37ec036cc6"),
                  UserFullName = "@de.prof.codes Tiktok"
              }
            },
            { "121", new AgentMappingDto
              {
                  AffiliateCode = "121",
                  UserId = Guid.Parse("5af5bd0a-d059-4b65-a993-1002731ebb2d"),
                  UserFullName = "@brightgen.academy"
              }
            },
            { "122", new AgentMappingDto
              {
                  AffiliateCode = "122",
                  UserId = Guid.Parse("790046ff-d160-46fc-a294-93a1017d6e3b"),
                  UserFullName = "@hype.gird"
              }
            },
            { "123", new AgentMappingDto
              {
                  AffiliateCode = "122",
                  UserId = Guid.Parse("848c33ca-0237-40ac-8744-9e90812166d8"),
                  UserFullName = "@pkm.projects"
              }
            },
            // Add more mappings as needed
        };

        public Task<AgentMappingDto?> GetByAffiliateCodeAsync(string affiliateCode)
        {
            if (string.IsNullOrWhiteSpace(affiliateCode))
                return Task.FromResult<AgentMappingDto?>(null);

            _agentMappings.TryGetValue(affiliateCode.Trim(), out var mapping);
            return Task.FromResult(mapping);
        }
    }
}
