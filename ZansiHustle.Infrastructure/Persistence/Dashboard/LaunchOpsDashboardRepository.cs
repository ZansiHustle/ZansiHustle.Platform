using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ZansiHustle.Application.Dashboard.Dtos;
using ZansiHustle.Application.Persistence.Dashboard;
using ZansiHustle.Infrastructure.Data;
using ZansiHustle.Shared.Enums.AgentApplications;
using ZansiHustle.Shared.Enums.SellerLeads;

namespace ZansiHustle.Infrastructure.Persistence.Dashboard
{
    /// <summary>
    /// Repository implementation for launch operations dashboard queries.
    /// </summary>
    public class LaunchOpsDashboardRepository : ILaunchOpsDashboardRepository
    {
        private readonly AppDbContext _context;

        /// <summary>
        /// Creates a new instance of the <see cref="LaunchOpsDashboardRepository"/> class.
        /// </summary>
        public LaunchOpsDashboardRepository(AppDbContext context)
        {
            _context = context;
        }

        /// <inheritdoc />
        public async Task<LaunchOpsSummaryDto> GetSummaryAsync()
        {
            var totalAgents = await _context.Users.CountAsync();
            var totalAgentApplications = await _context.AgentApplications.CountAsync();
            var pendingAgentApplications = await _context.AgentApplications.CountAsync(x => x.Status == AgentApplicationStatus.Pending);

            var totalSellerLeads = await _context.SellerLeads.CountAsync();
            var pendingSellerLeads = await _context.SellerLeads.CountAsync(x => x.ApprovalStatus == ApprovalStatus.Pending);
            var approvedSellerLeads = await _context.SellerLeads.CountAsync(x => x.ApprovalStatus == ApprovalStatus.Approved);
            var verifiedSellerLeads = await _context.SellerLeads.CountAsync(x => x.VerificationStatus == VerificationStatus.Verified);
            var convertedSellerLeads = await _context.SellerLeads.CountAsync(x => x.ConvertedSellerId != null);

            return new LaunchOpsSummaryDto
            {
                TotalAgents = totalAgents,
                TotalAgentApplications = totalAgentApplications,
                PendingAgentApplications = pendingAgentApplications,
                TotalSellerLeads = totalSellerLeads,
                PendingSellerLeads = pendingSellerLeads,
                ApprovedSellerLeads = approvedSellerLeads,
                VerifiedSellerLeads = verifiedSellerLeads,
                ConvertedSellerLeads = convertedSellerLeads,
                SellerLeadProvinceDistribution = await GetSellerLeadProvinceDistributionAsync(),
                TeamActivity = await GetTeamActivityAsync()
            };
        }

        /// <inheritdoc />
        public async Task<List<ProvinceDistributionDto>> GetSellerLeadProvinceDistributionAsync()
        {
            return await _context.SellerLeads
                .AsNoTracking()
                .Where(x => !string.IsNullOrWhiteSpace(x.Province))
                .GroupBy(x => x.Province!)
                .Select(g => new ProvinceDistributionDto
                {
                    Province = g.Key,
                    Count = g.Count()
                })
                .OrderByDescending(x => x.Count)
                .ToListAsync();
        }

        /// <inheritdoc />
        public async Task<List<TeamActivityDto>> GetTeamActivityAsync()
        {
            try
            {
                return await _context.Users
                    .AsNoTracking()
                    .Select(x => new TeamActivityDto
                    {
                        TeamMember = ((x.FirstName ?? string.Empty) + " " + (x.LastName ?? string.Empty)).Trim(),

                        Leads = _context.SellerLeads.Count(s => s.AssignedUserId == x.Id),

                        Conversions = _context.SellerLeads.Count(s =>
                            s.AssignedUserId == x.Id &&
                            s.ConvertedSellerId != null),

                        Pending = _context.SellerLeads.Count(s =>
                            s.AssignedUserId == x.Id &&
                            s.ApprovalStatus == ApprovalStatus.Pending),

                        Reach = 0,
                        Spend = 0m
                    })
                    .OrderByDescending(x => x.Leads)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                throw new Exception("An error occurred while retrieving team activity.", ex);
            }
        }
    }
}

