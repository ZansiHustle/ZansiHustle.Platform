using System;

namespace ZansiHustle.Domain.Blocks
{
    /// <summary>
    /// One user blocking another (App Store Guideline 1.2 — users must be able
    /// to block abusive users). Directional: <see cref="BlockerUserId"/> chose
    /// to block <see cref="BlockedUserId"/>. Enforced in chat (no new
    /// conversations/messages either direction) and used client-side to hide the
    /// blocked user's content.
    /// </summary>
    public class UserBlock
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>The user who initiated the block.</summary>
        public Guid BlockerUserId { get; set; }

        /// <summary>The user who was blocked.</summary>
        public Guid BlockedUserId { get; set; }

        /// <summary>Optional reason (private to the blocker).</summary>
        public string? Reason { get; set; }

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    }
}
