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
        // Format: <Word><Digit><Special>  e.g. "Orange7!"  (8 chars).
        // Word = 6-letter English word, first letter uppercase, rest
        // lowercase. Total length 8, with at least one upper / lower /
        // digit / non-alphanumeric — satisfies the IdentityOptions
        // .Password policy configured in
        // ServiceExtensions.AddIdentityServices (length>=8, RequireUpper,
        // RequireLower, RequireDigit, RequireNonAlphanumeric).
        //
        // Why a curated word list instead of random characters: admins
        // relay this password to agents over WhatsApp / SMS / phone.
        // Random strings like "qX7@aP9!" get misread; "Orange7!" does
        // not.
        //
        // Curated word constraints (enforced by IsValidWord at first
        // use): exactly 6 letters, uppercase first, lowercase rest.
        // ASCII only.
        //
        // Special pool excludes characters that get mangled in URLs
        // and SMS escaping (no &, %, ?, #).
        //
        // ── Bad-edit safety ───────────────────────────────────────
        // The previous implementation used a static field initializer
        // that THREW on the first bad word. Any bad entry then poisoned
        // the type — every subsequent call to a method on this class
        // hit a TypeInitializationException, returning 500 to admins
        // and blocking BOTH Create-Agent and Reset-Password forever.
        //
        // The current implementation FILTERS instead of throwing:
        // bad entries are silently dropped from the runtime pool
        // (and traced via Debug.WriteLine for dev visibility) so a
        // single typo cannot kill the whole agent flow. We only throw
        // if the resulting pool is empty — the truly catastrophic
        // case — and that throw is caught by the per-method catch
        // and surfaced as a clean Result.Failure.

        private static readonly string[] WordPool = new[]
        {
            "Apples", "Banana", "Branch", "Bridge", "Camera", "Castle",
            "Cherry", "Clever", "Cloudy", "Coffee", "Copper", "Cotton",
            "Crayon", "Crisps", "Dancer", "Driver", "Eagles", "Energy",
            "Falcon", "Family", "Flower", "Forest", "Friend", "Garden",
            "Gentle", "Ginger", "Golden", "Growth", "Guitar", "Harbor",
            "Honest", "Hustle", "Indigo", "Island", "Jacket", "Jersey",
            "Jungle", "Junior", "Kayaks", "Kettle", "Knight", "Ladder",
            "Lemons", "Letter", "Lights", "Liquid", "Listen", "Magnet",
            "Market", "Master", "Melody", "Method", "Mirror", "Mobile",
            "Modern", "Monkey", "Mosaic", "Motion", "Native", "Nectar",
            "Nickel", "Nimbus", "Notice", "Nugget", "Oceans", "Office",
            "Orange", "Orbits", "Output", "Oxygen", "Paddle", "Palace",
            "Parrot", "Pencil", "Pepper", "Photon", "Pickle", "Pillar",
            "Pilots", "Planet", "Player", "Pocket", "Polish", "Poster",
            "Pretty", "Public", "Purple", "Rabbit", "Racket", "Random",
            "Reader", "Resort", "Result", "Ribbon", "Rocket", "Rubies",
            "Safari", "Salmon", "Sanity", "Saturn", "Senior", "Shadow",
            "Shield", "Shiver", "Signal", "Silent", "Silver", "Simple",
            "Singer", "Sister", "Skater", "Smiles", "Soccer", "Sonata",
            "Sparks", "Spirit", "Stable", "Static", "Stones", "Studio",
            "Summit", "Sunset", "Sweets", "Tablet", "Tactic", "Tailor",
            "Talent", "Tennis", "Timber", "Tomato", "Toucan", "Trader",
            "Travel", "Tunnel", "Turtle", "Unique", "Valley", "Vector",
            "Velvet", "Vendor", "Violet", "Visual", "Walker", "Wallet",
            "Walnut", "Warmly", "Wealth", "Whales", "Wheels", "Window",
            "Winner", "Winter", "Wisdom", "Wonder", "Yellow", "Yogurt",
            "Zenith", "Zephyr", "Zester", "Zodiac",
        };

        private const string PoolDigit   = "23456789";
        private const string PoolSpecial = "!@$*";

        // Lazy so any unexpected init exception (empty pool) flows
        // through the per-method try/catch as a normal Exception
        // rather than the sticky TypeInitializationException you get
        // from a static field initializer.
        private static readonly Lazy<string[]> _validatedWordPool = new(BuildValidPool);

        private static string[] BuildValidPool()
        {
            var valid = new List<string>(WordPool.Length);
            var skipped = new List<string>();
            foreach (var w in WordPool)
            {
                if (IsValidWord(w)) valid.Add(w);
                else skipped.Add(w ?? "<null>");
            }

            if (skipped.Count > 0)
            {
                // Visible in dev runs / unit tests via the debug listener.
                // We can't inject ILogger into a static helper without
                // turning the whole class non-static — Debug trace is the
                // pragmatic compromise for a "this should never happen"
                // diagnostic.
                System.Diagnostics.Debug.WriteLine(
                    $"[AgentProvisioning] Skipping {skipped.Count} invalid password words: {string.Join(", ", skipped)}");
            }

            if (valid.Count == 0)
            {
                throw new InvalidOperationException(
                    "Agent password word pool is empty after filtering. " +
                    $"All {skipped.Count} entries were invalid: {string.Join(", ", skipped)}");
            }

            return valid.ToArray();
        }

        private static bool IsValidWord(string? w)
        {
            if (string.IsNullOrEmpty(w)) return false;
            if (w.Length != 6) return false;
            if (!char.IsUpper(w[0])) return false;
            for (var i = 1; i < w.Length; i++)
            {
                if (!char.IsLower(w[i])) return false;
            }
            return true;
        }

        private static string GenerateTempPassword()
        {
            var pool   = _validatedWordPool.Value;
            var word   = pool[RandomNumberGenerator.GetInt32(pool.Length)];
            var digit  = PoolDigit[RandomNumberGenerator.GetInt32(PoolDigit.Length)];
            var symbol = PoolSpecial[RandomNumberGenerator.GetInt32(PoolSpecial.Length)];
            return $"{word}{digit}{symbol}";
        }
    }
}
