using System;
using System.Threading.Tasks;
using ZansiHustle.Application.Persistence.Trust;
using ZansiHustle.Domain.Trust;
using ZansiHustle.Infrastructure.Data;

namespace ZansiHustle.Infrastructure.Persistence.Trust
{
    public class TrustEventRepository : ITrustEventRepository
    {
        private readonly AppDbContext _context;

        public TrustEventRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(TrustEvent trustEvent)
        {
            ArgumentNullException.ThrowIfNull(trustEvent);
            await _context.Set<TrustEvent>().AddAsync(trustEvent);
        }

        public async Task<bool> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync() > 0;
        }
    }
}
