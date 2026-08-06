using System;
using System.Threading.Tasks;
using ZansiHustle.Application.Reports.Dtos;
using ZansiHustle.Shared.Enums.Reports;
using ZansiHustle.Shared.Results;

namespace ZansiHustle.Application.Reports
{
    /// <summary>
    /// Content-report service (App Store Guideline 1.2). Users file reports;
    /// moderators (admin portal) list and resolve them.
    /// </summary>
    public interface IReportService
    {
        /// <summary>File a report on a piece of content. De-duplicated per
        /// (reporter, target).</summary>
        Task<Result<Guid>> CreateAsync(Guid reporterUserId, CreateReportRequestDto request);

        /// <summary>Admin: paged moderation queue, optional status filter.</summary>
        Task<Result<ReportPageDto>> ListAsync(int page, int pageSize, ReportStatus? status);

        /// <summary>Admin: resolve a report (ActionTaken / Dismissed).</summary>
        Task<Result> ResolveAsync(Guid reportId, Guid moderatorUserId, ResolveReportRequestDto request);
    }
}
