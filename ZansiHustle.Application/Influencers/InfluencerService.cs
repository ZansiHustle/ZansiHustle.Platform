using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ZansiHustle.Application.Influencers.Dtos;
using ZansiHustle.Application.Persistence.Influencers;
using ZansiHustle.Domain.Influencers;
using ZansiHustle.Shared.Results;

namespace ZansiHustle.Application.Influencers
{
    /// <summary>
    /// Provides business logic for influencer operations.
    /// </summary>
    public class InfluencerService : IInfluencerService
    {
        private readonly IInfluencerRepository _influencerRepository;

        /// <summary>
        /// Creates a new instance of the <see cref="InfluencerService"/> class.
        /// </summary>
        public InfluencerService(IInfluencerRepository influencerRepository)
        {
            _influencerRepository = influencerRepository;
        }

        /// <inheritdoc />
        public async Task<Result<List<InfluencerListItemDto>>> GetAllAsync()
        {
            try
            {
                var influencers = await _influencerRepository.GetAllAsync();
                var data = influencers.Select(MapToListItemDto).ToList();

                return Result<List<InfluencerListItemDto>>.Success(data, "Influencers retrieved successfully.");
            }
            catch (Exception ex)
            {
                return Result<List<InfluencerListItemDto>>.Failure($"An error occurred while retrieving influencers. {ex.Message}");
            }
        }

        /// <inheritdoc />
        public async Task<Result<InfluencerDetailsDto>> GetByIdAsync(Guid id)
        {
            try
            {
                var influencer = await _influencerRepository.GetByIdAsync(id);

                if (influencer is null)
                {
                    return Result<InfluencerDetailsDto>.Failure("Influencer not found.");
                }

                return Result<InfluencerDetailsDto>.Success(MapToDetailsDto(influencer), "Influencer retrieved successfully.");
            }
            catch (Exception ex)
            {
                return Result<InfluencerDetailsDto>.Failure($"An error occurred while retrieving the influencer. {ex.Message}");
            }
        }

        /// <inheritdoc />
        public async Task<Result<InfluencerDetailsDto>> CreateAsync(CreateInfluencerRequestDto request)
        {
            try
            {
                if (request is null)
                {
                    return Result<InfluencerDetailsDto>.Failure("Request is required.");
                }

                if (string.IsNullOrWhiteSpace(request.FullName))
                {
                    return Result<InfluencerDetailsDto>.Failure("Full name is required.");
                }

                var entity = new Influencer
                {
                    Id = Guid.NewGuid(),
                    Code = $"INF-{DateTime.UtcNow:yyyyMMddHHmmssfff}",
                    FullName = request.FullName.Trim(),
                    Niche = request.Niche?.Trim(),
                    Province = request.Province?.Trim(),
                    City = request.City?.Trim(),
                    Rate = request.Rate,
                    Email = request.Email?.Trim(),
                    PhoneNumber = request.PhoneNumber?.Trim(),
                    Notes = request.Notes?.Trim(),
                    AddedByUserId = request.AddedByUserId,
                    CreatedAtUtc = DateTime.UtcNow
                };

                foreach (var account in request.PlatformAccounts)
                {
                    entity.PlatformAccounts.Add(new InfluencerPlatformAccount
                    {
                        Id = Guid.NewGuid(),
                        Platform = account.Platform,
                        Handle = account.Handle?.Trim(),
                        Url = account.Url?.Trim(),
                        FollowersCount = account.FollowersCount,
                        CreatedAtUtc = DateTime.UtcNow
                    });
                }

                await _influencerRepository.AddAsync(entity);

                var saved = await _influencerRepository.SaveChangesAsync();

                if (!saved)
                {
                    return Result<InfluencerDetailsDto>.Failure("Failed to create influencer.");
                }

                var createdEntity = await _influencerRepository.GetByIdAsync(entity.Id) ?? entity;

                return Result<InfluencerDetailsDto>.Success(MapToDetailsDto(createdEntity), "Influencer created successfully.");
            }
            catch (Exception ex)
            {
                return Result<InfluencerDetailsDto>.Failure($"An error occurred while creating the influencer. {ex.Message}");
            }
        }

