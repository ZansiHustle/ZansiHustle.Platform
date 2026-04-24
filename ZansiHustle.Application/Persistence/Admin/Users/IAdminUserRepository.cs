using System;
using System.Threading.Tasks;
using ZansiHustle.Application.Admin.Users.Dtos;
using ZansiHustle.Application.Common.Paging;

namespace ZansiHustle.Application.Persistence.Admin.Users
{
    /// <summary>
    /// Persistence contract for admin-side user visibility queries.
    /// Implementations live in Infrastructure and talk to EF Core
    /// directly so we can batch Identity + Merchant joins efficiently.
    /// </summary>
    public interface IAdminUserRepository
    {
        Task<PagedResult<UserListItemDto>> GetPagedAsync(UserQueryRequestDto query);
        Task<UserListItemDto?> GetByIdAsync(Guid id);
        Task<UsersKpisDto> GetKpisAsync();
    }
}
