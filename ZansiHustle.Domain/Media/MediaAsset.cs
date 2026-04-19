using System;
using ZansiHustle.Shared.Enums.Media;

namespace ZansiHustle.Domain.Media
{
    /// <summary>
    /// Single shared media record. Polymorphic via OwnerEntityType +
    /// OwnerEntityId so the table backs verification documents, listing
    /// galleries, profile avatars, etc. without needing per-consumer FKs.
    ///
    /// Raw bytes live in blob storage at (StorageContainer, StorageKey).
    /// This entity carries metadata only.
    /// </summary>
    public class MediaAsset
    {
        public Guid Id { get; set; }

        // ── Ownership (polymorphic) ──────────────────────────────────────
        // OwnerEntityId is nullable so the client can request an upload
        // slot before the owner exists (e.g. during onboarding, before the
        // Merchant row is created). Re-parented when the owner is created.
        public OwnerEntityType OwnerEntityType { get; set; }
        public Guid? OwnerEntityId { get; set; }

        // The user who initiated the upload. Used for ownership checks
        // when re-parenting orphan uploads onto the new owner row.
        public Guid UploadedByUserId { get; set; }

        // ── Classification ───────────────────────────────────────────────
        public MediaKind Kind { get; set; }
        public MediaPurpose Purpose { get; set; }
        public MediaVisibility Visibility { get; set; }
        public MediaStatus Status { get; set; } = MediaStatus.Pending;

        // ── Storage ──────────────────────────────────────────────────────
        /// <summary>Logical container — "private" or "public".</summary>
        public string StorageContainer { get; set; } = "private";

        /// <summary>Object key within the container. Globally unique.</summary>
        public string StorageKey { get; set; } = string.Empty;

        public string FileName { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty;
        public long FileSizeBytes { get; set; }

        // ── Video extras (nullable for images/documents) ─────────────────
        public int? DurationSeconds { get; set; }
        public string? ThumbnailStorageKey { get; set; }

        // ── Display ──────────────────────────────────────────────────────
        public int SortOrder { get; set; }

        // ── Review (verification-class only) ─────────────────────────────
        public Guid? ReviewedByUserId { get; set; }
        public DateTime? ReviewedAtUtc { get; set; }
        public string? RejectionReason { get; set; }

        // ── Audit ────────────────────────────────────────────────────────
        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
        public DateTime? UploadedAtUtc { get; set; }
        public DateTime? UpdatedAtUtc { get; set; }
    }
}
