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
    /// are THREE distinct stored shapes in the wild:
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
    ///   3. <c>{host}/api/media/raw/{container}/{key}?sig=…&amp;exp=…&amp;verb=GET</c>
    ///      — LocalFilesystem-adapter signed URLs. The signature embeds
    ///      both the verb and the expiry; once <c>exp</c> passes, the
    ///      controller refuses the read. We extract container+key,
    ///      <see cref="Uri.UnescapeDataString"/> the key, and re-issue
    ///      a fresh signed URL via the current <see cref="IMediaStorageService"/>
    ///      using the current request host. This makes dev / UAT with
    ///      LocalFilesystem self-healing across signing-key rotations,
    ///      host changes (laptop ↔ LAN device), and TTL expiry.
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

        // Marker used to detect LocalFilesystem-adapter signed URLs.
        // Kept in lockstep with LocalFilesystemMediaStorageService.BuildSignedUrl.
        private const string LocalRawMarker = "/api/media/raw/";

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

            // Branch 1 — Stable public CDN URL — pass straight through.
            // This covers:
            //   • new uploads written as `{publicBaseUrl}/{key}`
            //   • legacy rows that were already rebased on a previous read
            //   • any future custom domain set via PublicBaseUrl
            // Must run BEFORE the cloudflarestorage.com parser; the
            // r2.dev / custom-CDN shape has `/{key}` (no bucket prefix)
            // which the parser would misclassify.
            if (!string.IsNullOrWhiteSpace(_publicBaseUrl) &&
                storedUrl.StartsWith(_publicBaseUrl!, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogDebug(
                    "[StorageUrlResolver] branch=public-base-passthrough stored={Stored}",
                    storedUrl);
                return storedUrl;
            }

            // Branch 2 — presigned `cloudflarestorage.com` URLs.
            // The path on those is always `/{bucket}/{key}` — the parser
            // below relies on that.
            if (storedUrl.Contains("r2.cloudflarestorage.com", StringComparison.OrdinalIgnoreCase))
            {
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
                        "[StorageUrlResolver] branch=r2-refresh bucket={Bucket} container={Container} stored={Stored} resolved={Resolved}",
                        bucket, container, storedUrl, refreshed);
                    return refreshed;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex,
                        "[StorageUrlResolver] branch=r2-refresh-failed; passing through stored={Stored}",
                        storedUrl);
                    return storedUrl;
                }
            }

            // Branch 3 — LocalFilesystem-adapter signed URL.
            // Shape: `{scheme}://{host}/api/media/raw/{container}/{escapedKey}?sig=…&exp=…&verb=GET`
            // We don't require a host match — the URL might have been
            // issued from a previous environment / earlier session.
            // `IssueReadUrlAsync` produces a fresh URL using the
            // CURRENT request host (LocalFilesystemMediaStorageService.ResolveHost).
            if (storedUrl.Contains(LocalRawMarker, StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    var uri = new Uri(storedUrl);
                    var path = uri.AbsolutePath; // e.g. /api/media/raw/public/abcd
                    var ix = path.IndexOf(LocalRawMarker, StringComparison.OrdinalIgnoreCase);
                    if (ix < 0) return storedUrl;

                    var rest = path[(ix + LocalRawMarker.Length)..];
                    var firstSlash = rest.IndexOf('/');
                    if (firstSlash < 0) return storedUrl;

                    var container = rest[..firstSlash];
                    var encodedKey = rest[(firstSlash + 1)..];
                    // BuildSignedUrl uses Uri.EscapeDataString on the key;
                    // reverse that here. UnescapeDataString is also a
                    // no-op when the key contains no escaped chars.
                    var key = Uri.UnescapeDataString(encodedKey);

                    var ttl = container.Equals("public", StringComparison.OrdinalIgnoreCase)
                        ? TimeSpan.FromDays(7)
                        : TimeSpan.FromMinutes(15);

                    var refreshed = await _storage.IssueReadUrlAsync(container, key, ttl);
                    _logger.LogDebug(
                        "[StorageUrlResolver] branch=local-raw-refresh container={Container} stored={Stored} resolved={Resolved}",
                        container, storedUrl, refreshed);
                    return refreshed;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex,
                        "[StorageUrlResolver] branch=local-raw-refresh-failed; passing through stored={Stored}",
                        storedUrl);
                    return storedUrl;
                }
            }

            // Branch 4 — anything else (third-party CDN, external host,
            // malformed string). Pass through; never throw on bad input.
            _logger.LogDebug(
                "[StorageUrlResolver] branch=passthrough stored={Stored}",
                storedUrl);
            return storedUrl;
        }
    }
}
