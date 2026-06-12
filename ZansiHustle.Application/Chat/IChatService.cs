using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ZansiHustle.Application.Chat.Dtos;
using ZansiHustle.Shared.Results;

namespace ZansiHustle.Application.Chat
{
    /// <summary>
    /// Buyer ↔ seller / buyer ↔ merchant chat service.
    ///
    /// Authorisation contract — every method takes the caller's
    /// `userId` and enforces:
    ///   • Start: caller != target seller / merchant (no self-chat).
    ///   • Read / write / mark-read: caller MUST be a participant of
    ///     the conversation. Non-participants get NOT_FOUND (we
    ///     deliberately don't differentiate from "does not exist" to
    ///     avoid leaking conversation ids).
    ///
    /// All methods return Result / Result&lt;T&gt;. No exceptions cross
    /// the service boundary — controllers map errors via the existing
    /// BaseController.MapFailure switch.
    /// </summary>
    public interface IChatService
    {
        /// <summary>
        /// Get-or-create the marketplace listing conversation between
        /// the caller and the listing's owner. Idempotent on retry —
        /// a second call from the same buyer returns the same id.
        /// </summary>
        Task<Result<ConversationDetailDto>> StartMarketplaceConversationAsync(
            Guid callerUserId, Guid listingId, CancellationToken ct = default);

        /// <summary>
        /// Get-or-create the order conversation. Caller must be the
        /// order's buyer OR the merchant's owner; anyone else gets
        /// FORBIDDEN.
        /// </summary>
        Task<Result<ConversationDetailDto>> StartOrderConversationAsync(
            Guid callerUserId, Guid orderId, CancellationToken ct = default);

        Task<Result<List<ConversationListItemDto>>> GetInboxAsync(
            Guid callerUserId, int take = 50, CancellationToken ct = default);

        /// <summary>
        /// Paginated message list for a conversation the caller
        /// participates in. Returns up to `take` messages older than
        /// `before` (or the newest `take` if `before` is null). The
        /// list is returned NEWEST-FIRST for convenience — the mobile
        /// client renders the array reversed for chronological order.
        /// </summary>
        Task<Result<MessagesPageDto>> GetMessagesAsync(
            Guid callerUserId, Guid conversationId, DateTime? before = null,
            int take = 50, CancellationToken ct = default);

        Task<Result<MessageDto>> SendMessageAsync(
            Guid callerUserId, Guid conversationId, string body, CancellationToken ct = default);

        Task<Result<MarkReadResultDto>> MarkReadAsync(
            Guid callerUserId, Guid conversationId, CancellationToken ct = default);

        /// <summary>
        /// The caller's chat-unread summary for the bottom-tab badge:
        /// number of conversations that have at least one unread message,
        /// plus the total unread message count (both capped). Cheap — a
        /// single grouped query. Independent of the bell/notification system.
        /// </summary>
        Task<Result<ChatUnreadSummaryDto>> GetUnreadSummaryAsync(
            Guid callerUserId, CancellationToken ct = default);
    }
}
