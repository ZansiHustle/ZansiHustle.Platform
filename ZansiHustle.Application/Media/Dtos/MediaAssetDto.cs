using System;
using ZansiHustle.Shared.Enums.Media;

namespace ZansiHustle.Application.Media.Dtos
{
    public class MediaAssetDto
    {
        public Guid Id { get; set; }
        public OwnerEntityType OwnerEntityType { get; set; }
        public Guid? OwnerEntityId { get; set; }
        public Guid UploadedByUserId { get; set; }
        public MediaKind Kind { get; set; }
        public MediaPurpose Purpose { get; set; }
        public MediaVisibility Visibility { get; set; }
        public MediaStatus Status { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty;
        public long FileSizeBytes { get; set; }
        public int? DurationSeconds { get; set; }
        public int SortOrder { get; set; }
        /// <summary>Pre-resolved read URL. Public assets get a CDN URL; private assets get a short-TTL signed URL.</summary>
        public string? ReadUrl { get; set; }
        public string? RejectionReason { get; set; }
        public DateTime? UploadedAtUtc { get; set; }
        public DateTime? ReviewedAtUtc { get; set; }
        public DateTime CreatedAtUtc { get; set; }
    }
}
