using System;
using ZansiHustle.Shared.Enums.Reviews;

namespace ZansiHustle.Application.Reviews.Dtos
{
    /// <summary>
    /// Request body for `POST /api/reviews`. The (TargetType, TargetId)
    /// pair is required; the service validates that the target exists
    /// and that the current user is allowed to review it.
    /// </summary>
    public class CreateReviewRequestDto
    {
        public ReviewTargetType TargetType { get; set; }
        public Guid TargetId { get; set; }
        public int Rating { get; set; }
        public string? Comment { get; set; }
    }
}
