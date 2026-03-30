using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ZansiHustle.Application.Agents.Dtos;
using ZansiHustle.Application.Persistence.Agents;
using ZansiHustle.Domain.Agents;
using ZansiHustle.Shared.Results;

namespace ZansiHustle.Application.Agents
{
    /// <summary>
    /// Provides business logic for agent operations.
    /// </summary>
    public class AgentService : IAgentService
    {
        private readonly IAgentRepository _agentRepository;

        /// <summary>
        /// Creates a new instance of the <see cref="AgentService"/> class.
        /// </summary>
        public AgentService(IAgentRepository agentRepository)
        {
            _agentRepository = agentRepository;
        }

        /// <inheritdoc />
        public async Task<Result<List<AgentListItemDto>>> GetAllAsync()
        {
            try
            {
                var agents = await _agentRepository.GetAllAsync();

                var data = agents.Select(MapToListItemDto).ToList();

                return Result<List<AgentListItemDto>>.Success(data, "Agents retrieved successfully.");
            }
            catch (Exception ex)
            {
                return Result<List<AgentListItemDto>>.Failure($"An error occurred while retrieving agents. {ex.Message}");
            }
        }

        /// <inheritdoc />
        public async Task<Result<AgentDetailsDto>> GetByIdAsync(Guid id)
        {
            try
            {
                var agent = await _agentRepository.GetByIdAsync(id);

                if (agent is null)
                {
                    return Result<AgentDetailsDto>.Failure("Agent not found.");
                }

                return Result<AgentDetailsDto>.Success(MapToDetailsDto(agent), "Agent retrieved successfully.");
            }
            catch (Exception ex)
            {
                return Result<AgentDetailsDto>.Failure($"An error occurred while retrieving the agent. {ex.Message}");
            }
        }

        /// <inheritdoc />
        public async Task<Result<AgentDetailsDto>> CreateAsync(CreateAgentRequestDto request)
        {
            try
            {
                if (request is null)
                {
                    return Result<AgentDetailsDto>.Failure("Request is required.");
                }

                if (string.IsNullOrWhiteSpace(request.FullName))
                {
                    return Result<AgentDetailsDto>.Failure("Full name is required.");
                }

                var entity = new Agent
                {
                    Id = Guid.NewGuid(),
                    Code = $"AGT-{DateTime.UtcNow:yyyyMMddHHmmssfff}",
                    FullName = request.FullName.Trim(),
                    PhoneNumber = request.PhoneNumber?.Trim(),
                    Email = request.Email?.Trim(),
                    Province = request.Province?.Trim(),
                    City = request.City?.Trim(),
                    SocialHandle = request.SocialHandle?.Trim(),
                    Notes = request.Notes?.Trim(),
                    CreatedAtUtc = DateTime.UtcNow,
                    JoinedDateUtc = DateTime.UtcNow
                };

                await _agentRepository.AddAsync(entity);

                var saved = await _agentRepository.SaveChangesAsync();

                if (!saved)
                {
                    return Result<AgentDetailsDto>.Failure("Failed to create agent.");
                }

                return Result<AgentDetailsDto>.Success(MapToDetailsDto(entity), "Agent created successfully.");
            }
            catch (Exception ex)
            {
                return Result<AgentDetailsDto>.Failure($"An error occurred while creating the agent. {ex.Message}");
            }
        }

        /// <inheritdoc />
        public async Task<Result<AgentDetailsDto>> UpdateAsync(Guid id, UpdateAgentRequestDto request)
        {
            try
            {
                if (request is null)
                {
                    return Result<AgentDetailsDto>.Failure("Request is required.");
                }

                if (string.IsNullOrWhiteSpace(request.FullName))
                {
                    return Result<AgentDetailsDto>.Failure("Full name is required.");
                }

                var agent = await _agentRepository.GetByIdAsync(id);

                if (agent is null)
                {
                    return Result<AgentDetailsDto>.Failure("Agent not found.");
                }

                agent.FullName = request.FullName.Trim();
                agent.PhoneNumber = request.PhoneNumber?.Trim();
                agent.Email = request.Email?.Trim();
                agent.Province = request.Province?.Trim();
                agent.City = request.City?.Trim();
                agent.SocialHandle = request.SocialHandle?.Trim();
                agent.Notes = request.Notes?.Trim();
                agent.UpdatedAtUtc = DateTime.UtcNow;

                _agentRepository.Update(agent);

                var saved = await _agentRepository.SaveChangesAsync();

                if (!saved)
                {
                    return Result<AgentDetailsDto>.Failure("Failed to update agent.");
                }

                return Result<AgentDetailsDto>.Success(MapToDetailsDto(agent), "Agent updated successfully.");
            }
            catch (Exception ex)
            {
                return Result<AgentDetailsDto>.Failure($"An error occurred while updating the agent. {ex.Message}");
            }
        }

        /// <inheritdoc />
        public async Task<Result> DeleteAsync(Guid id)
        {
            try
            {
                var agent = await _agentRepository.GetByIdAsync(id);

                if (agent is null)
                {
                    return Result.Failure("Agent not found.");
                }

                _agentRepository.Delete(agent);

                var saved = await _agentRepository.SaveChangesAsync();

                if (!saved)
                {
                    return Result.Failure("Failed to delete agent.");
                }

                return Result.Success("Agent deleted successfully.");
            }
            catch (Exception ex)
            {
                return Result.Failure($"An error occurred while deleting the agent. {ex.Message}");
            }
        }

        private static AgentListItemDto MapToListItemDto(Agent agent)
        {
            return new AgentListItemDto
            {
                Id = agent.Id,
                Code = agent.Code,
                FullName = agent.FullName,
                PhoneNumber = agent.PhoneNumber,
                Email = agent.Email,
                Province = agent.Province,
                City = agent.City,
                Status = agent.Status,
                JoinedDateUtc = agent.JoinedDateUtc,
                CreatedAtUtc = agent.CreatedAtUtc
            };
        }

        private static AgentDetailsDto MapToDetailsDto(Agent agent)
        {
            return new AgentDetailsDto
            {
                Id = agent.Id,
                Code = agent.Code,
                FullName = agent.FullName,
                PhoneNumber = agent.PhoneNumber,
                Email = agent.Email,
                Province = agent.Province,
                City = agent.City,
                SocialHandle = agent.SocialHandle,
                Notes = agent.Notes,
                Status = agent.Status,
                JoinedDateUtc = agent.JoinedDateUtc,
                CreatedAtUtc = agent.CreatedAtUtc,
                UpdatedAtUtc = agent.UpdatedAtUtc
            };
        }
    }
}
