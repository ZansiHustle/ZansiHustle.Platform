using System;
using ZansiHustle.Shared.Enums.Common;

namespace ZansiHustle.Application.ContentTasks.Dtos
{
    /// <summary>
    /// Request model used to create a new content task.
    /// </summary>
    public class CreateContentTaskRequestDto
    {
        public string Title { get; set; } = string.Empty;
        public PriorityLevel Priority { get; set; }
        public DateTime? DueDateUtc { get; set; }
        public Guid? CampaignId { get; set; }
        public Guid? AssignedToUserId { get; set; }
        public string? Notes { get; set; }
    }
}
