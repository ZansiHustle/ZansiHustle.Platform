using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ZansiHustle.Application.Campaigns.Dtos;
using ZansiHustle.Application.Persistence.Campaigns;
using ZansiHustle.Domain.Campaigns;
using ZansiHustle.Shared.Enums.Campaigns;
using ZansiHustle.Shared.Results;

namespace ZansiHustle.Application.Campaigns
{
    /// <summary>
    /// Provides business logic for campaign operations.
    /// </summary>
    public class CampaignService : ICampaignService
    {
        private readonly ICampaignRepository _campaignRepository;

        /// <summary>
        /// Creates a new instance of the <see cref="CampaignService"/> class.
        /// </summary>
        public CampaignService(ICampaignRepository campaignRepository)
        {
            _campaignRepository = campaignRepository;
        }

        /// <inheritdoc />
        public async Task<Result<List<CampaignListItemDto>>> GetAllAsync()
        {
            try
            {
                var campaigns = await _campaignRepository.GetAllAsync();
                var data = campaigns.Select(MapToListItemDto).ToList();

                return Result<List<CampaignListItemDto>>.Success(data, "Campaigns retrieved successfully.");
            }
            catch (Exception ex)
            {
                return Result<List<CampaignListItemDto>>.Failure($"An error occurred while retrieving campaigns. {ex.Message}");
            }
        }

        /// <inheritdoc />
        public async Task<Result<CampaignDetailsDto>> GetByIdAsync(Guid id)
        {
            try
            {
                var campaign = await _campaignRepository.GetByIdAsync(id);

                if (campaign is null)
                {
                    return Result<CampaignDetailsDto>.Failure("Campaign not found.");
                }

                return Result<CampaignDetailsDto>.Success(MapToDetailsDto(campaign), "Campaign retrieved successfully.");
            }
            catch (Exception ex)
            {
                return Result<CampaignDetailsDto>.Failure($"An error occurred while retrieving the campaign. {ex.Message}");
            }
        }

        /// <inheritdoc />
        public async Task<Result<CampaignDetailsDto>> CreateAsync(CreateCampaignRequestDto request)
        {
            try
            {
                if (request is null)
                {
                    return Result<CampaignDetailsDto>.Failure("Request is required.");
                }

                if (string.IsNullOrWhiteSpace(request.Name))
                {
                    return Result<CampaignDetailsDto>.Failure("Name is required.");
                }

                if (request.EndDateUtc < request.StartDateUtc)
                {
                    return Result<CampaignDetailsDto>.Failure("End date cannot be earlier than start date.");
                }

                var entity = new Campaign
                {
                    Id = Guid.NewGuid(),
                    Code = $"CAM-{DateTime.UtcNow:yyyyMMddHHmmssfff}",
                    Name = request.Name.Trim(),
                    Description = request.Description?.Trim(),
                    CampaignType = request.CampaignType?.Trim(),
                    PrimaryPlatform = request.PrimaryPlatform,
                    Budget = request.Budget,
                    StartDateUtc = request.StartDateUtc,
                    EndDateUtc = request.EndDateUtc,
                    CreatedByUserId = request.CreatedByUserId,
                    Status = CampaignStatus.Planned,
                    CreatedAtUtc = DateTime.UtcNow
                };

                await _campaignRepository.AddAsync(entity);

                var saved = await _campaignRepository.SaveChangesAsync();

                if (!saved)
                {
                    return Result<CampaignDetailsDto>.Failure("Failed to create campaign.");
                }

                return Result<CampaignDetailsDto>.Success(MapToDetailsDto(entity), "Campaign created successfully.");
            }
            catch (Exception ex)
            {
                return Result<CampaignDetailsDto>.Failure($"An error occurred while creating the campaign. {ex.Message}");
            }
        }

