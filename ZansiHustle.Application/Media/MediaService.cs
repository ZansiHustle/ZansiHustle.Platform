using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ZansiHustle.Application.Common.Interfaces.Shared;
using ZansiHustle.Application.Media.Dtos;
using ZansiHustle.Application.Media.Storage;
using ZansiHustle.Application.Persistence.Media;
using ZansiHustle.Domain.Media;
using ZansiHustle.Shared.Enums.Media;
using ZansiHustle.Shared.Errors;
using ZansiHustle.Shared.Results;

namespace ZansiHustle.Application.Media
{
    /// <summary>
    /// Thin orchestration layer between the storage adapter and the
    /// MediaAssets table. Responsible for:
    ///  - validating upload requests (size, MIME, purpose)
    ///  - reserving storage keys + Pending rows
    ///  - finalising uploads (Pending → Uploaded / PendingReview)
    ///  - re-parenting orphan uploads onto a freshly-created owner
    ///  - admin review (Approve / Reject)
    ///
    /// All authorization concerns (who can see what, who can review, who can
    /// re-parent) are enforced here using ICurrentUserService — callers
    /// (controllers / other services) never bypass.
    /// </summary>
    public class MediaService : IMediaService
    {
        private readonly IMediaAssetRepository _repo;
        private readonly IMediaStorageService _storage;
        private readonly ICurrentUserService _currentUser;
        private readonly ILogger<MediaService> _logger;

        // IMPORTANT: these MIME arrays MUST be declared BEFORE the Policies
        // dictionary. Static field initializers run in textual order, so
        // putting Policies first left AllowedMimes=null at the moment each
        // MediaPolicy was constructed — which then blew up as an
        // ArgumentNullException at the first request.
        private static readonly string[] ImagesOnly    = { "image/jpeg", "image/png", "image/webp", "image/gif" };
        private static readonly string[] ImagesOrPdf   = { "image/jpeg", "image/png", "image/webp", "application/pdf" };
        private static readonly string[] ImagesOrVideo = { "image/jpeg", "image/png", "image/webp", "video/mp4", "video/webm", "video/quicktime" };

