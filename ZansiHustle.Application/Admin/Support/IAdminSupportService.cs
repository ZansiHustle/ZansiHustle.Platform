using System.Collections.Generic;
using System.Threading.Tasks;
using ZansiHustle.Application.Admin.Support.Dtos;
using ZansiHustle.Application.Common.Paging;
using ZansiHustle.Shared.Queries;
using ZansiHustle.Shared.Results;

namespace ZansiHustle.Application.Admin.Support
{
    public interface IAdminSupportService
    {
        Task<Result<SupportKpisDto>> GetKpisAsync();
        Task<Result<PagedResult<AdminTicketListItemDto>>> GetTicketsAsync(TicketsListQuery query);
        Task<Result<PagedResult<AdminDisputeListItemDto>>> GetDisputesAsync(PagedListQueryBase query);
        Task<Result<List<AdminSupportAgentDto>>> GetAgentsAsync();
        Task<Result<List<BacklogTrendPointDto>>> GetBacklogTrendAsync(int days = 14);
        Task<Result<List<RecentEscalationDto>>> GetRecentEscalationsAsync(int limit = 10);
    }
}
