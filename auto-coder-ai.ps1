param(
    [string]$Root = ".",
    [switch]$DryRun = $false
)

$ErrorActionPreference = "Stop"

function Write-Info($msg) { Write-Host "[INFO] $msg" -ForegroundColor Cyan }
function Write-Ok($msg)   { Write-Host "[ OK ] $msg" -ForegroundColor Green }
function Write-Warn($msg) { Write-Host "[WARN] $msg" -ForegroundColor Yellow }

function Ensure-Dir {
    param([string]$Path)
    if (-not (Test-Path $Path)) {
        if (-not $DryRun) {
            New-Item -ItemType Directory -Path $Path -Force | Out-Null
        }
        Write-Ok "Created directory: $Path"
    }
}

function Write-FileUtf8 {
    param(
        [string]$Path,
        [string]$Content
    )

    $dir = Split-Path $Path -Parent
    Ensure-Dir $dir

    if (-not $DryRun) {
        Set-Content -Path $Path -Value $Content -Encoding UTF8
    }

    Write-Ok "Wrote file: $Path"
}

function Replace-InFile {
    param(
        [string]$Path,
        [string]$Pattern,
        [string]$Replacement
    )

    if (-not (Test-Path $Path)) {
        Write-Warn "File not found, skipping replace: $Path"
        return
    }

    $content = Get-Content -Path $Path -Raw
    $updated = [regex]::Replace($content, $Pattern, $Replacement)

    if ($updated -ne $content) {
        if (-not $DryRun) {
            Set-Content -Path $Path -Value $updated -Encoding UTF8
        }
        Write-Ok "Updated file: $Path"
    }
    else {
        Write-Warn "No changes made in: $Path"
    }
}

$rootPath = (Resolve-Path $Root).Path

Write-Info "Root path: $rootPath"
Write-Info "DryRun: $DryRun"

# -------------------------------------------------------------------
# Paths
# -------------------------------------------------------------------
$apiControllersDir = Join-Path $rootPath "ZansiHustle.API\Controllers"
$appTeamMembersDir = Join-Path $rootPath "ZansiHustle.Application\TeamMembers"
$appTeamMembersDtosDir = Join-Path $appTeamMembersDir "Dtos"

$serviceExtensionsPath = Join-Path $rootPath "ZansiHustle.API\Extensions\ServiceExtensions.cs"

# -------------------------------------------------------------------
# 1. DTOs
# -------------------------------------------------------------------
$teamMemberDto = @"
using System;
using System.Collections.Generic;

namespace ZansiHustle.Application.TeamMembers.Dtos
{
    /// <summary>
    /// Team member DTO used by admin/portal screens.
    /// </summary>
    public class TeamMemberDto
    {
        public string Id { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public List<string> Roles { get; set; } = new();
        public bool TeamPortalEnabled { get; set; }
        public bool AdminPortalEnabled { get; set; }
        public bool FinanceAccess { get; set; }
        public string Status { get; set; } = "active";
        public string? Department { get; set; }
        public DateTime JoinedDateUtc { get; set; }
        public string? Notes { get; set; }
    }
}
"@

$createTeamMemberRequestDto = @"
using System.Collections.Generic;

namespace ZansiHustle.Application.TeamMembers.Dtos
{
    /// <summary>
    /// Request model used to create a team member based on the Users table.
    /// </summary>
    public class CreateTeamMemberRequestDto
    {
        public string Email { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public List<string> Roles { get; set; } = new();
        public bool TeamPortalEnabled { get; set; } = true;
        public bool AdminPortalEnabled { get; set; }
        public bool FinanceAccess { get; set; }
        public string? Department { get; set; }
        public string? Notes { get; set; }
        public string Password { get; set; } = string.Empty;
    }
}
"@

$updateTeamMemberRequestDto = @"
using System.Collections.Generic;

namespace ZansiHustle.Application.TeamMembers.Dtos
{
    /// <summary>
    /// Request model used to update a team member based on the Users table.
    /// </summary>
    public class UpdateTeamMemberRequestDto
    {
        public string FullName { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public List<string> Roles { get; set; } = new();
        public bool TeamPortalEnabled { get; set; } = true;
        public bool AdminPortalEnabled { get; set; }
        public bool FinanceAccess { get; set; }
        public string Status { get; set; } = "active";
        public string? Department { get; set; }
        public string? Notes { get; set; }
    }
}
"@

# -------------------------------------------------------------------
# 2. Service interface
# -------------------------------------------------------------------
$iTeamMemberService = @"
using System.Collections.Generic;
using System.Threading.Tasks;
using ZansiHustle.Application.TeamMembers.Dtos;
using ZansiHustle.Shared.Results;

namespace ZansiHustle.Application.TeamMembers
{
    /// <summary>
    /// Service contract for team member CRUD operations using the Users table.
    /// </summary>
    public interface ITeamMemberService
    {
        Task<Result<List<TeamMemberDto>>> GetAllAsync();
        Task<Result<TeamMemberDto>> GetByIdAsync(string id);
        Task<Result<TeamMemberDto>> CreateAsync(CreateTeamMemberRequestDto request);
        Task<Result<TeamMemberDto>> UpdateAsync(string id, UpdateTeamMemberRequestDto request);
        Task<Result> DeleteAsync(string id);
    }
}
"@

# -------------------------------------------------------------------
# 3. Service implementation
# -------------------------------------------------------------------
$teamMemberService = @"
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ZansiHustle.Application.TeamMembers.Dtos;
using ZansiHustle.Domain.Identity;
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
                    TeamPortalEnabled = request.TeamPortalEnabled,
                    AdminPortalEnabled = request.AdminPortalEnabled,
                    FinanceAccess = request.FinanceAccess,
                    Department = request.Department?.Trim(),
                    Notes = request.Notes?.Trim(),
                    JoinedDateUtc = DateTime.UtcNow,
                    CreatedAtUtc = DateTime.UtcNow
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
                user.TeamPortalEnabled = request.TeamPortalEnabled;
                user.AdminPortalEnabled = request.AdminPortalEnabled;
                user.FinanceAccess = request.FinanceAccess;
                user.Department = request.Department?.Trim();
                user.Notes = request.Notes?.Trim();
                user.IsActive = string.Equals(request.Status, "active", StringComparison.OrdinalIgnoreCase);
                user.UpdatedAtUtc = DateTime.UtcNow;

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
                TeamPortalEnabled = user.TeamPortalEnabled,
                AdminPortalEnabled = user.AdminPortalEnabled,
                FinanceAccess = user.FinanceAccess,
                Status = user.IsActive ? "active" : "inactive",
                Department = user.Department,
                JoinedDateUtc = user.JoinedDateUtc ?? user.CreatedAtUtc ?? DateTime.UtcNow,
                Notes = user.Notes
            };
        }