        // Per-purpose policy: container, visibility, max bytes, allowed MIMEs,
        // and whether the asset enters admin review after upload.
        private static readonly Dictionary<MediaPurpose, MediaPolicy> Policies = new()
        {
            // ── Verification (Private + admin review) ─────────────────────
            [MediaPurpose.IdDocument]                = new("private", MediaVisibility.Private, 10_000_000, ImagesOrPdf, RequiresReview: true),
            [MediaPurpose.Portrait]                  = new("private", MediaVisibility.Private,  6_000_000, ImagesOnly, RequiresReview: true),
            [MediaPurpose.VerificationProductSample] = new("private", MediaVisibility.Private, 10_000_000, ImagesOrVideo, RequiresReview: true),
            [MediaPurpose.VerificationOther]         = new("private", MediaVisibility.Private, 10_000_000, ImagesOrPdf, RequiresReview: true),
            // Bank confirmation letter or statement — private, reviewable.
            // Re-uses ImagesOrPdf because banks commonly hand out both scans
            // and native PDFs. 10MB cap matches the rest of the verification
            // bucket.
            [MediaPurpose.BankProof]                 = new("private", MediaVisibility.Private, 10_000_000, ImagesOrPdf, RequiresReview: true),

            // ── Display (Public + no review) ──────────────────────────────
            [MediaPurpose.ProductGallery] = new("public", MediaVisibility.Public, 12_000_000, ImagesOrVideo, RequiresReview: false),
            [MediaPurpose.ProductHero]    = new("public", MediaVisibility.Public, 12_000_000, ImagesOnly,   RequiresReview: false),
            [MediaPurpose.ServiceGallery] = new("public", MediaVisibility.Public, 12_000_000, ImagesOrVideo, RequiresReview: false),
            [MediaPurpose.ServiceHero]    = new("public", MediaVisibility.Public, 12_000_000, ImagesOnly,   RequiresReview: false),
            [MediaPurpose.ShopLogo]       = new("public", MediaVisibility.Public,  4_000_000, ImagesOnly,   RequiresReview: false),
            [MediaPurpose.ShopBanner]     = new("public", MediaVisibility.Public,  6_000_000, ImagesOnly,   RequiresReview: false),
            [MediaPurpose.UserAvatar]     = new("public", MediaVisibility.Public,  4_000_000, ImagesOnly,   RequiresReview: false),
            // Public seller profile picture — goes live immediately (no review
            // gate), same public-bucket semantics as a logo. The selfie
            // (Portrait) stays private + reviewed; this is the buyer-facing face.
            [MediaPurpose.MerchantProfileImage] = new("public", MediaVisibility.Public, 4_000_000, ImagesOnly, RequiresReview: false),

            // Marketplace listing images. PUBLIC container is mandatory:
            // the listing renders the stored URL directly to buyers, so
            // a TTL'd private-signed URL would rot after upload. Resolves
            // via Storage:R2:PublicBaseUrl to a permanent CDN URL.
            //
            // Hard cap is 2MB — the mobile client optimises every photo
            // to roughly 1280px / quality 0.8 / under 800KB before
            // upload, with 2MB as the abort threshold. The backend
            // enforces the same number defensively so a non-optimising
            // client (or a tampered request) can't fill R2 with raw
            // 12MB phone photos.
            [MediaPurpose.MarketplaceListingImage] = new("public", MediaVisibility.Public, 2_000_000, ImagesOnly, RequiresReview: false),

            // ── Physical-store verification ──────────────────────────────
            // All four share the verification bucket's semantics:
            // Private container, admin review required, photo-or-PDF
            // for the documents and image-only for the photos. Sized
            // generously (8MB photos, 10MB documents) so good-quality
            // captures from a mid-range Android phone don't clip.
            [MediaPurpose.StorefrontPhoto]              = new("private", MediaVisibility.Private,  8_000_000, ImagesOnly,  RequiresReview: true),
            [MediaPurpose.StoreInteriorPhoto]           = new("private", MediaVisibility.Private,  8_000_000, ImagesOnly,  RequiresReview: true),
            [MediaPurpose.StoreShelvesPhoto]            = new("private", MediaVisibility.Private,  8_000_000, ImagesOnly,  RequiresReview: true),
            [MediaPurpose.BusinessRegistrationDocument] = new("private", MediaVisibility.Private, 10_000_000, ImagesOrPdf, RequiresReview: true),
            [MediaPurpose.BusinessLicenseDocument]      = new("private", MediaVisibility.Private, 10_000_000, ImagesOrPdf, RequiresReview: true),

            [MediaPurpose.Other]          = new("private", MediaVisibility.Private, 10_000_000, ImagesOrPdf, RequiresReview: false),
        };

        public MediaService(
            IMediaAssetRepository repo,
            IMediaStorageService storage,
            ICurrentUserService currentUser,
            ILogger<MediaService> logger)
        {
            _repo = repo;
            _storage = storage;
            _currentUser = currentUser;
            _logger = logger;
        }

