using System;
using System.Threading.Tasks;
using ZansiHustle.Application.Media.Storage;

namespace ZansiHustle.API.Storage
{
    /// <summary>
    /// Production storage adapter — Azure Blob Storage.
    ///
    /// Stub for now: keeps the swap surface explicit so the production
    /// switch is "register this in DI instead of LocalFilesystem and fill
    /// in the four method bodies with Azure.Storage.Blobs SAS calls". The
    /// rest of the system never sees Azure-specific types.
    ///
    /// Implementation notes for whoever fills this in:
    ///  - Use `BlobServiceClient` per-environment (private + public containers).
    ///  - IssueUploadAsync → generate a User Delegation SAS with `Write|Create`
    ///    permissions, scoped to the single blob, TTL = ttl. Return headers
    ///    {"x-ms-blob-type":"BlockBlob","x-ms-blob-content-type":contentType}.
    ///  - IssueReadUrlAsync → public container returns the CDN URL directly;
    ///    private container returns a `Read`-only SAS.
    ///  - ExistsAsync → BlobClient.ExistsAsync.
    ///  - DeleteAsync → BlobClient.DeleteIfExistsAsync.
    /// </summary>
    public class AzureBlobMediaStorageService : IMediaStorageService
    {
        public Task<MediaUploadTicket> IssueUploadAsync(
            string container, string storageKey, string contentType, long maxSizeBytes, TimeSpan ttl)
            => throw new NotImplementedException("Azure Blob adapter not configured. Use LocalFilesystem in dev.");

        public Task<string> IssueReadUrlAsync(string container, string storageKey, TimeSpan ttl)
            => throw new NotImplementedException("Azure Blob adapter not configured. Use LocalFilesystem in dev.");

        public Task<bool> ExistsAsync(string container, string storageKey)
            => throw new NotImplementedException("Azure Blob adapter not configured. Use LocalFilesystem in dev.");

        public Task DeleteAsync(string container, string storageKey)
            => throw new NotImplementedException("Azure Blob adapter not configured. Use LocalFilesystem in dev.");
    }
}
