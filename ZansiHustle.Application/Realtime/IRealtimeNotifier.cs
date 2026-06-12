using System;
using System.Threading.Tasks;

namespace ZansiHustle.Application.Realtime
{
    /// <summary>
    /// Pushes live, user-targeted events to connected clients (SignalR). Defined
    /// in Application so services stay framework-agnostic; the implementation
    /// (SignalR hub context) lives in the API layer. Best-effort: a transport
    /// failure must never break the business operation — REST remains the source
    /// of truth and the client also refetches.
    /// </summary>
    public interface IRealtimeNotifier
    {
        /// <summary>A new notification was created for <paramref name="userId"/>.</summary>
        Task NotificationCreatedAsync(Guid userId, object payload);

        /// <summary>The user's unread notification count changed.</summary>
        Task UnreadCountChangedAsync(Guid userId, int unreadCount);

        /// <summary>A booking the user is party to changed status.</summary>
        Task BookingStatusChangedAsync(Guid userId, object payload);

        /// <summary>The user's wallet balance changed (e.g. a refund credit).</summary>
        Task WalletBalanceChangedAsync(Guid userId, object payload);

        /// <summary>A new chat message arrived in a conversation the user is in
        /// (pushed to the RECIPIENT only — never the sender). The payload carries
        /// the conversationId + the message so the client can append it to an open
        /// thread without a refetch.</summary>
        Task ConversationMessageReceivedAsync(Guid userId, object payload);

        /// <summary>The user's chat unread total changed (number of conversations
        /// with unread messages). Lets the chat tab badge update without polling.</summary>
        Task ConversationUnreadCountChangedAsync(Guid userId, int unreadConversations);
    }
}
