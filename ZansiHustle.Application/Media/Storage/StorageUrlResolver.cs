using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ZansiHustle.Application.Media.Storage
{
    /// <summary>
    /// Default <see cref="IStorageUrlResolver"/> implementation.
    ///
    /// Recovers stored URLs to a usable read URL on every map. There
    /// are TWO distinct stored shapes in the wild:
    ///
    ///   1. <c>https://pub-{hash}.r2.dev/{key}</c> (and any future
    ///      custom CDN domain) — bucket is bound to the hostname; the
    ///      path is the storage key directly, no bucket prefix. This
    ///      is the stable form; it must pass through unchanged.
    ///
    ///   2. <c>https://{accountid}.r2.cloudflarestorage.com/{bucket}/{key}?X-Amz-...</c>
    ///      — bucket is in the path. These are legacy presigned URLs
    ///      written before <c>Storage:R2:PublicBaseUrl</c> was set; the
    ///      signature ages out (15 min private / 7 days public) and the
    ///      stored URL goes 403. We re-derive bucket+key and ask the
    ///      storage adapter for a fresh URL — for public buckets that
    ///      adapter now returns the stable <c>{publicBaseUrl}/{key}</c>
    ///      form, so legacy rows recover automatically on read.
    ///
    /// Past bug: the hostname guard included <c>r2.dev</c>, which
    /// pulled stable public-CDN URLs into the cloudflarestorage.com
    /// parser. Their path <c>/{key}</c> has no bucket prefix, so the
    /// first key segment ("5", from the OwnerEntityType int) was being
    /// classified as the bucket name, mapped to "private", and re-
    /// signed against <c>zansihustle-private</c> — pointing at an
    /// object that lives in <c>zansihustle-public</c>. The early-
    /// return below for PublicBaseUrl-prefixed URLs prevents the
    /// happy-path case from ever entering the parser.
    /// </summary>
    public sealed class StorageUrlResolver : IStorageUrlResolver
    {
        private readonly IMediaStorageService _storage;
        private readonly ILogger<StorageUrlResolver> _logger;
        private readonly string? _publicBaseUrl;

        public StorageUrlResolver(
            IMediaStorageService storage,
            IConfiguration config,
            ILogger<StorageUrlResolver> logger)
        {
            _storage = storage;
            _logger = logger;
            // Trailing-slash-stripped to match R2MediaStorageService —
            // both consumers must agree on shape so a `StartsWith` check
            // against either form works.
            _publicBaseUrl = config["Storage:R2:PublicBaseUrl"]?.TrimEnd('/');
        }

        public async Task<string?> RefreshAsync(string? storedUrl)
        {
            if (string.IsNullOrWhiteSpace(storedUrl)) return storedUrl;

            // Stable public CDN URL — pass straight through. This covers:
            //   • new uploads written as `{publicBaseUrl}/{key}`
            //   • legacy rows that were already rebased on a previous read
            //   • any future custom domain set via PublicBaseUrl
            // Must run BEFORE the cloudflarestorage.com parser; the
            // r2.dev / custom-CDN shape has `/{key}` (no bucket prefix)
            // which the parser would misclassify.
            if (!string.IsNullOrWhiteSpace(_publicBaseUrl) &&
                storedUrl.StartsWith(_publicBaseUrl!, StringComparison.OrdinalIgnoreCase))
            {
                return storedUrl;
            }

            // ONLY presigned `cloudflarestorage.com` URLs need re-signing.
            // The path on those is always `/{bucket}/{key}` — the parser
            // below relies on that. Anything else (external CDNs, third-
            // party hosts, the stable r2.dev form) must NOT enter the
            // parser.
            if (!storedUrl.Contains("r2.cloudflarestorage.com", StringComparison.OrdinalIgnoreCase))
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
