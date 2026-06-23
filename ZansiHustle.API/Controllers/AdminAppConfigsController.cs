using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using ZansiHustle.API.Realtime;
using ZansiHustle.Application.AppConfigs;
using ZansiHustle.Application.AppConfigs.Dtos;

namespace ZansiHustle.API.Controllers
{
    /// <summary>
    /// Admin management of remote app configs / feature flags. Super Admin only.
    /// </summary>
    [Route("api/admin/app-configs")]
    [Authorize(Roles = "SuperAdmin")]
    public class AdminAppConfigsController : BaseController
    {
        private readonly IAppRuntimeConfigService _service;
        private readonly IHubContext<AppConfigHub> _appConfigHub;
        private readonly ILogger<AdminAppConfigsController> _logger;

        public AdminAppConfigsController(
            IAppRuntimeConfigService service,
            IHubContext<AppConfigHub> appConfigHub,
            ILogger<AdminAppConfigsController> logger)
        {
            _service = service;
            _appConfigHub = appConfigHub;
            _logger = logger;
        }

        /// <summary>All active config rows for the admin UI.</summary>
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var result = await _service.GetAllAsync();
            return ToActionResult(result);
        }

        /// <summary>Update one config row by key (toggle + edit messages/visibility).</summary>
        [HttpPut("{key}")]
        public async Task<IActionResult> Update(string key, [FromBody] UpdateAppConfigRequestDto request)
        {
            var userId = ResolveCurrentUserId();
            var result = await _service.UpdateAsync(key, request, userId);
            if (result.IsSuccess)
            {
                _logger.LogInformation(
                    "App config '{Key}' updated by {UserId}.", key, userId);

                // Broadcast a lightweight change signal so online clients re-fetch
                // the public map live. NEVER push config values here. SignalR
                // failure must not fail the save — log and continue.
                try
                {
                    var updatedAt = result.Data?.UpdatedAtUtc ?? DateTime.UtcNow;
                    await _appConfigHub.Clients.All.SendAsync("appConfigsChanged", new
                    {
                        version = updatedAt.Ticks,
                        updatedAt,
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "appConfigsChanged broadcast failed for '{Key}'.", key);
                }
            }
            return ToActionResult(result);
        }

        private Guid? ResolveCurrentUserId()
        {
            var raw = User?.FindFirstValue(ClaimTypes.NameIdentifier)
                      ?? User?.FindFirstValue("sub")
                      ?? User?.FindFirstValue("nameid");
            return Guid.TryParse(raw, out var id) ? id : null;
        }
    }
}