        public async Task<Result<IssueUploadResponseDto>> IssueUploadAsync(IssueUploadRequestDto request)
        {
            // Validation returns Failure (never throws) so bad payloads
            // become 400 instead of opaque 500s.
            if (request is null)
                return Result<IssueUploadResponseDto>.Failure(ErrorCodes.BadRequest, "Request is required.");
            if (!_currentUser.UserId.HasValue)
                return Result<IssueUploadResponseDto>.Failure(ErrorCodes.Unauthorized, "Authenticated user required.");
            if (string.IsNullOrWhiteSpace(request.FileName))
                return Result<IssueUploadResponseDto>.Failure(ErrorCodes.BadRequest, "File name is required.");
            if (string.IsNullOrWhiteSpace(request.ContentType))
                return Result<IssueUploadResponseDto>.Failure(ErrorCodes.BadRequest, "Content type is required.");
            if (request.FileSizeBytes <= 0)
                return Result<IssueUploadResponseDto>.Failure(ErrorCodes.BadRequest, "File size must be positive.");

            if (!Policies.TryGetValue(request.Purpose, out var policy))
                return Result<IssueUploadResponseDto>.Failure(ErrorCodes.BadRequest, "Unknown purpose.");

            if (request.FileSizeBytes > policy.MaxBytes)
                return Result<IssueUploadResponseDto>.Failure(ErrorCodes.BadRequest,
                    $"File too large. Max for this purpose is {policy.MaxBytes / 1_000_000}MB.");

            var contentType = request.ContentType.ToLowerInvariant().Trim();
            // Belt-and-braces: if AllowedMimes somehow ends up null (static
            // init regression, missing policy entry rehydrated from
            // elsewhere), treat it as a clean 400 instead of throwing NRE.
            if (policy.AllowedMimes is null || !policy.AllowedMimes.Contains(contentType))
                return Result<IssueUploadResponseDto>.Failure(ErrorCodes.BadRequest,
                    $"Content type '{request.ContentType}' is not allowed for this purpose.");

            // Storage adapter + DB save can throw at runtime (permissions,
            // disk full, DB constraint). Without this try/catch the endpoint
            // returned an opaque 500 on UAT with no diagnostic — wrap so
            // clients see a clean message AND the exception hits the log.
            try
            {
                var assetId = Guid.NewGuid();
                var ext = SafeExtensionFor(request.FileName, contentType);
                // Storage key layout: {ownerType}/{ownerOrUserId}/{purpose}/{assetId}{ext}
                // Keeps a flat prefix scan per-owner cheap; assetId guarantees uniqueness.
                var ownerSegment = request.OwnerEntityId?.ToString("n") ?? $"u-{_currentUser.UserId.Value:n}";
                var storageKey = $"{(int)request.OwnerEntityType}/{ownerSegment}/{(int)request.Purpose}/{assetId:n}{ext}";

                var ticket = await _storage.IssueUploadAsync(
                    policy.Container, storageKey, contentType, policy.MaxBytes, TimeSpan.FromMinutes(15));

                var entity = new MediaAsset
                {
                    Id = assetId,
                    OwnerEntityType = request.OwnerEntityType,
                    OwnerEntityId = request.OwnerEntityId,
                    UploadedByUserId = _currentUser.UserId.Value,
                    Kind = request.Kind,
                    Purpose = request.Purpose,
                    Visibility = policy.Visibility,
                    Status = MediaStatus.Pending,
                    StorageContainer = policy.Container,
                    StorageKey = storageKey,
                    FileName = request.FileName,
                    ContentType = contentType,
                    FileSizeBytes = request.FileSizeBytes,
                    CreatedAtUtc = DateTime.UtcNow,
                };
                await _repo.AddAsync(entity);
                await _repo.SaveChangesAsync();

                return Result<IssueUploadResponseDto>.Success(new IssueUploadResponseDto
                {
                    MediaAssetId = assetId,
                    UploadUrl = ticket.UploadUrl,
                    Method = ticket.Method,
                    Headers = ticket.Headers,
                    ExpiresAtUtc = ticket.ExpiresAtUtc,
                }, "Upload ticket issued.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "[Media] IssueUploadAsync failed for user {UserId} purpose {Purpose}",
                    _currentUser.UserId, request.Purpose);
                return Result<IssueUploadResponseDto>.Failure(ErrorCodes.Exception,
                    $"Failed to issue upload ticket: {ex.Message}");
            }
        }

