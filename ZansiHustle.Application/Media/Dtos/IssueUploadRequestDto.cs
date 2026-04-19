using System;
using ZansiHustle.Shared.Enums.Media;

namespace ZansiHustle.Application.Media.Dtos
{
    /// <summary>
    /// Client → server request for an upload slot. Server validates size /
    /// MIME / purpose, creates a Pending MediaAsset row, and returns a
    /// signed PUT ticket. OwnerEntityId is OPTIONAL because the owner
    /// (e.g. Merchant) may not exist yet during onboarding — orphan
    /// uploads get re-parented when the owner row is created.
    /// </summary>
    public class IssueUploadRequestDto
    {
        public MediaPurpose Purpose { get; set; }
        public MediaKind Kind { get; set; }
        public OwnerEntityType OwnerEntityType { get; set; }
        public Guid? OwnerEntityId { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty;
        public long FileSizeBytes { get; set; }
    }
}
