using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ZansiHustle.Application.Agents.AgentPayouts;
using ZansiHustle.Application.Agents.AgentPayouts.Dtos;
using ZansiHustle.Domain.Agents.AgentPayouts;
using ZansiHustle.Domain.Identity;
using ZansiHustle.Infrastructure.Data;
using ZansiHustle.Shared.Enums.SellerLeads;
using ZansiHustle.Shared.Enums.User;
using ZansiHustle.Shared.Errors;
using ZansiHustle.Shared.Results;

namespace ZansiHustle.Infrastructure.Agents
{
    /// <summary>
    /// AgentPayoutService — the source of truth for agent commission
    /// accounting.
    ///
    /// Reads:
    ///   • Summary  — one cheap aggregate query for the totals.
    ///   • History  — chronological payouts joined with the recording
    ///     admin's display name.
    ///
    /// Writes:
    ///   • RecordPayoutAsync — wraps validation + insert in a
    ///     SERIALIZABLE transaction so two parallel POSTs from a
    ///     double-clicking admin can never both pass the
    ///     `amount &lt;= outstanding` check. Without this, the validation
    ///     would race: tx-A reads outstanding, tx-B reads the same
    ///     outstanding, both insert R10, agent ends up overpaid by R10.
    ///     SQL Server's Serializable level holds the range lock on the
    ///     payout sum until the inserting transaction commits.
    ///
    /// Why no overpayment escape hatch on day 1: the user's product
    /// brief explicitly blocks overpayment. The day we need it (refund
    /// owed back, etc.) we add a `void`/`reverse` flow with audit trail
    /// — not a silent overpayment toggle.
    /// </summary>
    public sealed class AgentPayoutService : IAgentPayoutService
    {
        private const string AgentRoleName = nameof(UserRole.Agent);

        private readonly AppDbContext _db;
        private readonly UserManager<User> _userManager;
        private readonly AgentEarningsSettings _settings;
        private readonly ILogger<AgentPayoutService> _logger;

        public AgentPayoutService(
            AppDbContext db,
            UserManager<User> userManager,
            IOptions<AgentEarningsSettings> settings,
            ILogger<AgentPayoutService> logger)
        {
            _db = db;
            _userManager = userManager;
            _settings = settings.Value;
            _logger = logger;
        }

        // ── Summary ────────────────────────────────────────────────

        public async Task<Result<AgentEarningsSummaryDto>> GetSummaryAsync(
            Guid agentUserId, CancellationToken ct = default)
        {
            try
            {
                var agentCheck = await EnsureAgentExistsAsync(agentUserId);
                if (!agentCheck.IsSuccess)
                    return Result<AgentEarningsSummaryDto>.Failure(agentCheck.Code, agentCheck.Message);

                var summary = await ComputeSummaryAsync(agentUserId, ct);
                return Result<AgentEarningsSummaryDto>.Success(summary, "Earnings summary retrieved.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Failed to compute earnings summary for agent {AgentId}", agentUserId);
                return Result<AgentEarningsSummaryDto>.Failure(ErrorCodes.Exception,
                    "An error occurred while computing earnings.");
            }
        }

        // ── History ────────────────────────────────────────────────

        public async Task<Result<AgentPayoutHistoryDto>> GetHistoryAsync(
            Guid agentUserId, int take = 100, CancellationToken ct = default)
        {
            try
            {
                var agentCheck = await EnsureAgentExistsAsync(agentUserId);
                if (!agentCheck.IsSuccess)
                    return Result<AgentPayoutHistoryDto>.Failure(agentCheck.Code, agentCheck.Message);

                // Bound the take — admins should not be able to pull
                // unbounded history on a misconfigured client.
                if (take <= 0) take = 100;
                if (take > 500) take = 500;

                var summary = await ComputeSummaryAsync(agentUserId, ct);

                // Single query for the rows; join with Users so the
                // RecordedByName is resolved at read time (admins can
                // rename and the audit row keeps its semantic meaning).
                var rows = await (
                    from p in _db.AgentPayouts.AsNoTracking()
                    where p.AgentUserId == agentUserId
                    orderby p.PaidAtUtc descending, p.RecordedAtUtc descending
                    join u in _db.Users.AsNoTracking() on p.RecordedByUserId equals u.Id into uj
                    from u in uj.DefaultIfEmpty()
                    select new AgentPayoutDto
                    {
                        Id = p.Id,
                        AgentUserId = p.AgentUserId,
                        Amount = p.Amount,
                        PaidAtUtc = p.PaidAtUtc,
                        RecordedAtUtc = p.RecordedAtUtc,
                        RecordedByUserId = p.RecordedByUserId,
                        RecordedByName = u != null
                            ? ((u.FirstName ?? string.Empty) + " " + (u.LastName ?? string.Empty)).Trim()
                            : null,
                        PaymentReference = p.PaymentReference,
                        PaymentMethod = p.PaymentMethod,
                        Note = p.Note,
                    }
                ).Take(take).ToListAsync(ct);

                return Result<AgentPayoutHistoryDto>.Success(new AgentPayoutHistoryDto
                {
                    Summary = summary,
                    Payouts = rows,
                }, "Payout history retrieved.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Failed to load payout history for agent {AgentId}", agentUserId);
                return Result<AgentPayoutHistoryDto>.Failure(ErrorCodes.Exception,
                    "An error occurred while loading payout history.");
            }
        }