        public async Task<Result<MediaAssetDto>> FinalizeAsync(Guid id)
        {
            try
            {
                var asset = await _repo.GetByIdAsync(id);
                if (asset is null)
                    return Result<MediaAssetDto>.Failure(ErrorCodes.NotFound, "Media asset not found.");

                if (!_currentUser.UserId.HasValue || asset.UploadedByUserId != _currentUser.UserId.Value)
                    return Result<MediaAssetDto>.Failure(ErrorCodes.Forbidden, "You did not upload this asset.");

                var exists = await _storage.ExistsAsync(asset.StorageContainer, asset.StorageKey);
                if (!exists)
                    return Result<MediaAssetDto>.Failure(ErrorCodes.BadRequest, "Blob not found in storage. Did the upload complete?");

                var policy = Policies.GetValueOrDefault(asset.Purpose);
                asset.Status = (policy?.RequiresReview ?? false) ? MediaStatus.PendingReview : MediaStatus.Uploaded;
                asset.UploadedAtUtc = DateTime.UtcNow;
                asset.UpdatedAtUtc = DateTime.UtcNow;
                _repo.Update(asset);
                await _repo.SaveChangesAsync();

                return Result<MediaAssetDto>.Success(await ToDtoAsync(asset), "Finalized.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Media] FinalizeAsync failed for {Id}", id);
                return Result<MediaAssetDto>.Failure(ErrorCodes.Exception, $"Failed to finalize media: {ex.Message}");
            }
        }

        public async Task<Result<MediaAssetDto>> GetAsync(Guid id)
        {
            try
            {
                var asset = await _repo.GetByIdAsync(id);
                if (asset is null)
                    return Result<MediaAssetDto>.Failure(ErrorCodes.NotFound, "Media asset not found.");
                return Result<MediaAssetDto>.Success(await ToDtoAsync(asset), "OK");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Media] GetAsync failed for {Id}", id);
                return Result<MediaAssetDto>.Failure(ErrorCodes.Exception, $"Failed to load media: {ex.Message}");
            }
        }

