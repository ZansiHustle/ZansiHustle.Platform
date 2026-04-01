using System;
using ZansiHustle.Shared.Enums.Common;
using ZansiHustle.Shared.Enums.Content;

namespace ZansiHustle.Application.ContentTasks.Dtos
{
    /// <summary>
    /// Detailed content task DTO for detail screens.
    /// </summary>
    public class ContentTaskDetailsDto
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public ContentTaskStatus Status { get; set; }
        public PriorityLevel Priority { get; set; }
        public DateTime? DueDateUtc { get; set; }
        public Guid? CampaignId { get; set; }
        public string? CampaignName { get; set; }
        public Guid? AssignedToUserId { get; set; }
        public string? Notes { get; set; }
        public DateTime CreatedAtUtc { get; set; }
        public DateTime? UpdatedAtUtc { get; set; }
    }
}
