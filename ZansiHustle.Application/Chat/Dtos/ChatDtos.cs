using System;
using System.Collections.Generic;
using ZansiHustle.Shared.Enums.Chat;

namespace ZansiHustle.Application.Chat.Dtos
{
    /// <summary>
    /// Request body for `POST /api/conversations/{id}/messages`.
    /// </summary>
    public class SendMessageRequest
    {
        public string Body { get; set; } = string.Empty;
    }

    /// <summary>
    /// One row in the buyer's / seller's inbox.
    /// Cached `LastMessage*` columns + the joined "other participant"
    /// name/photo so the list renders without per-row lookups.
    /// </summary>
    public class ConversationListItemDto
    {
        public Guid Id { get; set; }
        public ConversationType Type { get; set; }

        /// <summary>The OTHER participant — never the caller.</summary>
        public Guid OtherUserId { get; set; }
        public string OtherUserName { get; set; } = string.Empty;
        public string? OtherUserAvatarUrl { get; set; }

        // Anchor context (filled in only for the relevant Type so the
        // mobile inbox can render "About: <listing title>").
        public Guid? MarketplaceListingId { get; set; }
        public string? MarketplaceListingTitle { get; set; }
        public string? MarketplaceListingImageUrl { get; set; }
        public decimal? MarketplaceListingPrice { get; set; }

        public Guid? OrderId { get; set; }
        public string? OrderCode { get; set; }

        public DateTime CreatedAtUtc { get; set; }
        public DateTime? LastMessageAtUtc { get; set; }
        public string? LastMessagePreview { get; set; }

        /// <summary>
        /// Count of messages newer than the caller's LastReadAtUtc
        /// (capped at 99 server-side so the inbox badge never has to
        /// render "1,247 unread"). Always 0 for conversations the
        /// caller authored the latest message of.
        /// </summary>
        public int UnreadCount { get; set; }
    }

    /// <summary>
    /// A single message inside a conversation.
    /// </summary>
    public class MessageDto
    {
        public Guid Id { get; set; }
        public Guid ConversationId { get; set; }
        public Guid? SenderUserId { get; set; }
        public string? SenderName { get; set; }
        public string Body { get; set; } = string.Empty;
        public MessageType Type { get; set; }
        public DateTime CreatedAtUtc { get; set; }
    }

    /// <summary>
    /// Detail payload returned by the conversation-detail / start
    /// endpoints. Carries the same identity + anchor info as the inbox
    /// row plus the participant ids so the client can render "you" vs
    /// "them" without an extra lookup.
    /// </summary>
    public class ConversationDetailDto
    {
        public Guid Id { get; set; }
        public ConversationType Type { get; set; }

        public Guid? BuyerUserId { get; set; }
        public string? BuyerUserName { get; set; }
        public Guid? SellerUserId { get; set; }
        public string? SellerUserName { get; set; }

        public Guid? MarketplaceListingId { get; set; }
        public string? MarketplaceListingTitle { get; set; }
        public string? MarketplaceListingImageUrl { get; set; }
        public decimal? MarketplaceListingPrice { get; set; }

        public Guid? OrderId { get; set; }
        public string? OrderCode { get; set; }

        public DateTime CreatedAtUtc { get; set; }
        public DateTime? LastMessageAtUtc { get; set; }
        public string? LastMessagePreview { get; set; }
        public bool IsClosed { get; set; }
    }

    /// <summary>
    /// Paged messages response. `HasMore` true when more older
    /// messages exist beyond the returned page; the client passes the
    /// oldest returned message's CreatedAtUtc as `before` to fetch
    /// the next older page.
    /// </summary>
    public class MessagesPageDto
    {
        public List<MessageDto> Messages { get; set; } = new();
        public bool HasMore { get; set; }
    }

    /// <summary>
    /// Returned by the read-receipt endpoint. The caller's
    /// LastReadAtUtc has been stamped to the conversation's
    /// LastMessageAtUtc (or `now` when the conversation is empty).
    /// </summary>
    public class MarkReadResultDto
    {
        public Guid ConversationId { get; set; }
        public DateTime LastReadAtUtc { get; set; }
    }
}
