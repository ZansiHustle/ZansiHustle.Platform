using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ZansiHustle.Application.ContentTasks.Dtos;
using ZansiHustle.Application.Persistence.Campaigns;
using ZansiHustle.Application.Persistence.ContentTasks;
using ZansiHustle.Domain.ContentTasks;
using ZansiHustle.Shared.Enums.Content;
using ZansiHustle.Shared.Results;

namespace ZansiHustle.Application.ContentTasks
{
    /// <summary>
    /// Provides business logic for content task operations.
    /// </summary>
    public class ContentTaskService : IContentTaskService
    {
        private readonly IContentTaskRepository _contentTaskRepository;
        private readonly ICampaignRepository _campaignRepository;

        /// <summary>
        /// Creates a new instance of the <see cref="ContentTaskService"/> class.
        /// </summary>
        public ContentTaskService(IContentTaskRepository contentTaskRepository, ICampaignRepository campaignRepository)
        {
            _contentTaskRepository = contentTaskRepository;
            _campaignRepository = campaignRepository;
        }

        /// <inheritdoc />
        public async Task<Result<List<ContentTaskListItemDto>>> GetAllAsync()
        {
            try
            {
                var tasks = await _contentTaskRepository.GetAllAsync();
                var data = tasks.Select(MapToListItemDto).ToList();

                return Result<List<ContentTaskListItemDto>>.Success(data, "Content tasks retrieved successfully.");
            }
            catch (Exception ex)
            {
                return Result<List<ContentTaskListItemDto>>.Failure($"An error occurred while retrieving content tasks. {ex.Message}");
            }
        }

        /// <inheritdoc />
        public async Task<Result<ContentTaskDetailsDto>> GetByIdAsync(Guid id)
        {
            try
            {
                var task = await _contentTaskRepository.GetByIdAsync(id);

                if (task is null)
                {
                    return Result<ContentTaskDetailsDto>.Failure("Content task not found.");
                }

                return Result<ContentTaskDetailsDto>.Success(MapToDetailsDto(task), "Content task retrieved successfully.");
            }
            catch (Exception ex)
            {
                return Result<ContentTaskDetailsDto>.Failure($"An error occurred while retrieving the content task. {ex.Message}");
            }
        }

        /// <inheritdoc />
        public async Task<Result<ContentTaskDetailsDto>> CreateAsync(CreateContentTaskRequestDto request)
        {
            try
            {
                if (request is null)
                {
                    return Result<ContentTaskDetailsDto>.Failure("Request is required.");
                }

                if (string.IsNullOrWhiteSpace(request.Title))
                {
                    return Result<ContentTaskDetailsDto>.Failure("Title is required.");
                }

                if (request.CampaignId.HasValue)
                {
                    var campaign = await _campaignRepository.GetByIdAsync(request.CampaignId.Value);

                    if (campaign is null)
                    {
                        return Result<ContentTaskDetailsDto>.Failure("Selected campaign was not found.");
                    }
                }

                var entity = new ContentTask
                {
                    Id = Guid.NewGuid(),
                    Title = request.Title.Trim(),
                    Status = ContentTaskStatus.Todo,
                    Priority = request.Priority,
                    DueDateUtc = request.DueDateUtc,
                    CampaignId = request.CampaignId,
                    AssignedToUserId = request.AssignedToUserId,
                    Notes = request.Notes?.Trim(),
                    CreatedAtUtc = DateTime.UtcNow
                };

                await _contentTaskRepository.AddAsync(entity);

                var saved = await _contentTaskRepository.SaveChangesAsync();

                if (!saved)
                {
                    return Result<ContentTaskDetailsDto>.Failure("Failed to create content task.");
                }

                var createdEntity = await _contentTaskRepository.GetByIdAsync(entity.Id) ?? entity;

                return Result<ContentTaskDetailsDto>.Success(MapToDetailsDto(createdEntity), "Content task created successfully.");
            }
            catch (Exception ex)
            {
                return Result<ContentTaskDetailsDto>.Failure($"An error occurred while creating the content task. {ex.Message}");
            }
        }