        public async Task<Result<List<MediaAssetDto>>> GetByOwnerAsync(OwnerEntityType ownerType, Guid ownerId)
        {
            try
            {
                var assets = await _repo.GetByOwnerAsync(ownerType, ownerId);
                var dtos = new List<MediaAssetDto>(assets.Count);
                foreach (var a in assets) dtos.Add(await ToDtoAsync(a));
                return Result<List<MediaAssetDto>>.Success(dtos, "OK");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Media] GetByOwnerAsync failed for {OwnerType}/{OwnerId}", ownerType, ownerId);
                return Result<List<MediaAssetDto>>.Failure(ErrorCodes.Exception, $"Failed to load media list: {ex.Message}");
            }
        }

        public async Task<Result<MediaAssetDto>> ReviewAsync(Guid id, ReviewMediaRequestDto request)
        {
            if (request is null)
                return Result<MediaAssetDto>.Failure(ErrorCodes.BadRequest, "Request is required.");
            if (!_currentUser.UserId.HasValue)
                return Result<MediaAssetDto>.Failure(ErrorCodes.Unauthorized, "Authenticated user required.");

            try
            {
                var asset = await _repo.GetByIdAsync(id);
                if (asset is null)
                    return Result<MediaAssetDto>.Failure(ErrorCodes.NotFound, "Media asset not found.");

                asset.Status = request.Approved ? MediaStatus.Approved : MediaStatus.Rejected;
                asset.ReviewedByUserId = _currentUser.UserId.Value;
                asset.ReviewedAtUtc = DateTime.UtcNow;
                asset.RejectionReason = request.Approved ? null : request.RejectionReason?.Trim();
                asset.UpdatedAtUtc = DateTime.UtcNow;
                _repo.Update(asset);
                await _repo.SaveChangesAsync();

                return Result<MediaAssetDto>.Success(await ToDtoAsync(asset), request.Approved ? "Approved." : "Rejected.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Media] ReviewAsync failed for {Id}", id);
                return Result<MediaAssetDto>.Failure(ErrorCodes.Exception, $"Failed to review media: {ex.Message}");
            }
        }

        public async Task<Result> AttachToOwnerAsync(
            IEnumerable<Guid> mediaAssetIds, OwnerEntityType ownerType, Guid ownerId)
        {
            if (!_currentUser.UserId.HasValue)
                return Result.Failure(ErrorCodes.Unauthorized, "Authenticated user required.");

            try
            {
                var orphans = await _repo.GetOrphansForUserAsync(_currentUser.UserId.Value, mediaAssetIds);
                foreach (var asset in orphans)
                {
                    asset.OwnerEntityType = ownerType;
                    asset.OwnerEntityId = ownerId;
                    asset.UpdatedAtUtc = DateTime.UtcNow;
                    _repo.Update(asset);
                }
                if (orphans.Count > 0) await _repo.SaveChangesAsync();
                return Result.Success("Attached.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Media] AttachToOwnerAsync failed for {OwnerType}/{OwnerId}", ownerType, ownerId);
                return Result.Failure(ErrorCodes.Exception, $"Failed to attach media: {ex.Message}");
            }
        }

        // ── Helpers ───────────────────────────────────────────────────────
        private async Task<MediaAssetDto> ToDtoAsync(MediaAsset a)
        {
            string? readUrl = null;
            // Only resolve a read URL once the blob exists. Avoid signing
            // URLs to assets that haven't finished uploading yet.
            //
            // TTL choice: public-container assets (shop logo, shop banner,
            // product imagery) are often persisted as URLs on other records
            // (Merchant.LogoUrl, Listing image arrays), so a 15-minute
            // signed URL rots the moment the user navigates away. Use
            // 7 days — the SigV4 maximum — for public content. Private
            // verification/KYC media stays at 15 minutes because admin
            // review happens inside a single session and we don't want
            // long-lived links to ID documents.
            if (a.Status != MediaStatus.Pending)
            {
                var ttl = string.Equals(a.StorageContainer, "public", StringComparison.OrdinalIgnoreCase)
                    ? TimeSpan.FromDays(7)
                    : TimeSpan.FromMinutes(15);
                readUrl = await _storage.IssueReadUrlAsync(a.StorageContainer, a.StorageKey, ttl);
            }
            return new MediaAssetDto
            {
                Id = a.Id,
                OwnerEntityType = a.OwnerEntityType,
                OwnerEntityId = a.OwnerEntityId,
                UploadedByUserId = a.UploadedByUserId,
                Kind = a.Kind,
                Purpose = a.Purpose,
                Visibility = a.Visibility,
                Status = a.Status,
                FileName = a.FileName,
                ContentType = a.ContentType,
                FileSizeBytes = a.FileSizeBytes,
                DurationSeconds = a.DurationSeconds,
                SortOrder = a.SortOrder,
                ReadUrl = readUrl,
                RejectionReason = a.RejectionReason,
                UploadedAtUtc = a.UploadedAtUtc,
                ReviewedAtUtc = a.ReviewedAtUtc,
                CreatedAtUtc = a.CreatedAtUtc,
            };
        }

        private static string SafeExtensionFor(string fileName, string contentType)
        {
            var ext = System.IO.Path.GetExtension(fileName);
            if (!string.IsNullOrWhiteSpace(ext) && ext.Length <= 8) return ext.ToLowerInvariant();
            return contentType switch
            {
                "image/jpeg" => ".jpg",
                "image/png" => ".png",
                "image/webp" => ".webp",
                "image/gif" => ".gif",
                "video/mp4" => ".mp4",
                "video/webm" => ".webm",
                "video/quicktime" => ".mov",
                "application/pdf" => ".pdf",
                _ => ".bin",
            };
        }

        private record MediaPolicy(
            string Container,
            MediaVisibility Visibility,
            long MaxBytes,
            string[] AllowedMimes,
            bool RequiresReview);
    }
}
