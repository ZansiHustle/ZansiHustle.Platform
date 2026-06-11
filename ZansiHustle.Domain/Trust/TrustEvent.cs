using System;
using ZansiHustle.Shared.Enums.Trust;

namespace ZansiHustle.Domain.Trust
{
    /// <summary>
    /// A recorded trust signal for a user. Append-only history — NO scoring,
    /// penalties, or user-facing labels in this pass. <see cref="ScoreImpact"/>
    /// is reserved for a later scoring layer; <see cref="MetadataJson"/> carries
    /// context (e.g. the rejection reason code).
    /// </summary>
    public class TrustEvent
    {
        public Guid Id { get; set; }

        public Guid UserId { get; set; }

        public TrustActorRole ActorRole { get; set; }
        public TrustEventType Type { get; set; }

        public string? ReferenceType { get; set; }
        public Guid? ReferenceId { get; set; }

        /// <summary>Reserved for future scoring; null for now.</summary>
        public decimal? ScoreImpact { get; set; }

        /// <summary>Opaque JSON context (e.g. {"reasonCode":"NotAvailable"}).</summary>
        public string? MetadataJson { get; set; }

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    }
}
