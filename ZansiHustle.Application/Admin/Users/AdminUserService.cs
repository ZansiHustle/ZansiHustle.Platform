using System;
using System.Threading.Tasks;
using ZansiHustle.Application.Admin.Users.Dtos;
using ZansiHustle.Application.Common.Paging;
using ZansiHustle.Application.Persistence.Admin.Users;
using ZansiHustle.Shared.Errors;
using ZansiHustle.Shared.Queries;
using ZansiHustle.Shared.Results;

namespace ZansiHustle.Application.Admin.Users
{
    /// <summary>
    /// Pass-through layer that clamps paging inputs and catches repo
    /// exceptions before they escape the controller. Matches the
    /// established <c>AdminCustomerService</c> pattern.
    /// </summary>
    public sealed class AdminUserService : IAdminUserService
    {
        private readonly IAdminUserRepository _repository;

        public AdminUserService(IAdminUserRepository repository)
        {
            _repository = repository;
        }

        public async Task<Result<PagedResult<UserListItemDto>>> GetUsersAsync(UserQueryRequestDto query)
        {
            query ??= new UserQueryRequestDto();
            PagedQueryGuard.Clamp(query);

            try
            {
                var data = await _repository.GetPagedAsync(query);
                return Result<PagedResult<UserListItemDto>>.Success(data, "Users retrieved successfully.");
            }
            catch (Exception ex)
            {
                return Result<PagedResult<UserListItemDto>>.Failure(
                    ErrorCodes.Exception,
                    $"Failed to retrieve users. {ex.Message}");
            }
        }

        public async Task<Result<UserListItemDto>> GetByIdAsync(Guid id)
        {
            if (id == Guid.Empty)
                return Result<UserListItemDto>.Failure(ErrorCodes.BadRequest, "User id is required.");

            try
            {
                var user = await _repository.GetByIdAsync(id);
                if (user is null)
                    return Result<UserListItemDto>.Failure(ErrorCodes.NotFound, "User not found.");
                return Result<UserListItemDto>.Success(user, "User retrieved.");
            }
            catch (Exception ex)
            {
                return Result<UserListItemDto>.Failure(
                    ErrorCodes.Exception,
                    $"Failed to retrieve user. {ex.Message}");
            }
        }

        public async Task<Result<UsersKpisDto>> GetKpisAsync()
        {
            try
            {
                var data = await _repository.GetKpisAsync();
                return Result<UsersKpisDto>.Success(data, "User KPIs retrieved successfully.");
            }
            catch (Exception ex)
            {
                return Result<UsersKpisDto>.Failure(
                    ErrorCodes.Exception,
                    $"Failed to retrieve user KPIs. {ex.Message}");
            }
        }
    }
}
