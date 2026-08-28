using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ZansiHustle.Application.Catalog.External.Dtos;
using ZansiHustle.Application.Persistence.ExternalCatalog;
using ZansiHustle.Application.Persistence.Listings;
using ZansiHustle.Application.Persistence.Shops;
using ZansiHustle.Domain.ExternalCatalog;
using ZansiHustle.Domain.Listings;
using ZansiHustle.Shared.Enums.ExternalCatalog;
using ZansiHustle.Shared.Enums.Listings;
using ZansiHustle.Shared.Errors;
using ZansiHustle.Shared.Results;

namespace ZansiHustle.Application.Catalog.External
{
    /// <summary>
    /// Application service for the External Catalog Sync feature. Mirrors
    /// ExternalPayments (own registry, own shared-secret header, own
    /// unprefixed route) but authenticates with a SEPARATE secret namespace
    /// — catalog sync and payment authorization must never share a
    /// credential, even for the same external shop.
    ///
    /// Writes ONLY Listing/ListingVariant under the ListingSource.ShopProfile
    /// shape (see ListingService.CreateAsync's own ShopProfile invariant,
    /// which this mirrors: Listing.ShopProfileId set, Listing.MerchantId
    /// resolved from that ShopProfile). Never touches MarketplaceListing —
    /// a completely separate, unrelated entity.
    /// </summary>
    public sealed class ExternalCatalogSyncService : IExternalCatalogSyncService
    {
        private readonly IListingRepository _listingRepository;
        private readonly IShopProfileRepository _shopProfileRepository;
        private readonly IExternalCatalogRepository _catalogRepository;
        private readonly ExternalCatalogSourcesOptions _sources;
        private readonly ILogger<ExternalCatalogSyncService> _logger;

        public ExternalCatalogSyncService(
            IListingRepository listingRepository,
            IShopProfileRepository shopProfileRepository,
            IExternalCatalogRepository catalogRepository,
            IOptions<ExternalCatalogSourcesOptions> sources,
            ILogger<ExternalCatalogSyncService> logger)
        {
            _listingRepository = listingRepository;
            _shopProfileRepository = shopProfileRepository;
            _catalogRepository = catalogRepository;
            _sources = sources.Value ?? new ExternalCatalogSourcesOptions();
            _logger = logger;
        }

        // ─── Upsert ─────────────────────────────────────────────────────────

