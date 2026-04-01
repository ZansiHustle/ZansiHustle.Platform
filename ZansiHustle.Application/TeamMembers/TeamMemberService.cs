using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ZansiHustle.Application.TeamMembers.Dtos;
using ZansiHustle.Domain.Identity;
using ZansiHustle.Shared.Enums.User;
using ZansiHustle.Shared.Results;

namespace ZansiHustle.Application.TeamMembers
{
    /// <summary>
    /// Team member management service backed by the ASP.NET Identity users table.
    /// </summary>
    public class TeamMemberService : ITeamMemberService
    {
        private readonly UserManager<User> _userManager;

        public TeamMemberService(UserManager<User> userManager)
        {
            _userManager = userManager;
        }

        public async Task<Result<List<TeamMemberDto>>> GetAllAsync()
        {
            try
            {
                var users = await _userManager.Users
                    .AsNoTracking()
                    .OrderBy(x => x.FirstName)
                    .ThenBy(x => x.LastName)
                    .ToListAsync();

                var data = new List<TeamMemberDto>();

                foreach (var user in users)
                {
                    var roles = await _userManager.GetRolesAsync(user);

                    if (!IsTeamUser(user, roles))
                        continue;

                    data.Add(MapToDto(user, roles));
                }

                return Result<List<TeamMemberDto>>.Success(data, "Team members retrieved successfully.");
            }
            catch (Exception ex)
            {
                return Result<List<TeamMemberDto>>.Failure($"An error occurred while retrieving team members. {ex.Message}");
            }
        }

        public async Task<Result<TeamMemberDto>> GetByIdAsync(string id)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(id))
                    return Result<TeamMemberDto>.Failure("Team member id is required.");

                var user = await _userManager.Users.FirstOrDefaultAsync(x => x.Id.ToString() == id);
                if (user is null)
                    return Result<TeamMemberDto>.Failure("Team member not found.");

                var roles = await _userManager.GetRolesAsync(user);

                if (!IsTeamUser(user, roles))
                    return Result<TeamMemberDto>.Failure("Team member not found.");

