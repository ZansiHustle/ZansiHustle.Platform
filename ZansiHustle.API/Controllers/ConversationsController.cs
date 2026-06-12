using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ZansiHustle.Application.Chat;
using ZansiHustle.Application.Chat.Dtos;
using ZansiHustle.Shared.Errors;
using ZansiHustle.Shared.Results;

namespace ZansiHustle.API.Controllers
{
    /// <summary>
    /// Buyer ↔ seller chat. Every endpoint requires a signed-in user
    /// and resolves the caller from the JWT — there is no path-level
    /// user id. Participant-only access is enforced inside
    /// <see cref="IChatService"/>; non-participants get a uniform
    /// NOT_FOUND so the controller never leaks the existence of a
    /// conversation that isn't theirs.
    /// </summary>
    [Route("api/conversations")]
    [Authorize]
    public class ConversationsController : BaseController
    {
        private readonly IChatService _chat;

        public ConversationsController(IChatService chat)
        {
            _chat = chat;
        }

        /// <summary>
        /// Get-or-create the conversation between the caller and the
        /// marketplace listing's owner. Returns the same conversation
        /// on repeat calls (idempotent).
        /// </summary>
        [HttpPost("marketplace-listings/{listingId:guid}/start")]
        [ProducesResponseType(typeof(Result<ConversationDetailDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> StartMarketplace(Guid listingId, CancellationToken ct)
        {
            var userId = ResolveCurrentUserId();
            if (userId is null)
                return ToActionResult(Result<ConversationDetailDto>.Failure(
                    ErrorCodes.Unauthorized, "Sign in to start a conversation."));

            return ToActionResult(await _chat.StartMarketplaceConversationAsync(userId.Value, listingId, ct));
        }

        /// <summary>
        /// Get-or-create the conversation between buyer and merchant
        /// for an order. Caller must be buyer or merchant owner.
        /// </summary>
        [HttpPost("orders/{orderId:guid}/start")]
        [ProducesResponseType(typeof(Result<ConversationDetailDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> StartOrder(Guid orderId, CancellationToken ct)
        {
            var userId = ResolveCurrentUserId();
            if (userId is null)
                return ToActionResult(Result<ConversationDetailDto>.Failure(
                    ErrorCodes.Unauthorized, "Sign in to start a conversation."));

            return ToActionResult(await _chat.StartOrderConversationAsync(userId.Value, orderId, ct));
        }

        /// <summary>The caller's inbox — conversations they participate in.</summary>
        [HttpGet]
        [ProducesResponseType(typeof(Result<List<ConversationListItemDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetInbox([FromQuery] int? take, CancellationToken ct)
        {
            var userId = ResolveCurrentUserId();
            if (userId is null)
                return ToActionResult(Result<List<ConversationListItemDto>>.Failure(
                    ErrorCodes.Unauthorized, "Sign in to view your inbox."));

            return ToActionResult(await _chat.GetInboxAsync(userId.Value, take ?? 50, ct));
        }

        /// <summary>
        /// Paged message list. Newest-first; pass `before=<ISO utc>`
        /// to fetch older. Non-participants get NOT_FOUND.
        /// </summary>
        [HttpGet("{conversationId:guid}/messages")]
        [ProducesResponseType(typeof(Result<MessagesPageDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetMessages(
            Guid conversationId,
            [FromQuery] DateTime? before,
            [FromQuery] int? take,
            CancellationToken ct)
        {
            var userId = ResolveCurrentUserId();
            if (userId is null)
                return ToActionResult(Result<MessagesPageDto>.Failure(
                    ErrorCodes.Unauthorized, "Sign in to read messages."));

            return ToActionResult(
                await _chat.GetMessagesAsync(userId.Value, conversationId, before, take ?? 50, ct));
        }

        /// <summary>Send a text message. Non-participants get NOT_FOUND.</summary>
        [HttpPost("{conversationId:guid}/messages")]
        [ProducesResponseType(typeof(Result<MessageDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> Send(
            Guid conversationId,
            [FromBody] SendMessageRequest request,
            CancellationToken ct)
        {
            if (request is null)
                return ToActionResult(Result<MessageDto>.Failure(
                    ErrorCodes.BadRequest, "Request body is required."));

            var userId = ResolveCurrentUserId();
            if (userId is null)
                return ToActionResult(Result<MessageDto>.Failure(
                    ErrorCodes.Unauthorized, "Sign in to send messages."));

            return ToActionResult(
                await _chat.SendMessageAsync(userId.Value, conversationId, request.Body, ct));
        }

        /// <summary>
        /// The caller's chat-unread summary for the bottom-tab badge
        /// (conversations with unread + total unread messages). Independent
        /// of the bell/notification system.
        /// </summary>
        [HttpGet("unread-count")]
        [ProducesResponseType(typeof(Result<ChatUnreadSummaryDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetUnreadCount(CancellationToken ct)
        {
            var userId = ResolveCurrentUserId();
            if (userId is null)
                return ToActionResult(Result<ChatUnreadSummaryDto>.Failure(
                    ErrorCodes.Unauthorized, "Sign in first."));

            return ToActionResult(await _chat.GetUnreadSummaryAsync(userId.Value, ct));
        }

        /// <summary>Mark the conversation as read for the caller.</summary>
        [HttpPost("{conversationId:guid}/read")]
        [ProducesResponseType(typeof(Result<MarkReadResultDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> MarkRead(Guid conversationId, CancellationToken ct)
        {
            var userId = ResolveCurrentUserId();
            if (userId is null)
                return ToActionResult(Result<MarkReadResultDto>.Failure(
                    ErrorCodes.Unauthorized, "Sign in first."));

            return ToActionResult(await _chat.MarkReadAsync(userId.Value, conversationId, ct));
        }

        private Guid? ResolveCurrentUserId()
        {
            var raw = User?.FindFirstValue(ClaimTypes.NameIdentifier)
                      ?? User?.FindFirstValue("sub")
                      ?? User?.FindFirstValue("nameid")
                      ?? User?.FindFirstValue("user_id");
            return Guid.TryParse(raw, out var id) ? id : null;
        }
    }
}
