using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ZansiHustle.Application.Campaigns.Dtos;
using ZansiHustle.Shared.Results;

namespace ZansiHustle.Application.Campaigns
{
    /// <summary>
    /// Service contract for campaign operations.
    /// </summary>
    public interface ICampaignService
    {
        Task<Result<List<CampaignListItemDto>>> GetAllAsync();
        Task<Result<CampaignDetailsDto>> GetByIdAsync(Guid id);
        Task<Result<CampaignDetailsDto>> CreateAsync(CreateCampaignRequestDto request);
        Task<Result<CampaignDetailsDto>> UpdateAsync(Guid id, UpdateCampaignRequestDto request);
        Task<Result> DeleteAsync(Guid id);
    }
}
