using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ZansiHustle.Application.Influencers.Dtos;
using ZansiHustle.Shared.Results;

namespace ZansiHustle.Application.Influencers
{
    /// <summary>
    /// Service contract for influencer operations.
    /// </summary>
    public interface IInfluencerService
    {
        Task<Result<List<InfluencerListItemDto>>> GetAllAsync();
        Task<Result<InfluencerDetailsDto>> GetByIdAsync(Guid id);
        Task<Result<InfluencerDetailsDto>> CreateAsync(CreateInfluencerRequestDto request);
        Task<Result<InfluencerDetailsDto>> UpdateAsync(Guid id, UpdateInfluencerRequestDto request);
        Task<Result<InfluencerDetailsDto>> UpdateStatusAsync(Guid id, UpdateInfluencerStatusRequestDto request);
        Task<Result> DeleteAsync(Guid id);
    }
}
