using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace ZansiHustle.Application.Media.Storage
{
    /// <summary>
    /// Default <see cref="IStorageUrlResolver"/> implementation.
    ///
    /// Lifted from the previous private helper in
    /// <c>MerchantService.RefreshStoredUrlAsync</c> so merchant and
    /// marketplace share one canonical recovery path for stored URLs.
    /// The original behaviour is preserved verbatim — only the call
    /// site moved.
    ///
    /// Mechanics: when an asset is uploaded into R2 without a
    /// configured <c>Storage:R2:PublicBaseUrl</c>, FinalizeAsync persists
    /// a presigned URL. The signature ages out (15 min for private,
    /// 7 days for public) and the row's URL goes 403. We can't fix the
    /// stored value without a migration, so we re-derive bucket+key
    /// from the URL path on every read and ask the storage adapter for
    /// a fresh signed URL.
    ///
    /// Idempotent for permanent CDN URLs: <c>Storage:R2:PublicBaseUrl</c>
    /// configured → stored URL is <c>{cdn}/{key}</c>, not an R2 hostname,
    /// so the early-return at the top kicks in.
    /// </summary>
    public sealed class StorageUrlResolver : IStorageUrlResolver
    {
        private readonly IMediaStorageService _storage;
        private readonly ILogger<StorageUrlResolver> _logger;

        public StorageUrlResolver(IMediaStorageService storage, ILogger<StorageUrlResolver> logger)
        {
            _storage = storage;
            _logger = logger;
        }

        public async Task<string?> RefreshAsync(string? storedUrl)
        {
            if (string.IsNullOrWhiteSpace(storedUrl)) return storedUrl;

            // Anything that isn't an R2 hostname passes through. This is
            // the path permanent public CDN URLs and external/legacy
            // images take — we MUST NOT rewrite them.
            if (!storedUrl.Contains("r2.cloudflarestorage.com", StringComparison.OrdinalIgnoreCase)
                && !storedUrl.Contains("r2.dev", StringComparison.OrdinalIgnoreCase))
                return storedUrl;

            try
            {
                var uri = new Uri(storedUrl);
                var path = uri.AbsolutePath.TrimStart('/');
                var firstSlash = path.IndexOf('/');
                if (firstSlash < 0) return storedUrl;

                var bucket = path[..firstSlash];
                var key    = path[(firstSlash + 1)..];

                // Map the actual R2 bucket name back to the logical
                // container MediaService uses. Anything containing
                // "public" in the name maps to the public container;
                // everything else is treated as private (a conservative
                // default — a typo'd bucket name shouldn't leak a long
                // public URL to a private asset).
                var container = bucket.Contains("public", StringComparison.OrdinalIgnoreCase)
                    ? "public"
                    : "private";

                var ttl = container == "public"
                    ? TimeSpan.FromDays(7)
                    : TimeSpan.FromMinutes(15);

                var refreshed = await _storage.IssueReadUrlAsync(container, key, ttl);
                _logger.LogDebug(
                    "[StorageUrlResolver] refreshed R2 URL bucket={Bucket} container={Container}",
                    bucket, container);
                return refreshed;
            }
            catch (Exception ex)
            {
                // Malformed URL (or a transient storage hiccup) — pass
                // the original through so the caller still has SOMETHING
                // to render. The image may 403 in the browser but that's
                // strictly no worse than the current state.
                _logger.LogWarning(ex,
                    "[StorageUrlResolver] failed to refresh stored URL; passing through");
                return storedUrl;
            }
        }
    }
}
