using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ZansiHustle.Application.Reports;
using ZansiHustle.Application.Reports.Dtos;
using ZansiHustle.Domain.Identity;
using ZansiHustle.Domain.Reports;
using ZansiHustle.Infrastructure.Data;
using ZansiHustle.Shared.Enums.Reports;
using ZansiHustle.Shared.Errors;
using ZansiHustle.Shared.Results;

namespace ZansiHustle.Infrastructure.Reports
{
    /// <summary>
    /// DB-direct content-report service (Infrastructure convention shared with
    /// ChatService). See <see cref="IReportService"/>.
    /// </summary>
    public sealed class ReportService : IReportService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<ReportService> _logger;

        public ReportService(AppDbContext context, ILogger<ReportService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<Result<Guid>> CreateAsync(Guid reporterUserId, CreateReportRequestDto request)
        {
            try
            {
                if (request is null)
                    return Result<Guid>.Failure(ErrorCodes.BadRequest, "Request is required.");
                if (request.TargetId == Guid.Empty)
                    return Result<Guid>.Failure(ErrorCodes.BadRequest, "A report target is required.");

                // De-dupe: one active (unresolved) report per reporter+target.
                var existing = await _context.Set<ContentReport>().FirstOrDefaultAsync(r =>
                    r.ReporterUserId == reporterUserId &&
                    r.TargetType == request.TargetType &&
                    r.TargetId == request.TargetId &&
                    (r.Status == ReportStatus.Pending || r.Status == ReportStatus.Reviewing));
                if (existing is not null)
                    return Result<Guid>.Success(existing.Id, "You've already reported this. Our team is reviewing it.");

                var report = new ContentReport
                {
                    Id = Guid.NewGuid(),
                    ReporterUserId = reporterUserId,
                    TargetType = request.TargetType,
                    TargetId = request.TargetId,
                    TargetOwnerUserId = request.TargetOwnerUserId,
                    Reason = request.Reason,
                    Description = string.IsNullOrWhiteSpace(request.Description)
                        ? null
                        : request.Description.Trim(),
                    Status = ReportStatus.Pending,
                    CreatedAtUtc = DateTime.UtcNow,
                };

                await _context.Set<ContentReport>().AddAsync(report);
                await _context.SaveChangesAsync();

                _logger.LogInformation(
                    "Content report {ReportId} filed by {Reporter} on {TargetType}:{TargetId} reason={Reason}",
                    report.Id, reporterUserId, request.TargetType, request.TargetId, request.Reason);

                return Result<Guid>.Success(report.Id, "Thanks — your report has been submitted.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create content report for {Reporter}.", reporterUserId);
                return Result<Guid>.Failure(ErrorCodes.Exception, "Couldn't submit your report. Please try again.");
            }
        }

        public async Task<Result<ReportPageDto>> ListAsync(int page, int pageSize, ReportStatus? status)
        {
            try
            {
                page = Math.Max(1, page);
                pageSize = Math.Clamp(pageSize, 1, 100);

                var query = _context.Set<ContentReport>().AsNoTracking().AsQueryable();
                if (status.HasValue)
                    query = query.Where(r => r.Status == status.Value);

                var total = await query.CountAsync();

                var rows = await query
                    .OrderByDescending(r => r.CreatedAtUtc)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                // Resolve reporter + owner display names in one round-trip.
                var userIds = rows.Select(r => r.ReporterUserId)
                    .Concat(rows.Where(r => r.TargetOwnerUserId.HasValue).Select(r => r.TargetOwnerUserId!.Value))
                    .Distinct()
                    .ToList();
                var names = await _context.Set<User>()
                    .AsNoTracking()
                    .Where(u => userIds.Contains(u.Id))
                    .Select(u => new { u.Id, Name = ((u.FirstName ?? "") + " " + (u.LastName ?? "")).Trim() })
                    .ToDictionaryAsync(x => x.Id, x => x.Name);

                var items = rows.Select(r => new ReportDto
                {
                    Id = r.Id,
                    ReporterUserId = r.ReporterUserId,
                    ReporterName = names.TryGetValue(r.ReporterUserId, out var rn) && rn.Length > 0 ? rn : null,
                    TargetType = r.TargetType,
                    TargetId = r.TargetId,
                    TargetOwnerUserId = r.TargetOwnerUserId,
                    TargetOwnerName = r.TargetOwnerUserId.HasValue && names.TryGetValue(r.TargetOwnerUserId.Value, out var on) && on.Length > 0 ? on : null,
                    Reason = r.Reason,
                    Description = r.Description,
                    Status = r.Status,
                    ResolutionNote = r.ResolutionNote,
                    ReviewedByUserId = r.ReviewedByUserId,
                    ReviewedAtUtc = r.ReviewedAtUtc,
                    CreatedAtUtc = r.CreatedAtUtc,
                }).ToList();

                return Result<ReportPageDto>.Success(new ReportPageDto
                {
                    Items = items,
                    Total = total,
                    Page = page,
                    PageSize = pageSize,
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to list content reports.");
                return Result<ReportPageDto>.Failure(ErrorCodes.Exception, "Couldn't load reports.");
            }
        }

        public async Task<Result> ResolveAsync(Guid reportId, Guid moderatorUserId, ResolveReportRequestDto request)
        {
            try
            {
                if (request is null)
                    return Result.Failure(ErrorCodes.BadRequest, "Request is required.");
                if (request.Status != ReportStatus.ActionTaken && request.Status != ReportStatus.Dismissed)
                    return Result.Failure(ErrorCodes.BadRequest, "Resolve status must be ActionTaken or Dismissed.");

                var report = await _context.Set<ContentReport>().FirstOrDefaultAsync(r => r.Id == reportId);
                if (report is null)
                    return Result.Failure(ErrorCodes.NotFound, "Report not found.");

                report.Status = request.Status;
                report.ResolutionNote = string.IsNullOrWhiteSpace(request.ResolutionNote) ? null : request.ResolutionNote.Trim();
                report.ReviewedByUserId = moderatorUserId;
                report.ReviewedAtUtc = DateTime.UtcNow;
                report.UpdatedAtUtc = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                return Result.Success("Report resolved.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to resolve report {ReportId}.", reportId);
                return Result.Failure(ErrorCodes.Exception, "Couldn't resolve the report.");
            }
        }
    }
}
