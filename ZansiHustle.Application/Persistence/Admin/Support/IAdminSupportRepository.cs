using System.Collections.Generic;
using System.Threading.Tasks;
using ZansiHustle.Application.Admin.Support.Dtos;
using ZansiHustle.Application.Common.Paging;
using ZansiHustle.Shared.Queries;
using ZansiHustle.Shared.Results;

namespace ZansiHustle.Application.Persistence.Admin.Support
{
    /// <summary>
    /// Admin Support page queries.
    /// </summary>
    public interface IAdminSupportRepository
    {
        Task<SupportKpisDto> GetKpisAsync();
        Task<PagedResult<AdminTicketListItemDto>> GetTicketsPagedAsync(TicketsListQuery query);
        Task<PagedResult<AdminDisputeListItemDto>> GetDisputesPagedAsync(PagedListQueryBase query);
        Task<List<AdminSupportAgentDto>> GetAgentsAsync();
        Task<List<BacklogTrendPointDto>> GetBacklogTrendAsync(int days);
        Task<List<RecentEscalationDto>> GetRecentEscalationsAsync(int limit);
    }
}
