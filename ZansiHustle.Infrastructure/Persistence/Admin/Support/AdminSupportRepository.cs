using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using ZansiHustle.Application.Admin.Support.Dtos;
using ZansiHustle.Application.Persistence.Admin.Support;
using ZansiHustle.Infrastructure.Data;
using ZansiHustle.Application.Common.Paging;
using ZansiHustle.Shared.Queries;
using ZansiHustle.Shared.Results;

namespace ZansiHustle.Infrastructure.Persistence.Admin.Support
{
    /// <summary>
    /// Admin Support queries.
    ///
    /// No Ticket / Dispute / SupportAgent aggregates exist in the domain
    /// yet, so real-login mode honestly returns empty paged envelopes rather
    /// than fabricating rows. Swap in real implementations here when the
    /// entities land.
    /// </summary>
    public class AdminSupportRepository : IAdminSupportRepository
    {
        private readonly AppDbContext _context;

        public AdminSupportRepository(AppDbContext context)
        {
            _context = context;
            _ = _context; // retained for future wiring
        }

        public Task<SupportKpisDto> GetKpisAsync()
        {
            return Task.FromResult(new SupportKpisDto
            {
                TotalTickets = 0,
                OpenTickets = 0,
                ResolvedTickets = 0,
                AvgResolutionTime = "0h",
                UrgentTickets = 0,
                SlaBreached = 0,
                DisputesAwaitingReview = 0,
                AvgFirstResponse = "0h",
                CsatScore = 0m,
                ResolvedThisMonth = 0,
                TotalThisMonth = 0,
            });
        }

        public Task<PagedResult<AdminTicketListItemDto>> GetTicketsPagedAsync(TicketsListQuery query)
        {
            return Task.FromResult(new PagedResult<AdminTicketListItemDto>
            {
                Items = new List<AdminTicketListItemDto>(),
                Total = 0,
                Page = query.Page,
                PageSize = query.PageSize,
            });
        }

        public Task<PagedResult<AdminDisputeListItemDto>> GetDisputesPagedAsync(PagedListQueryBase query)
        {
            return Task.FromResult(new PagedResult<AdminDisputeListItemDto>
            {
                Items = new List<AdminDisputeListItemDto>(),
                Total = 0,
                Page = query.Page,
                PageSize = query.PageSize,
            });
        }

        public Task<List<AdminSupportAgentDto>> GetAgentsAsync()
        {
            return Task.FromResult(new List<AdminSupportAgentDto>());
        }

        public Task<List<BacklogTrendPointDto>> GetBacklogTrendAsync(int days)
        {
            var today = DateTime.UtcNow.Date;
            var points = new List<BacklogTrendPointDto>(days);
            for (var i = days - 1; i >= 0; i--)
            {
                var d = today.AddDays(-i);
                points.Add(new BacklogTrendPointDto
                {
                    Date = d.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                    Opened = 0,
                    Resolved = 0,
                    Escalated = 0,
                });
            }
            return Task.FromResult(points);
        }

        public Task<List<RecentEscalationDto>> GetRecentEscalationsAsync(int limit)
        {
            return Task.FromResult(new List<RecentEscalationDto>());
        }
    }
}
