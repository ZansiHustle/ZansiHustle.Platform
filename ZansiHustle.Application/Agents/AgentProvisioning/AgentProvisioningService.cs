using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ZansiHustle.Application.Agents.AgentProvisioning.Dtos;
using ZansiHustle.Domain.Identity;
using ZansiHustle.Shared.Enums.User;
using ZansiHustle.Shared.Errors;
using ZansiHustle.Shared.Results;

namespace ZansiHustle.Application.Agents.AgentProvisioning
{
    public class AgentProvisioningService : IAgentProvisioningService
    {
        private const string AgentRoleName = nameof(UserRole.Agent);

        private readonly UserManager<User> _userManager;

        public AgentProvisioningService(UserManager<User> userManager)
        {
            _userManager = userManager;
        }

        // ── Read ───────────────────────────────────────────────────

        public async Task<Result<List<AgentDetailsDto>>> GetAllAsync()
        {
            try
            {
                var agents = await _userManager.GetUsersInRoleAsync(AgentRoleName);
                // Ordering here is cheap — fetch count is low and stable
                // enough that sorting in-memory won't blow up.
                var data = agents
                    .OrderBy(u => u.FirstName).ThenBy(u => u.LastName)
                    .Select(ToDto)
                    .ToList();
                return Result<List<AgentDetailsDto>>.Success(data, "Agents retrieved successfully.");
            }
            catch (Exception ex)
            {
                return Result<List<AgentDetailsDto>>.Failure(ErrorCodes.Exception,
                    $"An error occurred while retrieving agents. {ex.Message}");
            }
        }

        public async Task<Result<AgentDetailsDto>> GetByIdAsync(Guid id)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(id.ToString());
                if (user is null)
                    return Result<AgentDetailsDto>.Failure(ErrorCodes.NotFound, "Agent not found.");

                var roles = await _userManager.GetRolesAsync(user);
                if (!roles.Contains(AgentRoleName))
                    return Result<AgentDetailsDto>.Failure(ErrorCodes.NotFound, "Agent not found.");

