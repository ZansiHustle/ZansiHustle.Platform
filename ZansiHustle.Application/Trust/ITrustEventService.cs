using System;
using System.Threading.Tasks;
using ZansiHustle.Shared.Enums.Trust;

namespace ZansiHustle.Application.Trust
{
    /// <summary>
    /// Records trust-signal events (append-only history). No scoring or penalties
    /// in this pass — just durable data for a future scoring layer / ZansiPulse.
    /// Best-effort: a failure to record must never break the business operation.
    /// </summary>
    public interface ITrustEventService
    {
        Task RecordAsync(
            Guid userId,
            TrustActorRole actorRole,
            TrustEventType type,
            string referenceType,
            Guid referenceId,
            object? metadata = null);
    }
}