        private static bool IsTeamUser(User user, IEnumerable<string> roles)
        {
            if (user.TeamPortalEnabled || user.AdminPortalEnabled)
                return true;

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
"@

# -------------------------------------------------------------------
# 4. Controller
# -------------------------------------------------------------------
$teamMembersController = @"
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ZansiHustle.Application.TeamMembers;
using ZansiHustle.Application.TeamMembers.Dtos;
using ZansiHustle.Shared.Results;

namespace ZansiHustle.API.Controllers
{
    /// <summary>
    /// Exposes endpoints for team member management using the Users table.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class TeamMembersController : ControllerBase
    {
        private readonly ITeamMemberService _teamMemberService;

        public TeamMembersController(ITeamMemberService teamMemberService)
        {
            _teamMemberService = teamMemberService;
        }

        [HttpGet]
        [ProducesResponseType(typeof(Result<System.Collections.Generic.List<TeamMemberDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAll()
        {
            var result = await _teamMemberService.GetAllAsync();
            return Ok(result);
        }

        [HttpGet("{id}")]
        [ProducesResponseType(typeof(Result<TeamMemberDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetById(string id)
        {
            var result = await _teamMemberService.GetByIdAsync(id);
            return Ok(result);
        }

        [HttpPost]
        [ProducesResponseType(typeof(Result<TeamMemberDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> Create([FromBody] CreateTeamMemberRequestDto request)
        {
            var result = await _teamMemberService.CreateAsync(request);
            return Ok(result);
        }

        [HttpPut("{id}")]
        [ProducesResponseType(typeof(Result<TeamMemberDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> Update(string id, [FromBody] UpdateTeamMemberRequestDto request)
        {
            var result = await _teamMemberService.UpdateAsync(id, request);
            return Ok(result);
        }

        [HttpDelete("{id}")]
        [ProducesResponseType(typeof(Result), StatusCodes.Status200OK)]
        public async Task<IActionResult> Delete(string id)
        {
            var result = await _teamMemberService.DeleteAsync(id);
            return Ok(result);
        }
    }
}
"@

# -------------------------------------------------------------------
# 5. Write files
# -------------------------------------------------------------------
Write-FileUtf8 -Path (Join-Path $appTeamMembersDtosDir "TeamMemberDto.cs") -Content $teamMemberDto
Write-FileUtf8 -Path (Join-Path $appTeamMembersDtosDir "CreateTeamMemberRequestDto.cs") -Content $createTeamMemberRequestDto
Write-FileUtf8 -Path (Join-Path $appTeamMembersDtosDir "UpdateTeamMemberRequestDto.cs") -Content $updateTeamMemberRequestDto
Write-FileUtf8 -Path (Join-Path $appTeamMembersDir "ITeamMemberService.cs") -Content $iTeamMemberService
Write-FileUtf8 -Path (Join-Path $appTeamMembersDir "TeamMemberService.cs") -Content $teamMemberService
Write-FileUtf8 -Path (Join-Path $apiControllersDir "TeamMembersController.cs") -Content $teamMembersController

# -------------------------------------------------------------------
# 6. Update ServiceExtensions
# -------------------------------------------------------------------
Replace-InFile -Path $serviceExtensionsPath `
    -Pattern 'using ZansiHustle\.Application\.TeamMembers;\s*' `
    -Replacement 'using ZansiHustle.Application.TeamMembers;' 

Replace-InFile -Path $serviceExtensionsPath `
    -Pattern '(using ZansiHustle\.Application\.Users;\s*)' `
    -Replacement "`$1`r`nusing ZansiHustle.Application.TeamMembers;`r`n"

Replace-InFile -Path $serviceExtensionsPath `
    -Pattern '(services\.AddScoped<IUserSettingsService,\s*UserSettingsService>\(\);\s*)' `
    -Replacement "`$1`r`n        services.AddScoped<ITeamMemberService, TeamMemberService>();"

Write-Info "Done."
Write-Warn "Next:"
Write-Warn "1. Ensure User entity has TeamPortalEnabled/AdminPortalEnabled/FinanceAccess/Department/Notes/JoinedDateUtc/CreatedAtUtc/UpdatedAtUtc."
Write-Warn "2. Run dotnet build."
Write-Warn "3. If User is missing any field above, either add them or trim the DTO/service mapping."
Write-Warn "4. Optional: secure controller with role policies."