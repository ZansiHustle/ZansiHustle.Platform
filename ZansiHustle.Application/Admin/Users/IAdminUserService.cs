using System;
using System.Threading.Tasks;
using ZansiHustle.Application.Admin.Users.Dtos;
using ZansiHustle.Application.Common.Paging;
using ZansiHustle.Shared.Results;

namespace ZansiHustle.Application.Admin.Users
{
    /// <summary>
    /// Admin-only user visibility surface. Consumed by
    /// <c>UsersController</c> to power the internal Users list / detail /
    /// KPIs for support, compliance, and launch-ops workflows.
    /// </summary>
    public interface IAdminUserService
    {
        Task<Result<PagedResult<UserListItemDto>>> GetUsersAsync(UserQueryRequestDto query);
        Task<Result<UserListItemDto>> GetByIdAsync(Guid id);
        Task<Result<UsersKpisDto>> GetKpisAsync();
    }
}
