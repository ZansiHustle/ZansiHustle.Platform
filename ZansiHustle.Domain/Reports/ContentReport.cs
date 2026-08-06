using System;
using ZansiHustle.Shared.Enums.Reports;

namespace ZansiHustle.Domain.Reports
{
    /// <summary>
    /// A user's report of objectionable user-generated content (App Store
    /// Guideline 1.2). Polymorphic via (<see cref="TargetType"/>,
    /// <see cref="TargetId"/>), the same shape as <c>Review</c>. Feeds the
    /// admin-portal moderation queue.
    /// </summary>
    public class ContentReport
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>The user who filed the report.</summary>
        public Guid ReporterUserId { get; set; }

        /// <summary>What kind of object is being reported.</summary>
        public ReportTargetType TargetType { get; set; }

        /// <summary>The id of the reported object (its own aggregate key).</summary>
        public Guid TargetId { get; set; }

        /// <summary>
        /// Best-effort id of the user who OWNS/authored the reported content
        /// (nullable — not always resolvable at report time). Lets moderators
        /// jump straight to suspending the offender.
        /// </summary>
        public Guid? TargetOwnerUserId { get; set; }

        /// <summary>Selected reason category.</summary>
        public ReportReason Reason { get; set; }

        /// <summary>Optional free-text detail from the reporter (max 1000).</summary>
        public string? Description { get; set; }

        /// <summary>Moderation lifecycle state.</summary>
        public ReportStatus Status { get; set; } = ReportStatus.Pending;

        /// <summary>Moderator's note recorded when the report is resolved.</summary>
        public string? ResolutionNote { get; set; }

        /// <summary>Admin/moderator who actioned the report.</summary>
        public Guid? ReviewedByUserId { get; set; }

        /// <summary>When the report was resolved.</summary>
        public DateTime? ReviewedAtUtc { get; set; }

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAtUtc { get; set; }
    }
}
