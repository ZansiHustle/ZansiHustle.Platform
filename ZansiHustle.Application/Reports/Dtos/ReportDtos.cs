using System;
using System.Collections.Generic;
using ZansiHustle.Shared.Enums.Reports;

namespace ZansiHustle.Application.Reports.Dtos
{
    /// <summary>Body for <c>POST /api/reports</c> (a user reporting content).</summary>
    public class CreateReportRequestDto
    {
        public ReportTargetType TargetType { get; set; }
        public Guid TargetId { get; set; }
        /// <summary>Optional — owner/author of the reported content, if the client knows it.</summary>
        public Guid? TargetOwnerUserId { get; set; }
        public ReportReason Reason { get; set; }
        public string? Description { get; set; }
    }

    /// <summary>Admin/moderation projection of a report.</summary>
    public class ReportDto
    {
        public Guid Id { get; set; }
        public Guid ReporterUserId { get; set; }
        public string? ReporterName { get; set; }
        public ReportTargetType TargetType { get; set; }
        public Guid TargetId { get; set; }
        public Guid? TargetOwnerUserId { get; set; }
        public string? TargetOwnerName { get; set; }
        public ReportReason Reason { get; set; }
        public string? Description { get; set; }
        public ReportStatus Status { get; set; }
        public string? ResolutionNote { get; set; }
        public Guid? ReviewedByUserId { get; set; }
        public DateTime? ReviewedAtUtc { get; set; }
        public DateTime CreatedAtUtc { get; set; }
    }

    /// <summary>Body for admin resolve: <c>POST /api/admin/reports/{id}/resolve</c>.</summary>
    public class ResolveReportRequestDto
    {
        /// <summary>New status — must be ActionTaken (3) or Dismissed (4).</summary>
        public ReportStatus Status { get; set; }
        public string? ResolutionNote { get; set; }
    }

    /// <summary>Paged admin listing envelope.</summary>
    public class ReportPageDto
    {
        public List<ReportDto> Items { get; set; } = new();
        public int Total { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
    }
}
