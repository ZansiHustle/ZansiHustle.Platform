using System.Collections.Generic;
using System.Threading.Tasks;
using ZansiHustle.Application.Admin.Payments.Dtos;
using ZansiHustle.Shared.Results;

namespace ZansiHustle.Application.Admin.Payments
{
    /// <summary>
    /// Service contract for admin Payments views.
    /// </summary>
    public interface IAdminPaymentService
    {
        Task<Result<PaymentsKpisDto>> GetKpisAsync();
        Task<Result<List<AdminPayoutListItemDto>>> GetPayoutQueueAsync(int limit = 100);
    }
}
