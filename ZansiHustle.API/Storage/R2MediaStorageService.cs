using System;
using System.Net;
using System.Threading.Tasks;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ZansiHustle.Application.Media.Storage;

namespace ZansiHustle.API.Storage
{
    /// <summary>
    /// Cloudflare R2 storage adapter. R2 is S3-compatible, so this uses the
    /// standard AWS S3 SDK with ServiceURL pointed at the R2 endpoint and
    /// ForcePathStyle addressing.
    ///
    /// Container routing:
    ///   "private" → config "Storage:R2:PrivateBucket" (signed reads only)
    ///   "public"  → config "Storage:R2:PublicBucket"
    ///                    └── if "Storage:R2:PublicBaseUrl" is set, reads are
    ///                        a direct URL (CDN / pub-*.r2.dev); otherwise a
    ///                        short-TTL signed GET is returned.
    ///
    /// The MediaService itself is agnostic of the adapter — it writes
    /// "private"/"public" as a logical container name and this class resolves
    /// that to an actual bucket.
    ///
    /// Note on CORS: R2 bucket CORS policy must allow the portal origins for
    /// PUT + GET; the signed URL won't bypass browser preflight. Configure in
    /// the Cloudflare dashboard (one-time per bucket).
    /// </summary>
    public class R2MediaStorageService : IMediaStorageService
    {
        private readonly IAmazonS3 _s3;
        private readonly ILogger<R2MediaStorageService> _logger;
        private readonly string _privateBucket;
        private readonly string _publicBucket;
        private readonly string? _publicBaseUrl;

        public R2MediaStorageService(
            IConfiguration config,
            ILogger<R2MediaStorageService> logger)
        {
            _logger = logger;

            var endpoint  = config["Storage:R2:Endpoint"]       ?? throw new InvalidOperationException("Storage:R2:Endpoint is not configured.");
            var accessKey = config["Storage:R2:AccessKey"]      ?? throw new InvalidOperationException("Storage:R2:AccessKey is not configured.");
            var secretKey = config["Storage:R2:SecretKey"]      ?? throw new InvalidOperationException("Storage:R2:SecretKey is not configured.");
            _privateBucket = config["Storage:R2:PrivateBucket"] ?? throw new InvalidOperationException("Storage:R2:PrivateBucket is not configured.");
            _publicBucket  = config["Storage:R2:PublicBucket"]  ?? throw new InvalidOperationException("Storage:R2:PublicBucket is not configured.");
            _publicBaseUrl = config["Storage:R2:PublicBaseUrl"]?.TrimEnd('/');

            var s3Config = new AmazonS3Config
            {
                ServiceURL = endpoint,
                // R2 does not use AWS regions — path-style addressing is required.
                ForcePathStyle = true,
                // R2 accepts "auto" for region; the SDK still needs something
                // non-null to feed SigV4.
                AuthenticationRegion = "auto",
            };

            _s3 = new AmazonS3Client(new BasicAWSCredentials(accessKey, secretKey), s3Config);
            _logger.LogInformation(
                "[Media] R2 adapter initialised. endpoint={Endpoint} private={Private} public={Public} publicBaseUrl={PublicBaseUrl}",
                endpoint, _privateBucket, _publicBucket, _publicBaseUrl ?? "(signed)");
        }

        public Task<MediaUploadTicket> IssueUploadAsync(
            string container, string storageKey, string contentType, long maxSizeBytes, TimeSpan ttl)
        {
            var bucket = ResolveBucket(container);
            var expires = DateTime.UtcNow.Add(ttl);

            var req = new GetPreSignedUrlRequest
            {
                BucketName = bucket,
                Key = storageKey,
                Verb = HttpVerb.PUT,
                Expires = expires,
                ContentType = contentType,
            };

            var url = _s3.GetPreSignedURL(req);

            // The signer bakes Content-Type into the signature, so the client
            // MUST echo this exact header on the PUT. We deliberately do NOT
            // send any custom x-zh-* headers — R2 can't enforce them from a
            // presigned URL, and they'd force an extra CORS AllowedHeaders
            // entry for no benefit. Size enforcement already happens in
            // MediaService before the ticket is issued.
            var headers = new System.Collections.Generic.Dictionary<string, string>
            {
                ["Content-Type"] = contentType,
            };

            return Task.FromResult(new MediaUploadTicket
            {
                UploadUrl = url,
                Method = "PUT",
                Headers = headers,
                ExpiresAtUtc = expires,
            });
        }

        public Task<string> IssueReadUrlAsync(string container, string storageKey, TimeSpan ttl)
        {
            // Public bucket + configured public base URL → serve direct.
            // This is the CDN path for product/service imagery.
            if (string.Equals(container, "public", StringComparison.OrdinalIgnoreCase)
                && !string.IsNullOrWhiteSpace(_publicBaseUrl))
            {
                return Task.FromResult($"{_publicBaseUrl}/{storageKey}");
            }

            // Private or no public-base configured → signed GET URL.
            var bucket = ResolveBucket(container);
            var req = new GetPreSignedUrlRequest
            {
                BucketName = bucket,
                Key = storageKey,
                Verb = HttpVerb.GET,
                Expires = DateTime.UtcNow.Add(ttl),
            };
            return Task.FromResult(_s3.GetPreSignedURL(req));
        }

        public async Task<bool> ExistsAsync(string container, string storageKey)
        {
            var bucket = ResolveBucket(container);
            try
            {
                await _s3.GetObjectMetadataAsync(bucket, storageKey);
                return true;
            }
            catch (AmazonS3Exception ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                return false;
            }
        }

        public async Task DeleteAsync(string container, string storageKey)
        {
            var bucket = ResolveBucket(container);
            try
            {
                await _s3.DeleteObjectAsync(bucket, storageKey);
            }
            catch (Exception ex)
            {
                // R2 delete is idempotent — log but don't propagate, matching
                // the local-filesystem adapter's best-effort contract.
                _logger.LogWarning(ex, "[Media] R2 delete failed for {Bucket}/{Key}", bucket, storageKey);
            }
        }

        private string ResolveBucket(string container) => container switch
        {
            "private" => _privateBucket,
            "public"  => _publicBucket,
            _ => throw new ArgumentException($"Unknown container '{container}'. Expected 'private' or 'public'.", nameof(container)),
        };
    }
}
