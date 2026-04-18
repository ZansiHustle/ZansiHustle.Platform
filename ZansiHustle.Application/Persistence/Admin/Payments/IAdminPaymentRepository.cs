using System.Collections.Generic;
using System.Threading.Tasks;
using ZansiHustle.Application.Admin.Payments.Dtos;

namespace ZansiHustle.Application.Persistence.Admin.Payments
{
    /// <summary>
    /// Admin Payments page queries.
    /// </summary>
    public interface IAdminPaymentRepository
    {
        Task<PaymentsKpisDto> GetKpisAsync();
        Task<List<AdminPayoutListItemDto>> GetPayoutQueueAsync(int limit);
    }
}
