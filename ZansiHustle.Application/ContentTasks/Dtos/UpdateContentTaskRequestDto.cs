using System;
using ZansiHustle.Shared.Enums.Common;
using ZansiHustle.Shared.Enums.Content;

namespace ZansiHustle.Application.ContentTasks.Dtos
{
    /// <summary>
    /// Request model used to update an existing content task.
    /// </summary>
    public class UpdateContentTaskRequestDto
    {
        public string Title { get; set; } = string.Empty;
        public ContentTaskStatus Status { get; set; }
        public PriorityLevel Priority { get; set; }
        public DateTime? DueDateUtc { get; set; }
        public Guid? CampaignId { get; set; }
        public Guid? AssignedToUserId { get; set; }
        public string? Notes { get; set; }
    }
}