                return Result<TeamMemberDto>.Success(MapToDto(user, roles), "Team member retrieved successfully.");
            }
            catch (Exception ex)
            {
                return Result<TeamMemberDto>.Failure($"An error occurred while retrieving the team member. {ex.Message}");
            }
        }

        public async Task<Result<TeamMemberDto>> CreateAsync(CreateTeamMemberRequestDto request)
        {
            try
            {
                if (request is null)
                    return Result<TeamMemberDto>.Failure("Request is required.");

                if (string.IsNullOrWhiteSpace(request.Email))
                    return Result<TeamMemberDto>.Failure("Email is required.");

                if (string.IsNullOrWhiteSpace(request.Password))
                    return Result<TeamMemberDto>.Failure("Password is required.");

                var existing = await _userManager.FindByEmailAsync(request.Email.Trim());
                if (existing is not null)
                    return Result<TeamMemberDto>.Failure("A user with this email already exists.");

                var (firstName, lastName) = SplitName(request.FullName);

                var user = new User
                {
                    UserName = request.Email.Trim(),
                    Email = request.Email.Trim(),
                    FirstName = firstName,
                    LastName = lastName,
                    PhoneNumber = request.PhoneNumber?.Trim(),
                    EmailConfirmed = true,
                    IsActive = true,
                    CreatedOnUtc = DateTime.UtcNow,
                    UpdatedOnUtc = DateTime.UtcNow
                };

                var createResult = await _userManager.CreateAsync(user, request.Password);
                if (!createResult.Succeeded)
                {
                    var message = string.Join(" | ", createResult.Errors.Select(x => x.Description));
                    return Result<TeamMemberDto>.Failure(message);
                }

                var roles = NormaliseRoles(request.Roles);
                if (!roles.Any())
                    roles.Add("TeamMember");

                var roleResult = await _userManager.AddToRolesAsync(user, roles);
                if (!roleResult.Succeeded)
                {
                    var message = string.Join(" | ", roleResult.Errors.Select(x => x.Description));
                    return Result<TeamMemberDto>.Failure(message);
                }

                var savedRoles = await _userManager.GetRolesAsync(user);

                return Result<TeamMemberDto>.Success(MapToDto(user, savedRoles), "Team member created successfully.");
            }
            catch (Exception ex)
            {
                return Result<TeamMemberDto>.Failure($"An error occurred while creating the team member. {ex.Message}");
            }
        }

        public async Task<Result<TeamMemberDto>> UpdateAsync(string id, UpdateTeamMemberRequestDto request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(id))
                    return Result<TeamMemberDto>.Failure("Team member id is required.");

                if (request is null)
                    return Result<TeamMemberDto>.Failure("Request is required.");

                var user = await _userManager.Users.FirstOrDefaultAsync(x => x.Id.ToString() == id);
                if (user is null)
                    return Result<TeamMemberDto>.Failure("Team member not found.");

                var (firstName, lastName) = SplitName(request.FullName);

                user.FirstName = firstName;
                user.LastName = lastName;
                user.PhoneNumber = request.PhoneNumber?.Trim();
                user.IsActive = string.Equals(request.Status, "active", StringComparison.OrdinalIgnoreCase);
                user.UpdatedOnUtc = DateTime.UtcNow;

                var updateResult = await _userManager.UpdateAsync(user);
                if (!updateResult.Succeeded)
                {
                    var message = string.Join(" | ", updateResult.Errors.Select(x => x.Description));
                    return Result<TeamMemberDto>.Failure(message);
                }

                var existingRoles = await _userManager.GetRolesAsync(user);
                if (existingRoles.Any())
                {
                    var removeRolesResult = await _userManager.RemoveFromRolesAsync(user, existingRoles);
                    if (!removeRolesResult.Succeeded)
                    {
                        var message = string.Join(" | ", removeRolesResult.Errors.Select(x => x.Description));
                        return Result<TeamMemberDto>.Failure(message);
                    }
                }

                var newRoles = NormaliseRoles(request.Roles);
                if (!newRoles.Any())
                    newRoles.Add("TeamMember");

                var addRolesResult = await _userManager.AddToRolesAsync(user, newRoles);
                if (!addRolesResult.Succeeded)
                {
                    var message = string.Join(" | ", addRolesResult.Errors.Select(x => x.Description));
                    return Result<TeamMemberDto>.Failure(message);
                }

                var savedRoles = await _userManager.GetRolesAsync(user);

                return Result<TeamMemberDto>.Success(MapToDto(user, savedRoles), "Team member updated successfully.");
            }
            catch (Exception ex)
            {
                return Result<TeamMemberDto>.Failure($"An error occurred while updating the team member. {ex.Message}");
            }
        }

        public async Task<Result> DeleteAsync(string id)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(id))
                    return Result.Failure("Team member id is required.");

                var user = await _userManager.Users.FirstOrDefaultAsync(x => x.Id.ToString() == id);
                if (user is null)
                    return Result.Failure("Team member not found.");

                var result = await _userManager.DeleteAsync(user);
                if (!result.Succeeded)
                {
                    var message = string.Join(" | ", result.Errors.Select(x => x.Description));
                    return Result.Failure(message);
                }

                return Result.Success("Team member deleted successfully.");
            }
            catch (Exception ex)
            {
                return Result.Failure($"An error occurred while deleting the team member. {ex.Message}");
            }
        }

        private static TeamMemberDto MapToDto(User user, IList<string> roles)
        {
            return new TeamMemberDto
            {
                Id = user.Id.ToString(),
                Email = user.Email ?? string.Empty,
                FullName = BuildFullName(user.FirstName, user.LastName),
                PhoneNumber = user.PhoneNumber,
                Roles = roles.ToList(),
                TeamPortalEnabled = roles.Contains(UserRole.TeamMember.ToString()),
                AdminPortalEnabled = roles.Contains(UserRole.Admin.ToString()),
                FinanceAccess = roles.Contains(UserRole.TeamMember.ToString()) || roles.Contains(UserRole.Accountant.ToString()),
                Status = user.IsActive ? "active" : "inactive",
                Department = "General",
                JoinedDateUtc = user.CreatedOnUtc,
                Notes = ""
            };
        }

        private static bool IsTeamUser(User user, IEnumerable<string> roles)
        {
            return roles.Any(r =>
                r.Equals("TeamMember", StringComparison.OrdinalIgnoreCase) ||
                r.Equals("Agent", StringComparison.OrdinalIgnoreCase) ||
                r.Equals("Admin", StringComparison.OrdinalIgnoreCase) ||
                r.Equals("TeamManager", StringComparison.OrdinalIgnoreCase) ||
                r.Equals("MarketplaceGrowthAssociate", StringComparison.OrdinalIgnoreCase) ||
                r.Equals("SocialMediaManager", StringComparison.OrdinalIgnoreCase) ||
                r.Equals("ContentCreator", StringComparison.OrdinalIgnoreCase));
        }

        private static List<string> NormaliseRoles(IEnumerable<string>? roles)
        {
            return (roles ?? Enumerable.Empty<string>())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private static (string firstName, string lastName) SplitName(string? fullName)
        {
            var clean = (fullName ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(clean))
                return (string.Empty, string.Empty);

            var parts = clean.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 1)
                return (parts[0], string.Empty);

            return (parts[0], string.Join(" ", parts.Skip(1)));
        }

        private static string BuildFullName(string? firstName, string? lastName)
        {
            return ((firstName ?? string.Empty) + " " + (lastName ?? string.Empty)).Trim();
        }
    }
}
