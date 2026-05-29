using System;
using System.Threading;
using System.Threading.Tasks;
using ZansiHustle.Application.Agents.AgentPayouts.Dtos;
using ZansiHustle.Shared.Results;

namespace ZansiHustle.Application.Agents.AgentPayouts
{
    /// <summary>
    /// Records and reads the agent payout ledger.
    ///
    /// Two read shapes:
    ///   • Summary — outstanding balance + totals. Used by both the
    ///     admin drawer and the agent self-view; cheap aggregate.
    ///   • History — chronological list of paid rows. Used by both
    ///     surfaces; bounded by `take`.
    ///
    /// One write shape: `RecordPayoutAsync`. Atomic, validated, idempotent
    /// against double submits via a serializable transaction (see impl).
    ///
    /// All authorisation lives in the controller — the service trusts
    /// `recordedByUserId` is a real admin / Marketplace Growth user.
    /// </summary>
    public interface IAgentPayoutService
    {
        Task<Result<AgentEarningsSummaryDto>> GetSummaryAsync(Guid agentUserId, CancellationToken ct = default);

        Task<Result<AgentPayoutHistoryDto>> GetHistoryAsync(Guid agentUserId, int take = 100, CancellationToken ct = default);

        Task<Result<RecordAgentPayoutResponseDto>> RecordPayoutAsync(
            Guid agentUserId,
            RecordAgentPayoutRequest request,
            Guid recordedByUserId,
            CancellationToken ct = default);
    }
}
