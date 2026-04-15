using System.Threading;
using System.Threading.Tasks;
using ZansiHustle.Shared.Results;

namespace ZansiHustle.Application.Admin.Seeding
{
    /// <summary>
    /// Idempotent UAT/dev data seeder. Safe to re-run; existing records are
    /// detected and skipped. Must never be invoked in Production.
    /// </summary>
    public interface IUatSeederService
    {
        Task<Result<UatSeedSummaryDto>> SeedAllAsync(CancellationToken cancellationToken = default);
    }
}
