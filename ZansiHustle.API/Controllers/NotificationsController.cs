using System;
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
    /// In-app notifications for the current user: list, unread badge count, and
    /// mark-read. The bell + notifications page read from here (REST is the
    /// source of truth; SignalR is a live-update layer on top).
    /// </summary>
    [Route("api/notifications")]
    [Authorize]
    public class NotificationsController : BaseController
    {
        private readonly INotificationService _notificationService;
        private readonly ICurrentUserService _currentUserService;

        public NotificationsController(
            INotificationService notificationService,
            ICurrentUserService currentUserService)
        {
            _notificationService = notificationService;
            _currentUserService = currentUserService;
        }

        [HttpGet]
        [ProducesResponseType(typeof(Result<NotificationListDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> Get([FromQuery] int take = 50)
        {
            var userId = _currentUserService.UserId;
            if (!userId.HasValue)
                return ToActionResult(Result<NotificationListDto>.Failure(ErrorCodes.Unauthorized, "User identifier not found in token."));

            var result = await _notificationService.GetForUserAsync(userId.Value, take);
            return ToActionResult(result);
        }

        [HttpGet("unread-count")]
        [ProducesResponseType(typeof(Result<UnreadCountDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetUnreadCount()
        {
            var userId = _currentUserService.UserId;
            if (!userId.HasValue)
                return ToActionResult(Result<UnreadCountDto>.Failure(ErrorCodes.Unauthorized, "User identifier not found in token."));

            var result = await _notificationService.GetUnreadCountAsync(userId.Value);
            return ToActionResult(result);
        }

        [HttpPost("{id:guid}/mark-read")]
        [ProducesResponseType(typeof(Result), StatusCodes.Status200OK)]
        public async Task<IActionResult> MarkRead(Guid id)
        {
            var userId = _currentUserService.UserId;
            if (!userId.HasValue)
                return ToActionResult(Result.Failure(ErrorCodes.Unauthorized, "User identifier not found in token."));

            var result = await _notificationService.MarkReadAsync(userId.Value, id);
            return ToActionResult(result);
        }

        [HttpPost("mark-all-read")]
        [ProducesResponseType(typeof(Result), StatusCodes.Status200OK)]
        public async Task<IActionResult> MarkAllRead()
        {
            var userId = _currentUserService.UserId;
            if (!userId.HasValue)
                return ToActionResult(Result.Failure(ErrorCodes.Unauthorized, "User identifier not found in token."));

            var result = await _notificationService.MarkAllReadAsync(userId.Value);
            return ToActionResult(result);
        }
    }
}
