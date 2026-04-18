using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ZansiHustle.Application.Admin.Payments.Dtos;
using ZansiHustle.Application.Persistence.Admin.Payments;
using ZansiHustle.Shared.Results;

namespace ZansiHustle.Application.Admin.Payments
{
    /// <summary>
    /// Thin pass-through; funnels repo exceptions into <see cref="Result{T}"/>
    /// so the controller can never leak a stack trace.
    /// </summary>
    public class AdminPaymentService : IAdminPaymentService
    {
        private readonly IAdminPaymentRepository _repository;

        public AdminPaymentService(IAdminPaymentRepository repository)
        {
            _repository = repository;
        }

        public async Task<Result<PaymentsKpisDto>> GetKpisAsync()
        {
            try
            {
                var data = await _repository.GetKpisAsync();
                return Result<PaymentsKpisDto>.Success(data, "Payments KPIs retrieved successfully.");
            }
            catch (Exception ex)
            {
                return Result<PaymentsKpisDto>.Failure($"Failed to retrieve payments KPIs. {ex.Message}");
            }
        }

        public async Task<Result<List<AdminPayoutListItemDto>>> GetPayoutQueueAsync(int limit = 100)
        {
            if (limit < 1) limit = 1;
            if (limit > 500) limit = 500;

            try
            {
                var data = await _repository.GetPayoutQueueAsync(limit);
                return Result<List<AdminPayoutListItemDto>>.Success(data, "Payout queue retrieved successfully.");
            }
            catch (Exception ex)
            {
                return Result<List<AdminPayoutListItemDto>>.Failure($"Failed to retrieve payout queue. {ex.Message}");
            }
        }
    }
}