        /// <inheritdoc />
        public async Task<Result<InfluencerDetailsDto>> UpdateAsync(Guid id, UpdateInfluencerRequestDto request)
        {
            try
            {
                if (request is null)
                {
                    return Result<InfluencerDetailsDto>.Failure("Request is required.");
                }

                if (string.IsNullOrWhiteSpace(request.FullName))
                {
                    return Result<InfluencerDetailsDto>.Failure("Full name is required.");
                }

                var influencer = await _influencerRepository.GetByIdAsync(id);

                if (influencer is null)
                {
                    return Result<InfluencerDetailsDto>.Failure("Influencer not found.");
                }

                influencer.FullName = request.FullName.Trim();
                influencer.Niche = request.Niche?.Trim();
                influencer.Province = request.Province?.Trim();
                influencer.City = request.City?.Trim();
                influencer.Rate = request.Rate;
                influencer.Email = request.Email?.Trim();
                influencer.PhoneNumber = request.PhoneNumber?.Trim();
                influencer.Notes = request.Notes?.Trim();
                influencer.UpdatedAtUtc = DateTime.UtcNow;

                influencer.PlatformAccounts.Clear();

                foreach (var account in request.PlatformAccounts)
                {
                    influencer.PlatformAccounts.Add(new InfluencerPlatformAccount
                    {
                        Id = Guid.NewGuid(),
                        InfluencerId = influencer.Id,
                        Platform = account.Platform,
                        Handle = account.Handle?.Trim(),
                        Url = account.Url?.Trim(),
                        FollowersCount = account.FollowersCount,
                        CreatedAtUtc = DateTime.UtcNow
                    });
                }

                _influencerRepository.Update(influencer);

                var saved = await _influencerRepository.SaveChangesAsync();

                if (!saved)
                {
                    return Result<InfluencerDetailsDto>.Failure("Failed to update influencer.");
                }

                var updatedEntity = await _influencerRepository.GetByIdAsync(influencer.Id) ?? influencer;

                return Result<InfluencerDetailsDto>.Success(MapToDetailsDto(updatedEntity), "Influencer updated successfully.");
            }
            catch (Exception ex)
            {
                return Result<InfluencerDetailsDto>.Failure($"An error occurred while updating the influencer. {ex.Message}");
            }
        }

        /// <inheritdoc />
        public async Task<Result<InfluencerDetailsDto>> UpdateStatusAsync(Guid id, UpdateInfluencerStatusRequestDto request)
        {
            try
            {
                if (request is null)
                {
                    return Result<InfluencerDetailsDto>.Failure("Request is required.");
                }

                var influencer = await _influencerRepository.GetByIdAsync(id);

                if (influencer is null)
                {
                    return Result<InfluencerDetailsDto>.Failure("Influencer not found.");
                }

                influencer.Status = request.Status;
                influencer.UpdatedAtUtc = DateTime.UtcNow;

                _influencerRepository.Update(influencer);

                var saved = await _influencerRepository.SaveChangesAsync();

                if (!saved)
                {
                    return Result<InfluencerDetailsDto>.Failure("Failed to update influencer status.");
                }

                return Result<InfluencerDetailsDto>.Success(MapToDetailsDto(influencer), "Influencer status updated successfully.");
            }
            catch (Exception ex)
            {
                return Result<InfluencerDetailsDto>.Failure($"An error occurred while updating influencer status. {ex.Message}");
            }
        }

        /// <inheritdoc />
        public async Task<Result> DeleteAsync(Guid id)
        {
            try
            {
                var influencer = await _influencerRepository.GetByIdAsync(id);

                if (influencer is null)
                {
                    return Result.Failure("Influencer not found.");
                }

                _influencerRepository.Delete(influencer);

                var saved = await _influencerRepository.SaveChangesAsync();

                if (!saved)
                {
                    return Result.Failure("Failed to delete influencer.");
                }

                return Result.Success("Influencer deleted successfully.");
            }
            catch (Exception ex)
            {
                return Result.Failure($"An error occurred while deleting the influencer. {ex.Message}");
            }
        }

        private static InfluencerListItemDto MapToListItemDto(Influencer influencer)
        {
            var accounts = influencer.PlatformAccounts.Select(MapPlatformAccountDto).ToList();

            return new InfluencerListItemDto
            {
                Id = influencer.Id,
                Code = influencer.Code,
                FullName = influencer.FullName,
                Niche = influencer.Niche,
                Province = influencer.Province,
                City = influencer.City,
                Rate = influencer.Rate,
                Email = influencer.Email,
                PhoneNumber = influencer.PhoneNumber,
                Status = influencer.Status,
                TotalFollowers = influencer.PlatformAccounts.Sum(x => x.FollowersCount),
                PlatformAccounts = accounts,
                CreatedAtUtc = influencer.CreatedAtUtc
            };
        }

        private static InfluencerDetailsDto MapToDetailsDto(Influencer influencer)
        {
            return new InfluencerDetailsDto
            {
                Id = influencer.Id,
                Code = influencer.Code,
                FullName = influencer.FullName,
                Niche = influencer.Niche,
                Province = influencer.Province,
                City = influencer.City,
                Rate = influencer.Rate,
                Email = influencer.Email,
                PhoneNumber = influencer.PhoneNumber,
                Notes = influencer.Notes,
                AddedByUserId = influencer.AddedByUserId,
                Status = influencer.Status,
                PlatformAccounts = influencer.PlatformAccounts.Select(MapPlatformAccountDto).ToList(),
                CreatedAtUtc = influencer.CreatedAtUtc,
                UpdatedAtUtc = influencer.UpdatedAtUtc
            };
        }

        private static InfluencerPlatformAccountDto MapPlatformAccountDto(InfluencerPlatformAccount account)
        {
            return new InfluencerPlatformAccountDto
            {
                Platform = account.Platform,
                Handle = account.Handle,
                Url = account.Url,
                FollowersCount = account.FollowersCount
            };
        }
    }
}
