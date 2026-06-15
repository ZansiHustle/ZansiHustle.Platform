using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ZansiHustle.Application.Sellers.Incidents;
using ZansiHustle.Domain.Merchants;
using ZansiHustle.Infrastructure.Data;
using ZansiHustle.Shared.Enums.Merchants;
using ZansiHustle.Shared.Errors;
using ZansiHustle.Shared.Results;

namespace ZansiHustle.Infrastructure.Sellers
{
    /// <summary>
    /// Records and (admin-)resolves seller fulfilment incidents against the
    /// <c>SellerIncidents</c> table. Talks to <see cref="AppDbContext"/> directly.
    /// Conservative V1: recording does not auto-charge; an admin applies/waives.
    /// </summary>
    public sealed class SellerIncidentService : ISellerIncidentService
    {
        private readonly AppDbContext _db;
        private readonly ILogger<SellerIncidentService> _logger;

        public SellerIncidentService(AppDbContext db, ILogger<SellerIncidentService> logger)
        {
            _db = db;
            _logger = logger;
        }

        public async Task<Result<Guid>> RecordAsync(RecordSellerIncidentInput input, CancellationToken ct = default)
        {
            try
            {
                if (input is null || input.MerchantId == Guid.Empty)
                    return Result<Guid>.Failure(ErrorCodes.BadRequest, "A merchant is required to record an incident.");

                var incident = new SellerIncident
                {
                    Id = Guid.NewGuid(),
                    MerchantId = input.MerchantId,
                    OrderId = input.OrderId,
                    IncidentType = input.IncidentType,
                    Severity = input.Severity,
                    Amount = input.Amount < 0m ? 0m : input.Amount,
                    Currency = string.IsNullOrWhiteSpace(input.Currency) ? "ZAR" : input.Currency,
                    Status = SellerIncidentStatus.PendingReview,
                    Reason = input.Reason?.Trim() ?? string.Empty,
                    CreatedAtUtc = DateTime.UtcNow,
                };

                _db.SellerIncidents.Add(incident);
                await _db.SaveChangesAsync(ct);
                return Result<Guid>.Success(incident.Id, "Incident recorded.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to record seller incident for merchant {MerchantId}.", input?.MerchantId);
                return Result<Guid>.Failure(ErrorCodes.Exception, "Could not record the incident.");
            }
        }

        public async Task<Result> ApplyAsync(Guid incidentId, string? adminNotes, CancellationToken ct = default)
        {
            try
            {
                var incident = await _db.SellerIncidents.FirstOrDefaultAsync(x => x.Id == incidentId, ct);
                if (incident is null) return Result.Failure(ErrorCodes.NotFound, "Incident not found.");
                if (incident.Status == SellerIncidentStatus.Applied) return Result.Success("Already applied.");

                incident.Status = SellerIncidentStatus.Applied;
                incident.AppliedAtUtc = DateTime.UtcNow;
                if (!string.IsNullOrWhiteSpace(adminNotes)) incident.AdminNotes = adminNotes.Trim();
                await _db.SaveChangesAsync(ct);
                return Result.Success("Penalty applied.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to apply seller incident {IncidentId}.", incidentId);
                return Result.Failure(ErrorCodes.Exception, "Could not apply the penalty.");
            }
        }

        public async Task<Result> WaiveAsync(Guid incidentId, string? adminNotes, CancellationToken ct = default)
        {
            try
            {
                var incident = await _db.SellerIncidents.FirstOrDefaultAsync(x => x.Id == incidentId, ct);
                if (incident is null) return Result.Failure(ErrorCodes.NotFound, "Incident not found.");

                incident.Status = SellerIncidentStatus.Waived;
                if (!string.IsNullOrWhiteSpace(adminNotes)) incident.AdminNotes = adminNotes.Trim();
                await _db.SaveChangesAsync(ct);
                return Result.Success("Incident waived.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to waive seller incident {IncidentId}.", incidentId);
                return Result.Failure(ErrorCodes.Exception, "Could not waive the incident.");
            }
        }

        public async Task<decimal> GetAppliedPenaltyTotalAsync(Guid merchantId, CancellationToken ct = default)
        {
            if (merchantId == Guid.Empty) return 0m;
            return await _db.SellerIncidents.AsNoTracking()
                .Where(x => x.MerchantId == merchantId && x.Status == SellerIncidentStatus.Applied)
                .SumAsync(x => (decimal?)x.Amount, ct) ?? 0m;
        }
    }
}
