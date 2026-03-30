using ZansiHustle.Shared.Enums.Influencers;

namespace ZansiHustle.Application.Influencers.Dtos
{
    /// <summary>
    /// Request model used to update influencer status.
    /// </summary>
    public class UpdateInfluencerStatusRequestDto
    {
        public InfluencerStatus Status { get; set; }
    }
}
