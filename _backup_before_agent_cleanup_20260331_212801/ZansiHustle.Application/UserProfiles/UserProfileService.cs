using Microsoft.Extensions.Logging;
using ZansiHustle.Application.Persistence.Users;
using ZansiHustle.Domain.Identity;
using ZansiHustle.Shared.Errors;
using ZansiHustle.Shared.Results;

namespace ZansiHustle.Application.Users;

/// <summary>
/// Handles user profile operations.
/// </summary>
public class UserProfileService : IUserProfileService
{
    private readonly IUserRepository _userRepository;
    private readonly IUserProfileRepository _userProfileRepository;
    private readonly ILogger<UserProfileService> _logger;

    public UserProfileService(
        IUserRepository userRepository,
        IUserProfileRepository userProfileRepository,
        ILogger<UserProfileService> logger)
    {
        _userRepository = userRepository;
        _userProfileRepository = userProfileRepository;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<Result<UserProfile>> GetByUserIdAsync(Guid userId)
    {
        try
        {
            var profile = await _userProfileRepository.GetByUserIdAsync(userId);
            if (profile == null)
                return Result<UserProfile>.Failure(ErrorCodes.NotFound, "User profile not found.");

            return Result<UserProfile>.Success(profile);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load profile for user {UserId}.", userId);
            return Result<UserProfile>.Failure(ErrorCodes.Exception, "Failed to load user profile.");
        }
    }

    /// <inheritdoc />
    public async Task<Result<UserProfile>> UpsertAsync(Guid userId, string? profileImageUrl, string? bio, string? city, string? province)
    {
        try
        {
            var userExists = await _userRepository.ExistsAsync(userId);
            if (!userExists)
                return Result<UserProfile>.Failure(ErrorCodes.NotFound, "User not found.");

            var profile = await _userProfileRepository.GetByUserIdAsync(userId);

            if (profile == null)
            {
                profile = new UserProfile
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    ProfileImageUrl = string.IsNullOrWhiteSpace(profileImageUrl) ? null : profileImageUrl.Trim(),
                    Bio = string.IsNullOrWhiteSpace(bio) ? null : bio.Trim(),
                    City = string.IsNullOrWhiteSpace(city) ? null : city.Trim(),
                    Province = string.IsNullOrWhiteSpace(province) ? null : province.Trim(),
                    CreatedOnUtc = DateTime.UtcNow
                };

                await _userProfileRepository.AddAsync(profile);
            }
            else
            {
                profile.ProfileImageUrl = string.IsNullOrWhiteSpace(profileImageUrl) ? null : profileImageUrl.Trim();
                profile.Bio = string.IsNullOrWhiteSpace(bio) ? null : bio.Trim();
                profile.City = string.IsNullOrWhiteSpace(city) ? null : city.Trim();
                profile.Province = string.IsNullOrWhiteSpace(province) ? null : province.Trim();
                profile.UpdatedOnUtc = DateTime.UtcNow;

                await _userProfileRepository.UpdateAsync(profile);
            }

            return Result<UserProfile>.Success(profile, "User profile saved successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save profile for user {UserId}.", userId);
            return Result<UserProfile>.Failure(ErrorCodes.Exception, "Failed to save user profile.");
        }
    }
}