        public async Task<Result<ExternalProductUpsertResponseDto>> UpsertProductAsync(
            string sourceCode,
            string? providedSecret,
            ExternalProductUpsertRequestDto? request,
            Guid? syncRunId,
            CancellationToken cancellationToken = default)
        {
            var auth = Authenticate(sourceCode, providedSecret);
            if (!auth.Ok)
                return Result<ExternalProductUpsertResponseDto>.Failure(auth.Code!, auth.Message!);

            var source = auth.Source!;
            var sourceKey = auth.SourceKey;

            if (request is null)
                return Fail("Request body is required.");
            if (string.IsNullOrWhiteSpace(request.ExternalProductId))
                return Fail("externalProductId is required.");
            if (string.IsNullOrWhiteSpace(request.Title))
                return Fail("title is required.");
            if (request.BasePrice < 0)
                return Fail("basePrice cannot be negative.");
            if (!string.Equals(request.Currency, "ZAR", StringComparison.OrdinalIgnoreCase))
                return Fail("Only ZAR is supported.");
            if (request.AvailableQuantity is < 0)
                return Fail("availableQuantity cannot be negative.");

            // Variants are optional — omitted/null/empty means a simple
            // product (mirrored with zero ListingVariant rows). ZansiTech is
            // never required to manufacture a synthetic variant for a
            // single-SKU product.
            var incomingVariants = request.Variants ?? new List<ExternalProductVariantDto>();

            foreach (var v in incomingVariants)
            {
                if (string.IsNullOrWhiteSpace(v.ExternalVariantId))
                    return Fail("Every variant needs an externalVariantId.");
                if (string.IsNullOrWhiteSpace(v.Name))
                    return Fail($"Variant {v.ExternalVariantId} needs a name.");
                if (v.Price < 0)
                    return Fail($"Variant {v.ExternalVariantId} price cannot be negative.");
                if (v.AvailableQuantity < 0)
                    return Fail($"Variant {v.ExternalVariantId} availableQuantity cannot be negative.");
            }

            if (incomingVariants.Count > 0)
            {
                var dupVariant = incomingVariants.GroupBy(v => v.ExternalVariantId.Trim()).FirstOrDefault(g => g.Count() > 1);
                if (dupVariant is not null)
                    return Fail($"externalVariantId '{dupVariant.Key}' appears more than once in this payload.");
            }

            var status = ParseStatus(request.Status);
            if (status is null)
                return Fail($"Unrecognised status '{request.Status}'. Expected Active, Draft, or Archived.");

            ExternalCategoryMapping? categoryMapping = null;
            if (!string.IsNullOrWhiteSpace(request.Category?.Code))
            {
                categoryMapping = await _catalogRepository.GetCategoryMappingAsync(sourceKey, request.Category!.Code!.Trim());
                if (categoryMapping is null)
                    _logger.LogWarning(
                        "[ExternalCatalog][Upsert] no category mapping for source={Source} code={Code} — syncing with no category.",
                        sourceKey, request.Category.Code);
            }

            var externalProductId = request.ExternalProductId.Trim();
            var existing = await _listingRepository.GetByExternalIdAsync(sourceKey, externalProductId);

            // Stale-update protection: an incoming update no newer than what's
            // already stored is a safe no-op — never overwrite newer mirrored
            // data with a delayed/out-of-order call.
            if (existing is not null && existing.ExternalSourceUpdatedAtUtc is DateTime storedTs
                && request.SourceUpdatedAtUtc <= storedTs)
            {
                _logger.LogInformation(
                    "[ExternalCatalog][Upsert] stale update ignored source={Source} externalProductId={ExtId} incoming={Incoming} stored={Stored}.",
                    sourceKey, externalProductId, request.SourceUpdatedAtUtc, storedTs);

                var skipped = new ExternalProductUpsertResponseDto
                {
                    ListingId = existing.Id,
                    ExternalProductId = externalProductId,
                    Result = "SkippedStale",
                };
                foreach (var v in existing.Variants.Where(v => !string.IsNullOrEmpty(v.ExternalVariantId)))
                    skipped.VariantIds[v.ExternalVariantId!] = v.Id;

                await RecordActivityAsync(sourceKey, "Upsert", succeeded: true, error: null, incrementUpserted: false);
                return Result<ExternalProductUpsertResponseDto>.Success(skipped, "Stale update ignored — nothing newer to apply.");
            }

            // Re-resolved from the configured ShopProfile on EVERY sync (never
            // cached) — a misconfigured ShopProfileId is caught immediately
            // and loudly, rather than silently persisting a stale MerchantId.
            var shopProfile = await _shopProfileRepository.GetByIdAsync(source.ShopProfileId);
            if (shopProfile is null)
                return Fail(
                    $"Configured ShopProfileId {source.ShopProfileId} for source '{sourceKey}' does not exist. Fix ExternalCatalogs:{sourceKey}:ShopProfileId.",
                    ErrorCodes.ProviderNotConfigured);

            var isNew = existing is null;
            var now = DateTime.UtcNow;

            var listing = existing ?? new Listing
            {
                Id = Guid.NewGuid(),
                Code = GenerateListingCode(),
                Slug = await GenerateUniqueExternalSlugAsync(request.Title, request.Slug),
                Type = ListingType.Product,
                ExternalSourceCode = sourceKey,
                ExternalProductId = externalProductId,
                CreatedAtUtc = now,
            };

            listing.ListingSource = ListingSource.ShopProfile;
            listing.ShopProfileId = shopProfile.Id;
            listing.MerchantId = shopProfile.MerchantId;
            listing.Title = request.Title.Trim();
            listing.Description = request.Description?.Trim();
            listing.Price = request.BasePrice;
            listing.Currency = "ZAR";
            listing.Status = status.Value;
            listing.Images = request.Images?.Where(s => !string.IsNullOrWhiteSpace(s)).Select(s => s.Trim()).ToList() ?? new List<string>();
            // ZansiHustle's own Stock is a synchronized PROJECTION, never
            // authoritative — see the distributed-inventory recommendation.
            // Variant product: aggregate of variant quantities. Simple
            // product (no variants): the product-level AvailableQuantity
            // mirrors straight in, same nullable-means-untracked convention.
            listing.Stock = incomingVariants.Count > 0
                ? incomingVariants.Sum(v => v.AvailableQuantity)
                : request.AvailableQuantity;
            listing.ExternalSourceUpdatedAtUtc = request.SourceUpdatedAtUtc;
            listing.ExternalSyncedAtUtc = now;
            listing.ExternalSyncRunId = syncRunId;
            if (!isNew) listing.UpdatedAtUtc = now;

            if (categoryMapping is not null)
            {
                listing.SellerCategoryId = categoryMapping.SellerCategoryId;
                listing.SellerSubcategoryId = categoryMapping.SellerSubcategoryId;
            }

            // ── Variant stable-id upsert — NEVER wholesale-replace for external rows ──
            // New variants are staged separately from the existing-variant
            // mutations below (see the two-pass save further down): keeping
            // an update-in-place set and an insert set distinct is simpler
            // to reason about than one mixed update+insert SaveChanges call
            // against a graph loaded via Include.
            // With incomingVariants empty (simple product, or a variant
            // product that just became simple), incomingExternalIds is
            // empty too — the "absent from payload" pruning loop below then
            // matches EVERY previously-mirrored variant for this listing and
            // deactivates them all. That's exactly the variant→simple
            // transition: previously mirrored variants are safely
            // deactivated, never deleted.
            var responseVariantIds = new Dictionary<string, Guid>();
            var incomingExternalIds = new HashSet<string>(incomingVariants.Select(v => v.ExternalVariantId.Trim()));
            var brandNewVariants = new List<ListingVariant>();

            foreach (var v in incomingVariants)
            {
                var extVariantId = v.ExternalVariantId.Trim();
                var existingVariant = listing.Variants.FirstOrDefault(x =>
                    x.ExternalSourceCode == sourceKey && x.ExternalVariantId == extVariantId);

                // Deterministic price rule: a variant whose own price equals
                // the product's base price inherits it (UsesCustomPrice=false);
                // any other price is stored explicit. Never derived from Name.
                var usesCustomPrice = v.Price != listing.Price;

                if (existingVariant is not null)
                {
                    existingVariant.Name = v.Name.Trim();
                    existingVariant.Sku = string.IsNullOrWhiteSpace(v.Sku) ? null : v.Sku.Trim();
                    existingVariant.UsesCustomPrice = usesCustomPrice;
                    existingVariant.Price = usesCustomPrice ? v.Price : null;
                    existingVariant.Stock = v.AvailableQuantity;
                    existingVariant.IsActive = v.IsActive;
                    existingVariant.UpdatedAtUtc = now;
                    responseVariantIds[extVariantId] = existingVariant.Id;
                }
                else
                {
                    var newVariant = new ListingVariant
                    {
                        Id = Guid.NewGuid(),
                        ListingId = listing.Id,
                        Name = v.Name.Trim(),
                        Sku = string.IsNullOrWhiteSpace(v.Sku) ? null : v.Sku.Trim(),
                        UsesCustomPrice = usesCustomPrice,
                        Price = usesCustomPrice ? v.Price : null,
                        Stock = v.AvailableQuantity,
                        IsActive = v.IsActive,
                        SortOrder = listing.Variants.Count + brandNewVariants.Count,
                        ExternalSourceCode = sourceKey,
                        ExternalVariantId = extVariantId,
                        CreatedAtUtc = now,
                    };
                    brandNewVariants.Add(newVariant);
                    responseVariantIds[extVariantId] = newVariant.Id;
                }
            }

            // An existing externally-managed variant absent from this payload
            // was removed source-side — deactivate, never delete (historical
            // OrderItems keep their own snapshot regardless).
            foreach (var existingVariant in listing.Variants.Where(x =>
                         x.ExternalSourceCode == sourceKey
                         && x.ExternalVariantId is not null
                         && !incomingExternalIds.Contains(x.ExternalVariantId)))
            {
                existingVariant.IsActive = false;
                existingVariant.UpdatedAtUtc = now;
            }

            bool saved;
            try
            {
                if (isNew)
                {
                    // True create: nothing pre-existing to conflict with —
                    // attach everything to the graph and insert in one pass.
                    listing.Variants.AddRange(brandNewVariants);
                    await _listingRepository.AddAsync(listing);
                    saved = await _listingRepository.SaveChangesAndDetachAsync();
                }
                else
                {
                    // Update: save the listing's scalar changes + already-tracked
                    // existing-variant mutations first, THEN insert brand-new
                    // variants as a distinct second pass.
                    saved = await _listingRepository.SaveChangesAndDetachAsync();

                    if (brandNewVariants.Count > 0)
                    {
                        await _listingRepository.AddVariantsAsync(brandNewVariants);
                        var insertedNew = await _listingRepository.SaveChangesAndDetachAsync();
                        saved = saved || insertedNew;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[ExternalCatalog][Upsert] save failed source={Source} externalProductId={ExtId}.", sourceKey, externalProductId);
                await RecordActivityAsync(sourceKey, "Upsert", succeeded: false, error: ex.Message, incrementUpserted: false);
                return Fail($"Failed to save listing: {ex.Message}", ErrorCodes.Exception);
            }

            if (!saved && isNew)
                return Fail("Failed to create listing.", ErrorCodes.Exception);

            await RecordActivityAsync(sourceKey, "Upsert", succeeded: true, error: null, incrementUpserted: true);

            _logger.LogInformation(
                "[ExternalCatalog][Upsert] OK source={Source} externalProductId={ExtId} listingId={ListingId} variantCount={VariantCount} syncRunId={SyncRunId} result={Result}.",
                sourceKey, externalProductId, listing.Id, incomingVariants.Count, syncRunId, isNew ? "Created" : "Updated");

            return Result<ExternalProductUpsertResponseDto>.Success(new ExternalProductUpsertResponseDto
            {
                ListingId = listing.Id,
                ExternalProductId = externalProductId,
                Result = isNew ? "Created" : "Updated",
                VariantIds = responseVariantIds,
            }, isNew ? "Listing created." : "Listing updated.");
        }

        // ─── Archive ────────────────────────────────────────────────────────

        public async Task<Result> ArchiveProductAsync(
            string sourceCode, string? providedSecret, string externalProductId, CancellationToken cancellationToken = default)
        {
            var auth = Authenticate(sourceCode, providedSecret);
            if (!auth.Ok)
                return Result.Failure(auth.Code!, auth.Message!);

            var sourceKey = auth.SourceKey;

            if (string.IsNullOrWhiteSpace(externalProductId))
                return Result.Failure(ErrorCodes.BadRequest, "externalProductId is required.");

            var listing = await _listingRepository.GetByExternalIdAsync(sourceKey, externalProductId.Trim());
            if (listing is null)
                return Result.Failure(ErrorCodes.NotFound, "No mirrored listing found for this externalProductId.");

            listing.Status = ListingStatus.Archived;
            listing.UpdatedAtUtc = DateTime.UtcNow;
            foreach (var v in listing.Variants)
            {
                v.IsActive = false;
                v.UpdatedAtUtc = listing.UpdatedAtUtc;
            }

            _listingRepository.Update(listing);
            await _listingRepository.SaveChangesAsync();

            await RecordActivityAsync(sourceKey, "Archive", succeeded: true, error: null, incrementUpserted: false, incrementArchived: true, archivedCount: 1);

            _logger.LogInformation(
                "[ExternalCatalog][Archive] source={Source} externalProductId={ExtId} listingId={ListingId}.",
                sourceKey, externalProductId, listing.Id);

            return Result.Success("Listing archived.");
        }

        // ─── Full snapshot: start / batch / complete / fail ──────────────────

        public async Task<Result<ExternalCatalogSyncRunStartResponseDto>> StartFullSyncRunAsync(
            string sourceCode, string? providedSecret, CancellationToken cancellationToken = default)
        {
            var auth = Authenticate(sourceCode, providedSecret);
            if (!auth.Ok)
                return Result<ExternalCatalogSyncRunStartResponseDto>.Failure(auth.Code!, auth.Message!);

            var run = new ExternalCatalogSyncRun
            {
                Id = Guid.NewGuid(),
                SourceCode = auth.SourceKey,
                Status = ExternalCatalogSyncRunStatus.InProgress,
                StartedAtUtc = DateTime.UtcNow,
            };

            await _catalogRepository.AddSyncRunAsync(run);
            await _catalogRepository.SaveChangesAsync();

            _logger.LogInformation("[ExternalCatalog][FullSync] STARTED source={Source} syncRunId={RunId}.", auth.SourceKey, run.Id);

            return Result<ExternalCatalogSyncRunStartResponseDto>.Success(
                new ExternalCatalogSyncRunStartResponseDto { SyncRunId = run.Id }, "Sync run started.");
        }

        public async Task<Result<ExternalCatalogBatchUpsertResponseDto>> UpsertBatchAsync(
            string sourceCode, string? providedSecret, Guid syncRunId, ExternalCatalogBatchUpsertRequestDto? request,
            CancellationToken cancellationToken = default)
        {
            var auth = Authenticate(sourceCode, providedSecret);
            if (!auth.Ok)
                return Result<ExternalCatalogBatchUpsertResponseDto>.Failure(auth.Code!, auth.Message!);

            var sourceKey = auth.SourceKey;

            var run = await _catalogRepository.GetSyncRunByIdAsync(syncRunId);
            if (run is null || !string.Equals(run.SourceCode, sourceKey, StringComparison.Ordinal))
                return Result<ExternalCatalogBatchUpsertResponseDto>.Failure(ErrorCodes.NotFound, "Sync run not found for this source.");
            if (run.Status != ExternalCatalogSyncRunStatus.InProgress)
                return Result<ExternalCatalogBatchUpsertResponseDto>.Failure(ErrorCodes.Conflict, $"Sync run is already {run.Status}.");

            if (request?.Products is null || request.Products.Count == 0)
                return Result<ExternalCatalogBatchUpsertResponseDto>.Failure(ErrorCodes.BadRequest, "At least one product is required.");

            var response = new ExternalCatalogBatchUpsertResponseDto();
            foreach (var product in request.Products)
            {
                var result = await UpsertProductAsync(sourceCode, providedSecret, product, syncRunId, cancellationToken);
                if (result.IsSuccess)
                {
                    response.Succeeded++;
                    response.Results.Add(new ExternalCatalogBatchItemResultDto
                    {
                        ExternalProductId = product.ExternalProductId,
                        Success = true,
                        ListingId = result.Data!.ListingId,
                    });
                    run.ProductsUpserted++;
                    run.VariantsUpserted += product.Variants?.Count ?? 0;
                }
                else
                {
                    response.Failed++;
                    response.Results.Add(new ExternalCatalogBatchItemResultDto
                    {
                        ExternalProductId = product.ExternalProductId,
                        Success = false,
                        Error = result.Message,
                    });
                }
            }

            _catalogRepository.UpdateSyncRun(run);
            await _catalogRepository.SaveChangesAsync();

            await RecordActivityAsync(
                sourceKey, "FullSyncBatch",
                succeeded: response.Failed == 0,
                error: response.Failed > 0 ? $"{response.Failed} item(s) failed in batch." : null,
                incrementUpserted: false);

            return Result<ExternalCatalogBatchUpsertResponseDto>.Success(response, "Batch processed.");
        }

        public async Task<Result<ExternalCatalogSyncRunCompleteResponseDto>> CompleteFullSyncRunAsync(
            string sourceCode, string? providedSecret, Guid syncRunId, CancellationToken cancellationToken = default)
        {
            var auth = Authenticate(sourceCode, providedSecret);
            if (!auth.Ok)
                return Result<ExternalCatalogSyncRunCompleteResponseDto>.Failure(auth.Code!, auth.Message!);

            var sourceKey = auth.SourceKey;

            var run = await _catalogRepository.GetSyncRunByIdAsync(syncRunId);
            if (run is null || !string.Equals(run.SourceCode, sourceKey, StringComparison.Ordinal))
                return Result<ExternalCatalogSyncRunCompleteResponseDto>.Failure(ErrorCodes.NotFound, "Sync run not found for this source.");
            if (run.Status != ExternalCatalogSyncRunStatus.InProgress)
                return Result<ExternalCatalogSyncRunCompleteResponseDto>.Failure(ErrorCodes.Conflict, $"Sync run is already {run.Status}.");

            // The archive boundary — reachable ONLY from this explicit, successful
            // completion path. A failed or abandoned run never reaches this line,
            // so a dropped connection / partial batch can never be read as
            // "delete everything missing".
            var archivedCount = await _listingRepository.ArchiveUntouchedExternalListingsAsync(sourceKey, syncRunId);

            run.Status = ExternalCatalogSyncRunStatus.Completed;
            run.ProductsArchived = archivedCount;
            run.CompletedAtUtc = DateTime.UtcNow;
            _catalogRepository.UpdateSyncRun(run);
            await _catalogRepository.SaveChangesAsync();

            var status = await _catalogRepository.GetSourceStatusAsync(sourceKey);
            if (status is not null)
            {
                status.LastFullSyncRunId = run.Id;
                status.LastFullSyncCompletedAtUtc = run.CompletedAtUtc;
                status.TotalProductsArchived += archivedCount;
                status.UpdatedAtUtc = DateTime.UtcNow;
                _catalogRepository.UpdateSourceStatus(status);
                await _catalogRepository.SaveChangesAsync();
            }

            await RecordActivityAsync(sourceKey, "FullSyncComplete", succeeded: true, error: null, incrementUpserted: false);

            _logger.LogInformation(
                "[ExternalCatalog][FullSync] COMPLETE source={Source} syncRunId={RunId} upserted={Upserted} archived={Archived}.",
                sourceKey, syncRunId, run.ProductsUpserted, archivedCount);

            return Result<ExternalCatalogSyncRunCompleteResponseDto>.Success(new ExternalCatalogSyncRunCompleteResponseDto
            {
                SyncRunId = run.Id,
                ProductsUpserted = run.ProductsUpserted,
                ProductsArchived = archivedCount,
            }, "Sync run completed.");
        }

        public async Task<Result> FailFullSyncRunAsync(
            string sourceCode, string? providedSecret, Guid syncRunId, string? errorMessage, CancellationToken cancellationToken = default)
        {
            var auth = Authenticate(sourceCode, providedSecret);
            if (!auth.Ok)
                return Result.Failure(auth.Code!, auth.Message!);

            var sourceKey = auth.SourceKey;

            var run = await _catalogRepository.GetSyncRunByIdAsync(syncRunId);
            if (run is null || !string.Equals(run.SourceCode, sourceKey, StringComparison.Ordinal))
                return Result.Failure(ErrorCodes.NotFound, "Sync run not found for this source.");

            if (run.Status == ExternalCatalogSyncRunStatus.InProgress)
            {
                run.Status = ExternalCatalogSyncRunStatus.Failed;
                run.ErrorMessage = errorMessage;
                run.CompletedAtUtc = DateTime.UtcNow;
                _catalogRepository.UpdateSyncRun(run);
                await _catalogRepository.SaveChangesAsync();
            }
            // Idempotent no-op if already Failed/Completed. Never archives.

            await RecordActivityAsync(sourceKey, "FullSyncFailed", succeeded: false, error: errorMessage, incrementUpserted: false);

            _logger.LogWarning("[ExternalCatalog][FullSync] FAILED source={Source} syncRunId={RunId} error={Error}.", sourceKey, syncRunId, errorMessage);

            return Result.Success("Sync run marked failed. No listings were archived.");
        }

        // ─── Status ─────────────────────────────────────────────────────────

        public async Task<Result<ExternalCatalogStatusResponseDto>> GetStatusAsync(
            string sourceCode, string? providedSecret, CancellationToken cancellationToken = default)
        {
            var auth = Authenticate(sourceCode, providedSecret);
            if (!auth.Ok)
                return Result<ExternalCatalogStatusResponseDto>.Failure(auth.Code!, auth.Message!);

            var source = auth.Source!;
            var status = await _catalogRepository.GetSourceStatusAsync(auth.SourceKey);

            return Result<ExternalCatalogStatusResponseDto>.Success(new ExternalCatalogStatusResponseDto
            {
                Enabled = source.Enabled,
                LastActivityAtUtc = status?.LastActivityAtUtc,
                LastActivityType = status?.LastActivityType,
                LastActivitySucceeded = status?.LastActivitySucceeded ?? true,
                LastError = status?.LastError,
                TotalProductsUpserted = status?.TotalProductsUpserted ?? 0,
                TotalProductsArchived = status?.TotalProductsArchived ?? 0,
                LastFullSyncCompletedAtUtc = status?.LastFullSyncCompletedAtUtc,
            }, "OK");
        }

        // ─── Helpers ────────────────────────────────────────────────────────

        /// <summary>
        /// Pure observability bookkeeping — updates the per-source "last
        /// activity" row. Deliberately swallows its own failures: the
        /// product/variant upsert this is called after has already been
        /// validated and saved by the time we get here, and a hiccup
        /// updating a status counter must never turn an otherwise-successful
        /// sync into a failure the caller has to retry.
        /// </summary>
        private async Task RecordActivityAsync(
            string sourceKey, string activityType, bool succeeded, string? error,
            bool incrementUpserted, bool incrementArchived = false, int archivedCount = 0)
        {
            try
            {
                var status = await _catalogRepository.GetSourceStatusAsync(sourceKey);
                var now = DateTime.UtcNow;
                var isNew = status is null;

                status ??= new ExternalCatalogSourceStatus { Id = Guid.NewGuid(), SourceCode = sourceKey, CreatedAtUtc = now };

                status.LastActivityAtUtc = now;
                status.LastActivityType = activityType;
                status.LastActivitySucceeded = succeeded;
                status.LastError = succeeded ? null : error;
                if (incrementUpserted) status.TotalProductsUpserted += 1;
                if (incrementArchived) status.TotalProductsArchived += archivedCount;
                status.UpdatedAtUtc = now;

                // Never both — an entity added moments ago in this same call must
                // not also be passed to Update() (which would mismark a
                // not-yet-inserted row as an existing one to UPDATE).
                if (isNew)
                    await _catalogRepository.AddSourceStatusAsync(status);
                else
                    _catalogRepository.UpdateSourceStatus(status);

                await _catalogRepository.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[ExternalCatalog] failed to record activity source={Source} type={Type} — the underlying operation still succeeded.", sourceKey, activityType);
            }
        }

        private (bool Ok, ExternalCatalogSourceOptions? Source, string SourceKey, string? Code, string? Message) Authenticate(
            string sourceCode, string? providedSecret)
        {
            if (string.IsNullOrWhiteSpace(sourceCode))
                return (false, null, string.Empty, ErrorCodes.BadRequest, "sourceCode is required.");

            var key = sourceCode.Trim().ToLowerInvariant();

            if (string.IsNullOrWhiteSpace(providedSecret))
                return (false, null, key, ErrorCodes.Unauthorized, "Missing X-ZansiHustle-Catalog-Secret header.");

            if (!_sources.TryGetValue(key, out var source) || source is null)
            {
                _logger.LogWarning("[ExternalCatalog][Auth] unknown sourceCode={SourceCode}", sourceCode);
                return (false, null, key, ErrorCodes.Unauthorized, "Unknown catalog source.");
            }

            if (!ConstantTimeEquals(providedSecret, source.SharedSecret))
            {
                _logger.LogWarning("[ExternalCatalog][Auth] invalid shared secret for sourceCode={SourceCode}", key);
                return (false, null, key, ErrorCodes.Unauthorized, "Invalid shared secret.");
            }

            if (!source.Enabled)
            {
                _logger.LogWarning("[ExternalCatalog][Auth] source disabled sourceCode={SourceCode}", key);
                return (false, null, key, ErrorCodes.Forbidden, "This catalog source is not enabled.");
            }

            return (true, source, key, null, null);
        }

        private static Result<ExternalProductUpsertResponseDto> Fail(string message, string code = ErrorCodes.BadRequest) =>
            Result<ExternalProductUpsertResponseDto>.Failure(code, message);

        private static ListingStatus? ParseStatus(string? status) => (status ?? string.Empty).Trim().ToLowerInvariant() switch
        {
            "active" => ListingStatus.Active,
            "draft" => ListingStatus.Draft,
            "archived" => ListingStatus.Archived,
            "soldout" => ListingStatus.SoldOut,
            _ => null,
        };

        private static bool ConstantTimeEquals(string a, string b)
        {
            var aBytes = Encoding.UTF8.GetBytes(a ?? string.Empty);
            var bBytes = Encoding.UTF8.GetBytes(b ?? string.Empty);
            if (aBytes.Length != bBytes.Length) return false;
            return CryptographicOperations.FixedTimeEquals(aBytes, bBytes);
        }

        private static string GenerateListingCode() => $"LIS-EXT-{DateTime.UtcNow:yyyyMMddHHmmssfff}";

        private async Task<string> GenerateUniqueExternalSlugAsync(string title, string? preferredSlug)
        {
            var baseSlug = Slugify(!string.IsNullOrWhiteSpace(preferredSlug) ? preferredSlug! : title);
            if (string.IsNullOrEmpty(baseSlug)) baseSlug = "listing";

            var candidate = baseSlug;
            var suffix = 1;
            while (await _listingRepository.ExistsBySlugAsync(candidate))
            {
                candidate = $"{baseSlug}-{suffix}";
                suffix++;
            }
            return candidate;
        }

        private static string Slugify(string input)
        {
            var lowered = input.Trim().ToLowerInvariant();
            var sb = new StringBuilder();
            var lastWasDash = false;
            foreach (var c in lowered)
            {
                if (char.IsLetterOrDigit(c))
                {
                    sb.Append(c);
                    lastWasDash = false;
                }
                else if (!lastWasDash && sb.Length > 0)
                {
                    sb.Append('-');
                    lastWasDash = true;
                }
            }
            var result = sb.ToString().Trim('-');
            return result.Length > 200 ? result[..200] : result;
        }
    }
}