                return Result<AgentDetailsDto>.Success(ToDto(user), "Agent retrieved.");
            }
            catch (Exception ex)
            {
                return Result<AgentDetailsDto>.Failure(ErrorCodes.Exception,
                    $"An error occurred while retrieving the agent. {ex.Message}");
            }
        }

        // ── Create ─────────────────────────────────────────────────

        public async Task<Result<AgentDetailsDto>> CreateAsync(CreateAgentRequest request)
        {
            try
            {
                if (request is null)
                    return Result<AgentDetailsDto>.Failure(ErrorCodes.BadRequest, "Request is required.");
                if (string.IsNullOrWhiteSpace(request.FullName))
                    return Result<AgentDetailsDto>.Failure(ErrorCodes.BadRequest, "Full name is required.");
                if (string.IsNullOrWhiteSpace(request.Email))
                    return Result<AgentDetailsDto>.Failure(ErrorCodes.BadRequest, "Email is required for login credentials.");

                var email = request.Email.Trim();
                var existing = await _userManager.FindByEmailAsync(email);
                if (existing != null)
                    return Result<AgentDetailsDto>.Failure(ErrorCodes.BadRequest,
                        "An account with this email already exists. Pick another email or reset the existing account.");

                SplitName(request.FullName, out var firstName, out var lastName);

                var user = new User
                {
                    Id = Guid.NewGuid(),
                    FirstName = firstName,
                    LastName = lastName,
                    Email = email,
                    UserName = email,
                    PhoneNumber = string.IsNullOrWhiteSpace(request.PhoneNumber) ? null : request.PhoneNumber.Trim(),
                    IsActive = true,
                    AccountStatus = PortalToAccountStatus(request.Status ?? 1),
                    CreatedOnUtc = DateTime.UtcNow,
                };

                var password = GenerateTempPassword();
                var createResult = await _userManager.CreateAsync(user, password);
                if (!createResult.Succeeded)
                    return Result<AgentDetailsDto>.Failure(ErrorCodes.BadRequest,
                        string.Join(" ", createResult.Errors.Select(e => e.Description)));

                var roleResult = await _userManager.AddToRolesAsync(user, new[] { AgentRoleName });
                if (!roleResult.Succeeded)
                {
                    // Roll back the user so we don't orphan an agent with
                    // no role if role assignment misfires (unusual path).
                    await _userManager.DeleteAsync(user);
                    return Result<AgentDetailsDto>.Failure(ErrorCodes.Exception,
                        $"Failed to assign Agent role: {string.Join(" ", roleResult.Errors.Select(e => e.Description))}");
                }

                // Preserve free-form notes / social handle via Identity
                // claims so they survive without a new schema column. Low
                // volume fields; claims table is the right scratch space.
                await AddOptionalClaimsAsync(user, request.SocialHandle, request.Notes, request.Province, request.City);

                var dto = ToDto(user);
                dto.SocialHandle = request.SocialHandle;
                dto.Notes = request.Notes;
                dto.Province = request.Province;
                dto.City = request.City;
                // Return the plaintext password ONCE, only in this response.
                // Never persisted anywhere in cleartext.
                dto.InitialPassword = password;
                return Result<AgentDetailsDto>.Success(dto, "Agent created. Share the temporary password with the agent — they'll use it to sign in and can change it afterwards.");
            }
            catch (Exception ex)
            {
                return Result<AgentDetailsDto>.Failure(ErrorCodes.Exception,
                    $"An error occurred while creating the agent. {ex.Message}");
            }
        }

        // ── Update ─────────────────────────────────────────────────

        public async Task<Result<AgentDetailsDto>> UpdateStatusAsync(Guid id, UpdateAgentRequest request)
        {
            try
            {
                if (request is null)
                    return Result<AgentDetailsDto>.Failure(ErrorCodes.BadRequest, "Request is required.");

                var user = await _userManager.FindByIdAsync(id.ToString());
                if (user is null)
                    return Result<AgentDetailsDto>.Failure(ErrorCodes.NotFound, "Agent not found.");

                var roles = await _userManager.GetRolesAsync(user);
                if (!roles.Contains(AgentRoleName))
                    return Result<AgentDetailsDto>.Failure(ErrorCodes.NotFound, "Agent not found.");

                if (request.Status.HasValue)
                {
                    user.AccountStatus = PortalToAccountStatus(request.Status.Value);
                    user.IsActive = user.AccountStatus == AccountStatus.Active;
                }
                user.UpdatedOnUtc = DateTime.UtcNow;

                var result = await _userManager.UpdateAsync(user);
                if (!result.Succeeded)
                    return Result<AgentDetailsDto>.Failure(ErrorCodes.Exception,
                        string.Join(" ", result.Errors.Select(e => e.Description)));

                return Result<AgentDetailsDto>.Success(ToDto(user), "Agent updated.");
            }
            catch (Exception ex)
            {
                return Result<AgentDetailsDto>.Failure(ErrorCodes.Exception,
                    $"An error occurred while updating the agent. {ex.Message}");
            }
        }

        // ── Deactivate ─────────────────────────────────────────────

        public async Task<Result> DeactivateAsync(Guid id)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(id.ToString());
                if (user is null)
                    return Result.Failure(ErrorCodes.NotFound, "Agent not found.");

                var roles = await _userManager.GetRolesAsync(user);
                if (!roles.Contains(AgentRoleName))
                    return Result.Failure(ErrorCodes.NotFound, "Agent not found.");

                user.IsActive = false;
                user.AccountStatus = AccountStatus.Inactive;
                user.UpdatedOnUtc = DateTime.UtcNow;
                var result = await _userManager.UpdateAsync(user);
                if (!result.Succeeded)
                    return Result.Failure(ErrorCodes.Exception,
                        string.Join(" ", result.Errors.Select(e => e.Description)));

                return Result.Success("Agent deactivated.");
            }
            catch (Exception ex)
            {
                return Result.Failure(ErrorCodes.Exception,
                    $"An error occurred while deactivating the agent. {ex.Message}");
            }
        }

        // ── Reset password ─────────────────────────────────────────

        public async Task<Result<AgentDetailsDto>> ResetPasswordAsync(Guid id)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(id.ToString());
                if (user is null)
                    return Result<AgentDetailsDto>.Failure(ErrorCodes.NotFound, "Agent not found.");

                var roles = await _userManager.GetRolesAsync(user);
                if (!roles.Contains(AgentRoleName))
                    return Result<AgentDetailsDto>.Failure(ErrorCodes.NotFound, "Agent not found.");

                var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                var newPassword = GenerateTempPassword();
                var reset = await _userManager.ResetPasswordAsync(user, token, newPassword);
                if (!reset.Succeeded)
                    return Result<AgentDetailsDto>.Failure(ErrorCodes.Exception,
                        string.Join(" ", reset.Errors.Select(e => e.Description)));

                user.UpdatedOnUtc = DateTime.UtcNow;
                await _userManager.UpdateAsync(user);

                var dto = ToDto(user);
                dto.InitialPassword = newPassword;
                return Result<AgentDetailsDto>.Success(dto, "Password reset. Share the new temporary password with the agent.");
            }
            catch (Exception ex)
            {
                return Result<AgentDetailsDto>.Failure(ErrorCodes.Exception,
                    $"An error occurred while resetting the password. {ex.Message}");
            }
        }

        // ── Helpers ────────────────────────────────────────────────

        private const string ClaimSocial   = "zh:agent:social";
        private const string ClaimNotes    = "zh:agent:notes";
        private const string ClaimProvince = "zh:agent:province";
        private const string ClaimCity     = "zh:agent:city";

        private async Task AddOptionalClaimsAsync(User user, string? social, string? notes, string? province, string? city)
        {
            var toAdd = new List<System.Security.Claims.Claim>();
            if (!string.IsNullOrWhiteSpace(social))   toAdd.Add(new(ClaimSocial,   social.Trim()));
            if (!string.IsNullOrWhiteSpace(notes))    toAdd.Add(new(ClaimNotes,    notes.Trim()));
            if (!string.IsNullOrWhiteSpace(province)) toAdd.Add(new(ClaimProvince, province.Trim()));
            if (!string.IsNullOrWhiteSpace(city))     toAdd.Add(new(ClaimCity,     city.Trim()));
            if (toAdd.Count > 0) await _userManager.AddClaimsAsync(user, toAdd);
        }

        private static void SplitName(string fullName, out string firstName, out string lastName)
        {
            var trimmed = fullName.Trim();
            var space = trimmed.IndexOf(' ');
            if (space < 0) { firstName = trimmed; lastName = ""; return; }
            firstName = trimmed[..space];
            lastName  = trimmed[(space + 1)..].Trim();
        }

        /// <summary>Portal 1/3/4 → AccountStatus.</summary>
        private static AccountStatus PortalToAccountStatus(int s) => s switch
        {
            1 => AccountStatus.Active,
            3 => AccountStatus.Suspended,
            _ => AccountStatus.Inactive,
        };

        /// <summary>AccountStatus → Portal 1/3/4 encoding.</summary>
        private static int AccountStatusToPortal(AccountStatus s) => s switch
        {
            AccountStatus.Active    => 1,
            AccountStatus.Suspended => 3,
            _                       => 4,
        };

        private static AgentDetailsDto ToDto(User u)
        {
            var fullName = string.IsNullOrWhiteSpace(u.LastName)
                ? u.FirstName
                : $"{u.FirstName} {u.LastName}".Trim();

            return new AgentDetailsDto
            {
                Id = u.Id,
                FullName = fullName,
                Email = u.Email ?? string.Empty,
                PhoneNumber = u.PhoneNumber,
                Status = AccountStatusToPortal(u.AccountStatus),
                JoinedDateUtc = u.CreatedOnUtc,
                UpdatedAtUtc = u.UpdatedOnUtc,
            };
        }

        // ── Temp password generator ────────────────────────────────
        // Guarantees at least one character from each of four classes
        // (upper / lower / digit / special) and total length = 8,
        // satisfying IdentityOptions.Password policy configured in
        // ServiceExtensions.AddIdentityServices.
        //
        // Ambiguous characters (O / 0, I / 1 / l) are excluded so the
        // password is easy to relay verbally or by SMS without
        // misreads. Random bytes come from RandomNumberGenerator
        // (cryptographic).
        private const string PoolUpper   = "ABCDEFGHJKMNPQRSTUVWXYZ";
        private const string PoolLower   = "abcdefghjkmnpqrstuvwxyz";
        private const string PoolDigit   = "23456789";
        private const string PoolSpecial = "!@#$%&*?";

        private static string GenerateTempPassword()
        {
            var chars = new char[8];
            chars[0] = PickFrom(PoolUpper);
            chars[1] = PickFrom(PoolLower);
            chars[2] = PickFrom(PoolDigit);
            chars[3] = PickFrom(PoolSpecial);
            var allPools = PoolUpper + PoolLower + PoolDigit + PoolSpecial;
            for (var i = 4; i < chars.Length; i++) chars[i] = PickFrom(allPools);

            // Fisher-Yates shuffle so the four "guarantee" characters
            // aren't always at positions 0-3.
            for (var i = chars.Length - 1; i > 0; i--)
            {
                var j = RandomNumberGenerator.GetInt32(i + 1);
                (chars[i], chars[j]) = (chars[j], chars[i]);
            }
            return new string(chars);
        }

        private static char PickFrom(string pool) => pool[RandomNumberGenerator.GetInt32(pool.Length)];
    }
}
