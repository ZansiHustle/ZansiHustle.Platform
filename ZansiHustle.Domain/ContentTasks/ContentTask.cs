using System;
using ZansiHustle.Domain.Campaigns;
using ZansiHustle.Shared.Enums.Common;
using ZansiHustle.Shared.Enums.Content;

namespace ZansiHustle.Domain.ContentTasks
{
    public class ContentTask
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public ContentTaskStatus Status { get; set; } = ContentTaskStatus.Todo;
        public PriorityLevel Priority { get; set; } = PriorityLevel.Medium;
        public DateTime? DueDateUtc { get; set; }
        public Guid? CampaignId { get; set; }
        public virtual Campaign? Campaign { get; set; }
        public Guid? AssignedToUserId { get; set; }
        public string? Notes { get; set; }
        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAtUtc { get; set; }
    }
}