        /// <inheritdoc />
        public async Task<Result<ContentTaskDetailsDto>> UpdateAsync(Guid id, UpdateContentTaskRequestDto request)
        {
            try
            {
                if (request is null)
                {
                    return Result<ContentTaskDetailsDto>.Failure("Request is required.");
                }

                if (string.IsNullOrWhiteSpace(request.Title))
                {
                    return Result<ContentTaskDetailsDto>.Failure("Title is required.");
                }

                if (request.CampaignId.HasValue)
                {
                    var campaign = await _campaignRepository.GetByIdAsync(request.CampaignId.Value);

                    if (campaign is null)
                    {
                        return Result<ContentTaskDetailsDto>.Failure("Selected campaign was not found.");
                    }
                }

                var task = await _contentTaskRepository.GetByIdAsync(id);

                if (task is null)
                {
                    return Result<ContentTaskDetailsDto>.Failure("Content task not found.");
                }

                task.Title = request.Title.Trim();
                task.Status = request.Status;
                task.Priority = request.Priority;
                task.DueDateUtc = request.DueDateUtc;
                task.CampaignId = request.CampaignId;
                task.AssignedToUserId = request.AssignedToUserId;
                task.Notes = request.Notes?.Trim();
                task.UpdatedAtUtc = DateTime.UtcNow;

                _contentTaskRepository.Update(task);

                var saved = await _contentTaskRepository.SaveChangesAsync();

                if (!saved)
                {
                    return Result<ContentTaskDetailsDto>.Failure("Failed to update content task.");
                }

                var updatedEntity = await _contentTaskRepository.GetByIdAsync(task.Id) ?? task;

                return Result<ContentTaskDetailsDto>.Success(MapToDetailsDto(updatedEntity), "Content task updated successfully.");
            }
            catch (Exception ex)
            {
                return Result<ContentTaskDetailsDto>.Failure($"An error occurred while updating the content task. {ex.Message}");
            }
        }

        /// <inheritdoc />
        public async Task<Result> DeleteAsync(Guid id)
        {
            try
            {
                var task = await _contentTaskRepository.GetByIdAsync(id);

                if (task is null)
                {
                    return Result.Failure("Content task not found.");
                }

                _contentTaskRepository.Delete(task);

                var saved = await _contentTaskRepository.SaveChangesAsync();

                if (!saved)
                {
                    return Result.Failure("Failed to delete content task.");
                }

                return Result.Success("Content task deleted successfully.");
            }
            catch (Exception ex)
            {
                return Result.Failure($"An error occurred while deleting the content task. {ex.Message}");
            }
        }

        private static ContentTaskListItemDto MapToListItemDto(ContentTask task)
        {
            return new ContentTaskListItemDto
            {
                Id = task.Id,
                Title = task.Title,
                Status = task.Status,
                Priority = task.Priority,
                DueDateUtc = task.DueDateUtc,
                CampaignId = task.CampaignId,
                CampaignName = task.Campaign?.Name,
                AssignedToUserId = task.AssignedToUserId,
                CreatedAtUtc = task.CreatedAtUtc
            };
        }

        private static ContentTaskDetailsDto MapToDetailsDto(ContentTask task)
        {
            return new ContentTaskDetailsDto
            {
                Id = task.Id,
                Title = task.Title,
                Status = task.Status,
                Priority = task.Priority,
                DueDateUtc = task.DueDateUtc,
                CampaignId = task.CampaignId,
                CampaignName = task.Campaign?.Name,
                AssignedToUserId = task.AssignedToUserId,
                Notes = task.Notes,
                CreatedAtUtc = task.CreatedAtUtc,
                UpdatedAtUtc = task.UpdatedAtUtc
            };
        }
    }
}
