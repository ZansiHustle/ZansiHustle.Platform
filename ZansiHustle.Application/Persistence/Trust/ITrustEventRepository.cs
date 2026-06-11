using System.Threading.Tasks;
using ZansiHustle.Domain.Trust;

namespace ZansiHustle.Application.Persistence.Trust
{
    public interface ITrustEventRepository
    {
        Task AddAsync(TrustEvent trustEvent);
        Task<bool> SaveChangesAsync();
    }
}
