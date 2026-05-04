using System.Threading.Tasks;

namespace ZansiHustle.Application.Media.Storage
{
    /// <summary>
    /// Read-time URL refresh for stored asset URLs that may have been
    /// presigned with a short TTL at upload time. Implementations are
    /// expected to be:
    ///   • Idempotent — passing in a permanent CDN URL must return it
    ///     unchanged (no signing, no rewrite).
    ///   • Tolerant of foreign URLs — anything that isn't an R2 URL
    ///     passes through; the resolver never throws on bad input.
    ///   • Cheap — called per-asset on every read path (listing
    ///     gallery, shop logo, etc.). Implementations that hit the
    ///     storage backend on every call should keep that cheap or
    ///     introduce caching.
    /// </summary>
    public interface IStorageUrlResolver
    {
        /// <summary>
        /// If <paramref name="storedUrl"/> points at our R2 storage,
        /// extract the bucket+key and re-issue a fresh read URL with
        /// the appropriate TTL for that container. Otherwise return
        /// the input unchanged. Null/empty inputs return as-is.
        /// </summary>
        Task<string?> RefreshAsync(string? storedUrl);
    }
}
