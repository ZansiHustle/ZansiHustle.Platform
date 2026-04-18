using System.Collections.Generic;
using System.Threading.Tasks;
using ZansiHustle.Application.Admin.Affiliates.Dtos;
using ZansiHustle.Application.Common.Paging;
using ZansiHustle.Shared.Queries;
using ZansiHustle.Shared.Results;

namespace ZansiHustle.Application.Admin.Affiliates
{
    public interface IAdminAffiliateService
    {
        Task<Result<AffiliatesKpisDto>> GetKpisAsync();
        Task<Result<PagedResult<AdminAffiliateListItemDto>>> GetAffiliatesAsync(PagedListQueryBase query);
        Task<Result<List<AffiliatePerformancePointDto>>> GetPerformanceTrendAsync(int months = 7);
    }
}