        /// <inheritdoc />
        public async Task<Result<CampaignDetailsDto>> UpdateAsync(Guid id, UpdateCampaignRequestDto request)
        {
            try
            {
                if (request is null)
                {
                    return Result<CampaignDetailsDto>.Failure("Request is required.");
                }

                if (string.IsNullOrWhiteSpace(request.Name))
                {
                    return Result<CampaignDetailsDto>.Failure("Name is required.");
                }

                if (request.EndDateUtc < request.StartDateUtc)
                {
                    return Result<CampaignDetailsDto>.Failure("End date cannot be earlier than start date.");
                }

                var campaign = await _campaignRepository.GetByIdAsync(id);

                if (campaign is null)
                {
                    return Result<CampaignDetailsDto>.Failure("Campaign not found.");
                }

                campaign.Name = request.Name.Trim();
                campaign.Description = request.Description?.Trim();
                campaign.CampaignType = request.CampaignType?.Trim();
                campaign.Status = request.Status;
                campaign.PrimaryPlatform = request.PrimaryPlatform;
                campaign.Budget = request.Budget;
                campaign.StartDateUtc = request.StartDateUtc;
                campaign.EndDateUtc = request.EndDateUtc;
                campaign.UpdatedAtUtc = DateTime.UtcNow;

                _campaignRepository.Update(campaign);

                var saved = await _campaignRepository.SaveChangesAsync();

                if (!saved)
                {
                    return Result<CampaignDetailsDto>.Failure("Failed to update campaign.");
                }

                return Result<CampaignDetailsDto>.Success(MapToDetailsDto(campaign), "Campaign updated successfully.");
            }
            catch (Exception ex)
            {
                return Result<CampaignDetailsDto>.Failure($"An error occurred while updating the campaign. {ex.Message}");
            }
        }

        /// <inheritdoc />
        public async Task<Result> DeleteAsync(Guid id)
        {
            try
            {
                var campaign = await _campaignRepository.GetByIdAsync(id);

                if (campaign is null)
                {
                    return Result.Failure("Campaign not found.");
                }

                _campaignRepository.Delete(campaign);

                var saved = await _campaignRepository.SaveChangesAsync();

                if (!saved)
                {
                    return Result.Failure("Failed to delete campaign.");
                }

                return Result.Success("Campaign deleted successfully.");
            }
            catch (Exception ex)
            {
                return Result.Failure($"An error occurred while deleting the campaign. {ex.Message}");
            }
        }

        private static CampaignListItemDto MapToListItemDto(Campaign campaign)
        {
            return new CampaignListItemDto
            {
                Id = campaign.Id,
                Code = campaign.Code,
                Name = campaign.Name,
                CampaignType = campaign.CampaignType,
                Status = campaign.Status,
                PrimaryPlatform = campaign.PrimaryPlatform,
                Budget = campaign.Budget,
                StartDateUtc = campaign.StartDateUtc,
                EndDateUtc = campaign.EndDateUtc,
                ContentTasksCount = campaign.ContentTasks.Count,
                MetricSnapshotsCount = campaign.MetricSnapshots.Count,
                CreatedAtUtc = campaign.CreatedAtUtc
            };
        }

        private static CampaignDetailsDto MapToDetailsDto(Campaign campaign)
        {
            return new CampaignDetailsDto
            {
                Id = campaign.Id,
                Code = campaign.Code,
                Name = campaign.Name,
                Description = campaign.Description,
                CampaignType = campaign.CampaignType,
                Status = campaign.Status,
                PrimaryPlatform = campaign.PrimaryPlatform,
                Budget = campaign.Budget,
                StartDateUtc = campaign.StartDateUtc,
                EndDateUtc = campaign.EndDateUtc,
                CreatedByUserId = campaign.CreatedByUserId,
                ContentTasksCount = campaign.ContentTasks.Count,
                MetricSnapshotsCount = campaign.MetricSnapshots.Count,
                CreatedAtUtc = campaign.CreatedAtUtc,
                UpdatedAtUtc = campaign.UpdatedAtUtc
            };
        }
    }
}