        // ── Record ─────────────────────────────────────────────────

        public async Task<Result<RecordAgentPayoutResponseDto>> RecordPayoutAsync(
            Guid agentUserId,
            RecordAgentPayoutRequest request,
            Guid recordedByUserId,
            CancellationToken ct = default)
        {
            try
            {
                if (request is null)
                    return Result<RecordAgentPayoutResponseDto>.Failure(
                        ErrorCodes.BadRequest, "Request is required.");

                // Money math is decimal; rejecting non-positive amounts
                // up front saves a DB round-trip on the obvious bad case.
                if (request.Amount <= 0m)
                    return Result<RecordAgentPayoutResponseDto>.Failure(
                        ErrorCodes.BadRequest, "Payment amount must be greater than zero.");

                // Cap to two decimals — the column is decimal(18,2). We
                // round here so a stray 9.999 from a flaky client doesn't
                // store as 9.99 silently with the wrong remainder.
                var amount = decimal.Round(request.Amount, 2, MidpointRounding.AwayFromZero);

                var agentCheck = await EnsureAgentExistsAsync(agentUserId);
                if (!agentCheck.IsSuccess)
                    return Result<RecordAgentPayoutResponseDto>.Failure(
                        agentCheck.Code, agentCheck.Message);

                // The admin must be a real user. We don't re-check role
                // here — the controller's [Authorize(Roles=…)] gate is
                // authoritative for "who can record". This is a safety
                // net so a missing/expired user can't write a payout
                // attributed to a nonexistent admin id.
                var recordedBy = await _userManager.FindByIdAsync(recordedByUserId.ToString());
                if (recordedBy is null)
                    return Result<RecordAgentPayoutResponseDto>.Failure(
                        ErrorCodes.Forbidden, "Recording admin not found.");

                // SERIALIZABLE so the (read summary → validate → insert)
                // window is atomic against concurrent RecordPayout calls
                // for the same agent. Without this an admin double-click
                // could legitimately race two valid-looking payouts that
                // jointly exceed outstanding.
                await using var tx = await _db.Database.BeginTransactionAsync(
                    IsolationLevel.Serializable, ct);

                var summary = await ComputeSummaryAsync(agentUserId, ct);

                if (summary.Outstanding <= 0m)
                    return Result<RecordAgentPayoutResponseDto>.Failure(
                        ErrorCodes.BadRequest, "No outstanding balance to pay.");

                if (amount > summary.Outstanding)
                    return Result<RecordAgentPayoutResponseDto>.Failure(
                        ErrorCodes.BadRequest,
                        $"Payment exceeds outstanding balance (R{summary.Outstanding:0.00}).");

                var now = DateTime.UtcNow;
                var paidAt = request.PaidAtUtc ?? now;
                // Guard against future-dated payouts — admins entering
                // "I'll pay them next week" is a process error, not a
                // ledger entry. Allow a 5-minute clock-skew tolerance.
                if (paidAt > now.AddMinutes(5))
                    return Result<RecordAgentPayoutResponseDto>.Failure(
                        ErrorCodes.BadRequest, "Paid date cannot be in the future.");

                var payout = new AgentPayout
                {
                    Id = Guid.NewGuid(),
                    AgentUserId = agentUserId,
                    Amount = amount,
                    PaidAtUtc = paidAt,
                    RecordedAtUtc = now,
                    RecordedByUserId = recordedByUserId,
                    PaymentReference = NullIfBlank(request.PaymentReference),
                    PaymentMethod = NullIfBlank(request.PaymentMethod),
                    Note = NullIfBlank(request.Note),
                };

                _db.AgentPayouts.Add(payout);
                await _db.SaveChangesAsync(ct);
                await tx.CommitAsync(ct);

                // Recompute AFTER insert so the response carries the
                // current snapshot. Cheap — one aggregate query.
                var fresh = await ComputeSummaryAsync(agentUserId, ct);

                var recordedByName = string.IsNullOrWhiteSpace(recordedBy.LastName)
                    ? recordedBy.FirstName ?? string.Empty
                    : $"{recordedBy.FirstName} {recordedBy.LastName}".Trim();

                var response = new RecordAgentPayoutResponseDto
                {
                    Payout = new AgentPayoutDto
                    {
                        Id = payout.Id,
                        AgentUserId = payout.AgentUserId,
                        Amount = payout.Amount,
                        PaidAtUtc = payout.PaidAtUtc,
                        RecordedAtUtc = payout.RecordedAtUtc,
                        RecordedByUserId = payout.RecordedByUserId,
                        RecordedByName = recordedByName,
                        PaymentReference = payout.PaymentReference,
                        PaymentMethod = payout.PaymentMethod,
                        Note = payout.Note,
                    },
                    Summary = fresh,
                };

                _logger.LogInformation(
                    "Recorded agent payout {PayoutId} amount {Amount} for agent {AgentId} by admin {AdminId}",
                    payout.Id, payout.Amount, agentUserId, recordedByUserId);

                return Result<RecordAgentPayoutResponseDto>.Success(response, "Payment recorded successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Failed to record payout for agent {AgentId} by admin {AdminId}",
                    agentUserId, recordedByUserId);
                return Result<RecordAgentPayoutResponseDto>.Failure(
                    ErrorCodes.Exception, "An error occurred while recording the payment.");
            }
        }

