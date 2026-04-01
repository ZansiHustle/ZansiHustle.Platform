using System;
using ZansiHustle.Shared.Enums.Common;
using ZansiHustle.Shared.Enums.Content;

namespace ZansiHustle.Application.ContentTasks.Dtos
{
    /// <summary>
    /// Lightweight content task DTO for list screens.
    /// </summary>
    public class ContentTaskListItemDto
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public ContentTaskStatus Status { get; set; }
        public PriorityLevel Priority { get; set; }
        public DateTime? DueDateUtc { get; set; }
        public Guid? CampaignId { get; set; }
        public string? CampaignName { get; set; }
        public Guid? AssignedToUserId { get; set; }
        public DateTime CreatedAtUtc { get; set; }
    }
}
