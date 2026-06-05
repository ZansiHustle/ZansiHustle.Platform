using System;
using ZansiHustle.Domain.Identity;
using ZansiHustle.Shared.Enums.Chat;

namespace ZansiHustle.Domain.Chat
{
    /// <summary>
    /// A single chat message inside a Conversation. Text-only in v1.
    /// SenderUserId is nullable so that auto-generated System messages
    /// (e.g. "Order #ZH-001 created on 2026-05-29") don't need to
    /// pretend they came from a user — the row carries Type == System
    /// and a null sender so the client renders it as system chrome.
    ///
    /// EditedAtUtc / DeletedAtUtc are reserved for future edit /
    /// soft-delete features. v1 never sets them; messages are
    /// immutable. The fields exist so the table doesn't have to be
    /// altered to add those features later.
    /// </summary>
    public class Message
    {
        public Guid Id { get; set; }

        public Guid ConversationId { get; set; }
        public virtual Conversation? Conversation { get; set; }

        /// <summary>
        /// Null only for System messages emitted by the server.
        /// User-sent messages always carry a non-null SenderUserId.
        /// </summary>
        public Guid? SenderUserId { get; set; }
        public virtual User? Sender { get; set; }

        /// <summary>
        /// Text body. Bounded at 4 000 chars in the EF configuration —
        /// generous enough for legitimate use, small enough to keep
        /// row scans cheap and to neutralise oversized-payload abuse.
        /// </summary>
        public string Body { get; set; } = string.Empty;

        public MessageType Type { get; set; } = MessageType.Text;

        public DateTime CreatedAtUtc { get; set; }
        public DateTime? EditedAtUtc { get; set; }
        public DateTime? DeletedAtUtc { get; set; }
    }
}
