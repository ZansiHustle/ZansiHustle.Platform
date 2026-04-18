using System.Collections.Generic;
using System.Threading.Tasks;
using ZansiHustle.Application.Admin.Affiliates.Dtos;
using ZansiHustle.Application.Common.Paging;
using ZansiHustle.Shared.Queries;
using ZansiHustle.Shared.Results;

namespace ZansiHustle.Application.Persistence.Admin.Affiliates
{
    public interface IAdminAffiliateRepository
    {
        Task<AffiliatesKpisDto> GetKpisAsync();
        Task<PagedResult<AdminAffiliateListItemDto>> GetPagedAsync(PagedListQueryBase query);
        Task<List<AffiliatePerformancePointDto>> GetPerformanceTrendAsync(int months);
    }
}
