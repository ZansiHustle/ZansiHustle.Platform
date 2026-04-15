using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ZansiHustle.Domain.Payments;

namespace ZansiHustle.Application.Persistence.Payments
{
    public interface IPaymentRepository
    {
        Task<Payment?> GetByIdAsync(Guid id);
        Task<Payment?> GetByCodeAsync(string code);
        Task<Payment?> GetByProviderReferenceAsync(string providerReference);
        Task<List<Payment>> GetByOrderAsync(Guid orderId);
        Task<Payment?> GetActiveAttemptForOrderAsync(Guid orderId);
        Task<bool> ExistsByCodeAsync(string code);
        Task AddAsync(Payment payment);
        void Update(Payment payment);
        Task AddEventAsync(PaymentEvent paymentEvent);
        Task<bool> EventKeyExistsAsync(string providerEventKey);
        Task<bool> SaveChangesAsync();
    }
}
