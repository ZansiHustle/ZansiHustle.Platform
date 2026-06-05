using System;
using ZansiHustle.Domain.Identity;

namespace ZansiHustle.Domain.Chat
{
    /// <summary>
    /// One side of a Conversation. Two rows per conversation in v1
    /// (buyer + seller). Modelled as its own table — rather than two
    /// FK columns on Conversation — so per-participant state
    /// (LastReadAtUtc, IsArchived, …) is row-local and the participant
    /// count can grow later (group support, internal escalation) without
    /// a schema rewrite.
    ///
    /// Unique index on (ConversationId, UserId) — a user can never be
    /// added twice to the same conversation. Hot read path: "all
    /// conversations I'm in" filters by UserId.
    /// </summary>
    public class ConversationParticipant
    {
        public Guid Id { get; set; }

        public Guid ConversationId { get; set; }
        public virtual Conversation? Conversation { get; set; }

        public Guid UserId { get; set; }
        public virtual User? User { get; set; }

        /// <summary>
        /// Timestamp of the newest message THIS participant has seen.
        /// Compared against the conversation's `LastMessageAtUtc` to
        /// derive an unread flag for the inbox list. Null means the
        /// participant has never opened the conversation since
        /// joining — every message is unread.
        /// </summary>
        public DateTime? LastReadAtUtc { get; set; }

        /// <summary>
        /// Reserved for a future "archive" affordance on the inbox.
        /// Default false. v1 never sets it; the field exists so the
        /// migration doesn't have to be reworked when we add archive.
        /// </summary>
        public bool IsArchived { get; set; }

        public DateTime CreatedAtUtc { get; set; }
    }
}
