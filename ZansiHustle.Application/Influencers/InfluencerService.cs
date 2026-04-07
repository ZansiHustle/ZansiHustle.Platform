using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ZansiHustle.Application.Common.Interfaces.Shared;
using ZansiHustle.Application.Influencers.Dtos;
using ZansiHustle.Application.Persistence.Influencers;
using ZansiHustle.Application.SellerLeads.Dtos;
using ZansiHustle.Application.Users;
using ZansiHustle.Domain.Identity;
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
        private readonly IUserService _userServ;
        private readonly ICurrentUserService _currentUserService;

        /// <summary>
        /// Creates a new instance of the <see cref="InfluencerService"/> class.
        /// </summary>
        public InfluencerService(IInfluencerRepository influencerRepository, IUserService userServ, ICurrentUserService currentUserService)
        {
            _influencerRepository = influencerRepository;
            _userServ = userServ;
            _currentUserService = currentUserService;
        }

        /// <inheritdoc />
        public async Task<Result<List<InfluencerListItemDto>>> GetAllAsync()
        {
            try
            {
                var influencers = await _influencerRepository.GetAllAsync();

                // Get all unique user IDs from influencers
                var userIds = influencers
                    .Where(x => x.AddedByUserId.HasValue)
                    .Select(x => x.AddedByUserId.Value)
                    .Distinct()
                    .ToList();

                // Fetch all users in one batch
                var users = await _userServ.GetByIDsAsync(userIds);
                Dictionary<Guid, User> userDict = new Dictionary<Guid, User>();

                if (userIds.Any())
                {
                    var usersResult = await _userServ.GetByIDsAsync(userIds);
                    if (usersResult.IsSuccess && usersResult.Data != null)
                    {
                        userDict = usersResult.Data.ToDictionary(u => u.Id, u => u);
                    }
                }

                var data = influencers.Select(influencer => MapToListItemDto(influencer, userDict)).ToList();

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

                if (!_currentUserService.UserId.HasValue)
                {
                    return Result<InfluencerDetailsDto>.Failure("Authenticated user was not found.");
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
                    AddedByUserId = _currentUserService.UserId,
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

                // Get the existing entity WITH tracking
                var influencer = await _influencerRepository.GetByIdForUpdateAsync(id);

                if (influencer is null)
                {
                    return Result<InfluencerDetailsDto>.Failure("Influencer not found.");
                }

                // Update simple properties
                influencer.FullName = request.FullName.Trim();
                influencer.Niche = request.Niche?.Trim();
                influencer.Province = request.Province?.Trim();
                influencer.City = request.City?.Trim();
                influencer.Rate = request.Rate;
                influencer.Email = request.Email?.Trim();
                influencer.PhoneNumber = request.PhoneNumber?.Trim();
                influencer.Notes = request.Notes?.Trim();
                influencer.UpdatedAtUtc = DateTime.UtcNow;

                // Update platform accounts - UPDATE existing, don't clear and recreate
                var requestAccounts = request.PlatformAccounts.ToDictionary(a => a.Platform);

                // Remove accounts not in request
                var accountsToRemove = influencer.PlatformAccounts
                    .Where(a => !requestAccounts.ContainsKey(a.Platform))
                    .ToList();

                foreach (var account in accountsToRemove)
                {
                    _influencerRepository.RemovePlatformAccount(account);
                }

                // Update or add accounts
                foreach (var requestAccount in request.PlatformAccounts)
                {
                    var existingAccount = influencer.PlatformAccounts
                        .FirstOrDefault(a => a.Platform == requestAccount.Platform);

                    if (existingAccount != null)
                    {
                        // Update existing
                        existingAccount.Handle = requestAccount.Handle?.Trim();
                        existingAccount.Url = requestAccount.Url?.Trim();
                        existingAccount.FollowersCount = requestAccount.FollowersCount;
                    }
                    else
                    {
                        // Add new
                        influencer.PlatformAccounts.Add(new InfluencerPlatformAccount
                        {
                            Id = Guid.NewGuid(),
                            InfluencerId = influencer.Id,
                            Platform = requestAccount.Platform,
                            Handle = requestAccount.Handle?.Trim(),
                            Url = requestAccount.Url?.Trim(),
                            FollowersCount = requestAccount.FollowersCount,
                            CreatedAtUtc = DateTime.UtcNow
                        });
                    }
                }

                // Just call SaveChangesAsync - EF tracks everything
                var saved = await _influencerRepository.SaveChangesAsync();

                if (!saved)
                {
                    return Result<InfluencerDetailsDto>.Failure("Failed to update influencer.");
                }

                var updatedEntity = await _influencerRepository.GetByIdAsync(influencer.Id) ?? influencer;
                return Result<InfluencerDetailsDto>.Success(MapToDetailsDto(updatedEntity), "Influencer updated successfully.");
            }
            catch (DbUpdateConcurrencyException ex)
            {
                return Result<InfluencerDetailsDto>.Failure("The influencer was modified by another user. Please refresh and try again.");
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

        private static InfluencerListItemDto MapToListItemDto(Influencer influencer, Dictionary<Guid, User> userDict)
        {
            var accounts = influencer.PlatformAccounts.Select(MapPlatformAccountDto).ToList();

            var dto = new InfluencerListItemDto
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
                AddedByUserId = influencer.AddedByUserId,
                CreatedAtUtc = influencer.CreatedAtUtc
            };

            if (influencer.AddedByUserId.HasValue && userDict.TryGetValue(influencer.AddedByUserId.Value, out var user))
            {
                dto.AddedByUserFullname = $"{user.FirstName} {user.LastName}".Trim();
            }
            return dto;
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
