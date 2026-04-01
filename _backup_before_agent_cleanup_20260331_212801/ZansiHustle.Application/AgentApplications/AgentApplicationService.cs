using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ZansiHustle.Application.AgentApplications.Dtos;
using ZansiHustle.Application.Persistence.AgentApplications;
using ZansiHustle.Domain.AgentApplications;
using ZansiHustle.Shared.Enums.AgentApplications;
using ZansiHustle.Shared.Results;

namespace ZansiHustle.Application.AgentApplications
{
    /// <summary>
    /// Provides business logic for agent application operations.
    /// </summary>
    public class AgentApplicationService : IAgentApplicationService
    {
        private readonly IAgentApplicationRepository _agentApplicationRepository;

        /// <summary>
        /// Creates a new instance of the <see cref="AgentApplicationService"/> class.
        /// </summary>
        public AgentApplicationService(IAgentApplicationRepository agentApplicationRepository)
        {
            _agentApplicationRepository = agentApplicationRepository;
        }

        /// <inheritdoc />
        public async Task<Result<List<AgentApplicationListItemDto>>> GetAllAsync()
        {
            try
            {
                var applications = await _agentApplicationRepository.GetAllAsync();

                var data = applications.Select(MapToListItemDto).ToList();

                return Result<List<AgentApplicationListItemDto>>.Success(data, "Agent applications retrieved successfully.");
            }
            catch (Exception ex)
            {
                return Result<List<AgentApplicationListItemDto>>.Failure($"An error occurred while retrieving agent applications. {ex.Message}");
            }
        }

        /// <inheritdoc />
        public async Task<Result<AgentApplicationDetailsDto>> GetByIdAsync(Guid id)
        {
            try
            {
                var application = await _agentApplicationRepository.GetByIdAsync(id);

                if (application is null)
                {
                    return Result<AgentApplicationDetailsDto>.Failure("Agent application not found.");
                }

                return Result<AgentApplicationDetailsDto>.Success(MapToDetailsDto(application), "Agent application retrieved successfully.");
            }
            catch (Exception ex)
            {
                return Result<AgentApplicationDetailsDto>.Failure($"An error occurred while retrieving the agent application. {ex.Message}");
            }
        }

        /// <inheritdoc />
        public async Task<Result<AgentApplicationDetailsDto>> CreateAsync(CreateAgentApplicationRequestDto request)
        {
            try
            {
                if (request is null)
                {
                    return Result<AgentApplicationDetailsDto>.Failure("Request is required.");
                }

                if (string.IsNullOrWhiteSpace(request.FullName))
                {
                    return Result<AgentApplicationDetailsDto>.Failure("Full name is required.");
                }

                var entity = new AgentApplication
                {
                    Id = Guid.NewGuid(),
                    Code = $"AAP-{DateTime.UtcNow:yyyyMMddHHmmssfff}",
                    FullName = request.FullName.Trim(),
                    PhoneNumber = request.PhoneNumber?.Trim(),
                    Email = request.Email?.Trim(),
                    Province = request.Province?.Trim(),
                    City = request.City?.Trim(),
                    SocialHandle = request.SocialHandle?.Trim(),
                    Notes = request.Notes?.Trim(),
                    Status = AgentApplicationStatus.Pending,
                    SubmittedAtUtc = DateTime.UtcNow,
                    CreatedAtUtc = DateTime.UtcNow
                };

                await _agentApplicationRepository.AddAsync(entity);

                var saved = await _agentApplicationRepository.SaveChangesAsync();

                if (!saved)
                {
                    return Result<AgentApplicationDetailsDto>.Failure("Failed to create agent application.");
                }

                return Result<AgentApplicationDetailsDto>.Success(MapToDetailsDto(entity), "Agent application created successfully.");
            }
            catch (Exception ex)
            {
                return Result<AgentApplicationDetailsDto>.Failure($"An error occurred while creating the agent application. {ex.Message}");
            }
        }

        /// <inheritdoc />
        public async Task<Result<AgentApplicationDetailsDto>> ReviewAsync(Guid id, ReviewAgentApplicationRequestDto request)
        {
            try
            {
                if (request is null)
                {
                    return Result<AgentApplicationDetailsDto>.Failure("Request is required.");
                }

                var application = await _agentApplicationRepository.GetByIdAsync(id);

                if (application is null)
                {
                    return Result<AgentApplicationDetailsDto>.Failure("Agent application not found.");
                }

                application.Status = request.Status;
                application.Notes = string.IsNullOrWhiteSpace(request.Notes) ? application.Notes : request.Notes.Trim();
                application.ReviewedByUserId = request.ReviewedByUserId;
                application.ReviewedAtUtc = DateTime.UtcNow;
                application.UpdatedAtUtc = DateTime.UtcNow;

                _agentApplicationRepository.Update(application);

                var saved = await _agentApplicationRepository.SaveChangesAsync();

                if (!saved)
                {
                    return Result<AgentApplicationDetailsDto>.Failure("Failed to review agent application.");
                }

                return Result<AgentApplicationDetailsDto>.Success(MapToDetailsDto(application), "Agent application reviewed successfully.");
            }
            catch (Exception ex)
            {
                return Result<AgentApplicationDetailsDto>.Failure($"An error occurred while reviewing the agent application. {ex.Message}");
            }
        }

        /// <inheritdoc />
        public async Task<Result> DeleteAsync(Guid id)
        {
            try
            {
                var application = await _agentApplicationRepository.GetByIdAsync(id);

                if (application is null)
                {
                    return Result.Failure("Agent application not found.");
                }

                _agentApplicationRepository.Delete(application);

                var saved = await _agentApplicationRepository.SaveChangesAsync();

                if (!saved)
                {
                    return Result.Failure("Failed to delete agent application.");
                }

                return Result.Success("Agent application deleted successfully.");
            }
            catch (Exception ex)
            {
                return Result.Failure($"An error occurred while deleting the agent application. {ex.Message}");
            }
        }

        private static AgentApplicationListItemDto MapToListItemDto(AgentApplication application)
        {
            return new AgentApplicationListItemDto
            {
                Id = application.Id,
                Code = application.Code,
                FullName = application.FullName,
                PhoneNumber = application.PhoneNumber,
                Email = application.Email,
                Province = application.Province,
                City = application.City,
                Status = application.Status,
                SubmittedAtUtc = application.SubmittedAtUtc
            };
        }

        private static AgentApplicationDetailsDto MapToDetailsDto(AgentApplication application)
        {
            return new AgentApplicationDetailsDto
            {
                Id = application.Id,
                Code = application.Code,
                FullName = application.FullName,
                PhoneNumber = application.PhoneNumber,
                Email = application.Email,
                Province = application.Province,
                City = application.City,
                SocialHandle = application.SocialHandle,
                Notes = application.Notes,
                Status = application.Status,
                SubmittedAtUtc = application.SubmittedAtUtc,
                ReviewedAtUtc = application.ReviewedAtUtc,
                ReviewedByUserId = application.ReviewedByUserId,
                CreatedAtUtc = application.CreatedAtUtc,
                UpdatedAtUtc = application.UpdatedAtUtc
            };
        }
    }
}
