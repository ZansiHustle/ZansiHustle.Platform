using System;
using System.Collections.Generic;
using ZansiHustle.Shared.Enums.Chat;

namespace ZansiHustle.Domain.Chat
{
    /// <summary>
    /// A two-party chat thread. The Type discriminates which anchor
    /// FK is meaningful: MarketplaceListingId for buyer ↔ seller
    /// pre-sale chat, OrderId for buyer ↔ merchant post-order chat.
    ///
    /// LastMessage* columns are denormalised so the inbox list query
    /// can return preview text + timestamp + counts without joining
    /// the Messages table for every row. The service updates them in
    /// the same transaction as the message insert.
    ///
    /// IsClosed is reserved for moderation / "this listing was
    /// removed" scenarios — set to true to lock the conversation
    /// without deleting history. v1 only sets it when a marketplace
    /// listing is hard-deleted (today: never).
    /// </summary>
    public class Conversation
    {
        public Guid Id { get; set; }

        public ConversationType Type { get; set; }

        /// <summary>Set only when Type == MarketplaceListing.</summary>
        public Guid? MarketplaceListingId { get; set; }

        /// <summary>Set only when Type == Order.</summary>
        public Guid? OrderId { get; set; }

        /// <summary>
        /// Cached anchor pair. For MarketplaceListing this is the
        /// listing's `OwnerUserId`; for Order it's the order's buyer.
        /// Cached so we don't re-resolve them on every send / read.
        /// </summary>
        public Guid? BuyerUserId { get; set; }
        public Guid? SellerUserId { get; set; }

        public DateTime CreatedAtUtc { get; set; }
        public DateTime UpdatedAtUtc { get; set; }

        /// <summary>
        /// When the most recent message landed. Drives inbox sort
        /// order ("most recently active on top"). Updated atomically
        /// with each message insert. Null only until the first
        /// message is sent — which, in v1, the start endpoint never
        /// triggers (the conversation starts empty and the buyer
        /// sends the first message manually).
        /// </summary>
        public DateTime? LastMessageAtUtc { get; set; }

        /// <summary>
        /// Plain-text preview of the most recent message body.
        /// Trimmed to 200 chars when written. Null until the first
        /// message is sent. Used by the inbox list so we don't have
        /// to fetch the message body for every row.
        /// </summary>
        public string? LastMessagePreview { get; set; }

        public bool IsClosed { get; set; }

        public virtual ICollection<ConversationParticipant> Participants { get; set; }
            = new List<ConversationParticipant>();

        public virtual ICollection<Message> Messages { get; set; }
            = new List<Message>();
    }
}
