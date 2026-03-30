using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ZansiHustle.Application.Podcasts.Dtos;
using ZansiHustle.Shared.Results;

namespace ZansiHustle.Application.Podcasts
{
    /// <summary>
    /// Service contract for podcast operations.
    /// </summary>
    public interface IPodcastService
    {
        Task<Result<List<PodcastListItemDto>>> GetAllAsync();
        Task<Result<PodcastDetailsDto>> GetByIdAsync(Guid id);
        Task<Result<PodcastDetailsDto>> CreateAsync(CreatePodcastRequestDto request);
        Task<Result<PodcastDetailsDto>> UpdateAsync(Guid id, UpdatePodcastRequestDto request);
        Task<Result<PodcastDetailsDto>> UpdateStatusAsync(Guid id, UpdatePodcastStatusRequestDto request);
        Task<Result> DeleteAsync(Guid id);
    }
}
