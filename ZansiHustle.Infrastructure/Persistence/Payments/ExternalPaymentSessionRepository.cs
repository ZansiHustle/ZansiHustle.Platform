using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ZansiHustle.Application.Persistence.Payments;
using ZansiHustle.Domain.Payments.External;
using ZansiHustle.Infrastructure.Data;
using ZansiHustle.Shared.Enums.Payments;

namespace ZansiHustle.Infrastructure.Persistence.Payments
{
    public class ExternalPaymentSessionRepository : IExternalPaymentSessionRepository
    {
        private static readonly ExternalPaymentSessionStatus[] TerminalFailedStatuses =
        {
            ExternalPaymentSessionStatus.Failed,
            ExternalPaymentSessionStatus.Cancelled,
            ExternalPaymentSessionStatus.Expired,
        };

        private readonly AppDbContext _context;

        public ExternalPaymentSessionRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<ExternalPaymentSession?> GetByIdAsync(Guid id)
        {
            return await _context.ExternalPaymentSessions.FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<ExternalPaymentSession?> GetByProviderReferenceAsync(string providerReference)
        {
            if (string.IsNullOrWhiteSpace(providerReference)) return null;

            var normalised = providerReference.Trim();
            return await _context.ExternalPaymentSessions.FirstOrDefaultAsync(x => x.ProviderReference == normalised);
        }

        public async Task<ExternalPaymentSession?> GetActiveByShopAndExternalOrderAsync(string shopCode, string externalOrderId)
        {
            return await _context.ExternalPaymentSessions
                .Where(x => x.ShopCode == shopCode && x.ExternalOrderId == externalOrderId)
                .Where(x => !TerminalFailedStatuses.Contains(x.Status))
                .OrderByDescending(x => x.CreatedAtUtc)
                .FirstOrDefaultAsync();
        }

        public async Task AddAsync(ExternalPaymentSession session)
        {
            ArgumentNullException.ThrowIfNull(session);
            await _context.ExternalPaymentSessions.AddAsync(session);
        }

        public void Update(ExternalPaymentSession session)
        {
            ArgumentNullException.ThrowIfNull(session);
            _context.ExternalPaymentSessions.Update(session);
        }

        public async Task<bool> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync() > 0;
        }
    }
}
