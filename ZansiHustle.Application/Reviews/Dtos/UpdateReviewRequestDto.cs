namespace ZansiHustle.Application.Reviews.Dtos
{
    /// <summary>
    /// Request body for `PUT /api/reviews/{id}`. Owner-only; the
    /// service reads the route id and confirms it belongs to the
    /// caller before applying changes. Target type/id are immutable
    /// after creation, so they aren't on this DTO.
    /// </summary>
    public class UpdateReviewRequestDto
    {
        public int Rating { get; set; }
        public string? Comment { get; set; }
    }
}
