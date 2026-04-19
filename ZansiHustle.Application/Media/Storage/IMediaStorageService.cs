using System;
using System.Threading.Tasks;

namespace ZansiHustle.Application.Media.Storage
{
    /// <summary>
    /// Storage-provider abstraction. Production implementation is Azure Blob;
    /// dev/test implementation writes under wwwroot/_media. Anything that
    /// does not fit this surface (transcoding, image resizing, etc.) belongs
    /// in a separate concern, NOT in the storage adapter.
    /// </summary>
    public interface IMediaStorageService
    {
        /// <summary>
        /// Generates an upload URL the client can PUT a blob to. Returns
        /// optional headers the client must echo (e.g. x-ms-blob-type for
        /// Azure). The storage key is unique within the container.
        /// </summary>
        Task<MediaUploadTicket> IssueUploadAsync(
            string container, string storageKey, string contentType, long maxSizeBytes, TimeSpan ttl);

        /// <summary>
        /// Returns a short-TTL signed read URL. Public-container assets MAY
        /// return a non-signed CDN URL — the client never assumes signed.
        /// </summary>
        Task<string> IssueReadUrlAsync(string container, string storageKey, TimeSpan ttl);

        /// <summary>True if the blob is present (used to confirm finalize).</summary>
        Task<bool> ExistsAsync(string container, string storageKey);

        /// <summary>Best-effort delete; safe to call on missing blobs.</summary>
        Task DeleteAsync(string container, string storageKey);
    }

    /// <summary>Server response describing how the client should PUT the blob.</summary>
    public class MediaUploadTicket
    {
        public string UploadUrl { get; set; } = string.Empty;
        public string Method { get; set; } = "PUT";
        public System.Collections.Generic.Dictionary<string, string> Headers { get; set; } = new();
        public DateTime ExpiresAtUtc { get; set; }
    }
}