        // ── Helpers ────────────────────────────────────────────────

        /// <summary>
        /// Authoritative summary computation. Splits into two DB calls
        /// (lead counts + payout sum) because counting SellerLeads with
        /// a server-side aggregate AND summing AgentPayouts in the same
        /// query would force a Cartesian product join for no benefit.
        /// </summary>
        private async Task<AgentEarningsSummaryDto> ComputeSummaryAsync(
            Guid agentUserId, CancellationToken ct)
        {
            // Single round-trip via grouping — counts per status for
            // this agent. Anonymous type so EF Core builds an
            // efficient COUNT-with-CASE query.
            var counts = await _db.SellerLeads
                .AsNoTracking()
                .Where(l => l.AssignedUserId == agentUserId)
                .GroupBy(_ => 1)
                .Select(g => new
                {
                    Submitted = g.Count(),
                    Approved = g.Count(l => l.ApprovalStatus == ApprovalStatus.Approved),
                    Rejected = g.Count(l => l.ApprovalStatus == ApprovalStatus.Rejected),
                })
                .FirstOrDefaultAsync(ct);

            var submitted = counts?.Submitted ?? 0;
            var approved = counts?.Approved ?? 0;
            var rejected = counts?.Rejected ?? 0;

            var paidOut = await _db.AgentPayouts
                .AsNoTracking()
                .Where(p => p.AgentUserId == agentUserId)
                .SumAsync(p => (decimal?)p.Amount, ct) ?? 0m;

            var commissionPerLead = _settings.CommissionPerApprovedLead;
            var totalEarned = decimal.Round(commissionPerLead * approved, 2, MidpointRounding.AwayFromZero);
            var outstanding = decimal.Round(totalEarned - paidOut, 2, MidpointRounding.AwayFromZero);
            // Defensive: outstanding can technically go negative if
            // someone runs SQL by hand to backfill a manual payout
            // larger than current earnings. The UI clamps at zero so
            // the admin doesn't see "-R5 outstanding" and get confused;
            // the underlying ledger still records the truth.
            if (outstanding < 0m) outstanding = 0m;

            return new AgentEarningsSummaryDto
            {
                AgentUserId = agentUserId,
                ApprovedLeadsCount = approved,
                SubmittedLeadsCount = submitted,
                RejectedLeadsCount = rejected,
                CommissionPerLead = commissionPerLead,
                TotalEarned = totalEarned,
                TotalPaidOut = paidOut,
                Outstanding = outstanding,
            };
        }

        private async Task<Result> EnsureAgentExistsAsync(Guid agentUserId)
        {
            var user = await _userManager.FindByIdAsync(agentUserId.ToString());
            if (user is null)
                return Result.Failure(ErrorCodes.NotFound, "Agent not found.");

            var roles = await _userManager.GetRolesAsync(user);
            if (!roles.Contains(AgentRoleName))
                return Result.Failure(ErrorCodes.NotFound, "Agent not found.");

            return Result.Success();
        }

        private static string? NullIfBlank(string? s) =>
            string.IsNullOrWhiteSpace(s) ? null : s.Trim();
    }
}
