using System;
using System.Threading.Tasks;
using ZansiHustle.Application.Admin.Customers.Dtos;
using ZansiHustle.Application.Persistence.Admin.Customers;
using ZansiHustle.Application.Common.Paging;
using ZansiHustle.Shared.Queries;
using ZansiHustle.Shared.Results;

namespace ZansiHustle.Application.Admin.Customers
{
    /// <summary>
    /// Pass-through service; centralises try/catch so repo exceptions never
    /// escape past the controller.
    /// </summary>
    public class AdminCustomerService : IAdminCustomerService
    {
        private readonly IAdminCustomerRepository _repository;

        public AdminCustomerService(IAdminCustomerRepository repository)
        {
            _repository = repository;
        }

        public async Task<Result<CustomersKpisDto>> GetKpisAsync()
        {
            try
            {
                var data = await _repository.GetKpisAsync();
                return Result<CustomersKpisDto>.Success(data, "Customer KPIs retrieved successfully.");
            }
            catch (Exception ex)
            {
                return Result<CustomersKpisDto>.Failure($"Failed to retrieve customer KPIs. {ex.Message}");
            }
        }

        public async Task<Result<PagedResult<AdminCustomerListItemDto>>> GetCustomersAsync(PagedListQueryBase query)
        {
            var safe = PagedQueryGuard.Clamp(query);

            try
            {
                var data = await _repository.GetPagedAsync(safe);
                return Result<PagedResult<AdminCustomerListItemDto>>.Success(data, "Customers retrieved successfully.");
            }
            catch (Exception ex)
            {
                return Result<PagedResult<AdminCustomerListItemDto>>.Failure($"Failed to retrieve customers. {ex.Message}");
            }
        }
    }
}
