using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ZansiHustle.Application.Common.Interfaces.Shared;
using ZansiHustle.Application.Notifications;
using ZansiHustle.Application.Notifications.Dtos;
using ZansiHustle.Shared.Errors;
using ZansiHustle.Shared.Results;

namespace ZansiHustle.API.Controllers
{
    /// <summary>
    /// Push-device registration for the current user. The mobile app registers
    /// its OneSignal player id after login and unregisters on logout. Push
    /// delivery is best-effort — these endpoints only manage the registry.
    /// </summary>
    [Route("api/notification-devices")]
    [Authorize]
    public class NotificationDevicesController : BaseController
    {
        private readonly INotificationService _notificationService;
        private readonly ICurrentUserService _currentUserService;

        public NotificationDevicesController(
            INotificationService notificationService,
            ICurrentUserService currentUserService)
        {
            _notificationService = notificationService;
            _currentUserService = currentUserService;
        }

        [HttpPost("register")]
        [ProducesResponseType(typeof(Result), StatusCodes.Status200OK)]
        public async Task<IActionResult> Register([FromBody] RegisterDeviceRequestDto request)
        {
            var userId = _currentUserService.UserId;
            if (!userId.HasValue)
                return ToActionResult(Result.Failure(ErrorCodes.Unauthorized, "User identifier not found in token."));

            var result = await _notificationService.RegisterDeviceAsync(userId.Value, request);
            return ToActionResult(result);
        }

        [HttpPost("unregister")]
        [ProducesResponseType(typeof(Result), StatusCodes.Status200OK)]
        public async Task<IActionResult> Unregister([FromBody] UnregisterDeviceRequestDto request)
        {
            var userId = _currentUserService.UserId;
            if (!userId.HasValue)
                return ToActionResult(Result.Failure(ErrorCodes.Unauthorized, "User identifier not found in token."));

            var result = await _notificationService.UnregisterDeviceAsync(userId.Value, request?.PlayerId ?? string.Empty);
            return ToActionResult(result);
        }
    }
}
