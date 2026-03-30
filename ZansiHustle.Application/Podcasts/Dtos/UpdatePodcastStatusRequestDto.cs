using ZansiHustle.Shared.Enums.Podcasts;

namespace ZansiHustle.Application.Podcasts.Dtos
{
    /// <summary>
    /// Request model used to update podcast workflow statuses.
    /// </summary>
    public class UpdatePodcastStatusRequestDto
    {
        public PodcastOutreachStatus OutreachStatus { get; set; }
        public PodcastResponseStatus ResponseStatus { get; set; }
    }
}
