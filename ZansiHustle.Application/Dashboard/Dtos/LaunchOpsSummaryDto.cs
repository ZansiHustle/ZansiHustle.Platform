using System.Collections.Generic;

namespace ZansiHustle.Application.Dashboard.Dtos
{
    /// <summary>
    /// Represents launch and operations dashboard summary data.
    /// </summary>
    public class LaunchOpsSummaryDto
    {
        public int TotalAgents { get; set; }
        public int TotalAgentApplications { get; set; }
        public int PendingAgentApplications { get; set; }
        public int TotalSellerLeads { get; set; }
        public int PendingSellerLeads { get; set; }
        public int ApprovedSellerLeads { get; set; }
        public int VerifiedSellerLeads { get; set; }
        public int ConvertedSellerLeads { get; set; }

        public List<ProvinceDistributionDto> SellerLeadProvinceDistribution { get; set; } = new();
        public List<TeamActivityDto> TeamActivity { get; set; } = new();
    }
}
