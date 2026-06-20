using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ZansiHustle.Application.ZansiPulse;
using ZansiHustle.Application.ZansiPulse.Dtos;
using ZansiHustle.Domain.ZansiPulse;
using ZansiHustle.Infrastructure.Data;
using ZansiHustle.Shared.Enums.Listings;
using ZansiHustle.Shared.Enums.Shops;
using ZansiHustle.Shared.Enums.ZansiPulse;
using ZansiHustle.Shared.Errors;
using ZansiHustle.Shared.Results;

namespace ZansiHustle.Infrastructure.ZansiPulse
{
    /// <summary>
    /// ZansiPulse intelligence-layer service (Phase 1). Talks to
    /// <see cref="AppDbContext"/> directly — the same in-Infrastructure
    /// pattern used by ChatService / AgentPayoutService — so it can run the
    /// cross-entity joins recommendations and trending need without a wide
    /// repository surface.
    ///
    /// Everything here is deterministic: scores are pure functions of stored
    /// data and DB-tunable weights. No randomness, no external AI. The method
    /// surface is shaped so a model can replace the scoring internals later
    /// without changing callers.
    /// </summary>
    public sealed class ZansiPulseService : IZansiPulseService
    {
        private readonly AppDbContext _db;
        private readonly ILogger<ZansiPulseService> _logger;

        // Saturation half-points for asymptotic 0..1 normalisation.
        private const decimal ListingEngagementHalf = 200m;
        private const decimal SellerPopularityHalf = 200m;
        private const decimal ShopPopularityHalf = 200m;
        private const double RecencyHalfLifeDays = 7d;

        // Bound on how many recent events an aggregate read (trending
        // categories/regions, dashboard, snapshot) will pull into memory.
        // Keeps those queries predictable and UAT/production-safe on a growing
        // event table — Phase 1 volumes sit well under this, and the
        // most-recent ordering means we still aggregate the freshest signal if
        // the cap is ever hit.
        // TODO(scale): when this cap is regularly reached, replace the
        // in-memory aggregation with incremental materialised rollups — the
        // ZansiPulseCategoryMetric / ZansiPulseRegionMetric daily buckets are
        // already maintained on track for exactly this purpose, so the
        // dashboard/trending reads can switch to summing those buckets instead
        // of scanning raw events.
        private const int MaxScanEvents = 50_000;

        // Candidate pool sizes for recommendation scoring.
        private const int ListingCandidatePool = 500;
        private const int ShopCandidatePool = 300;

        public ZansiPulseService(AppDbContext db, ILogger<ZansiPulseService> logger)
        {
            _db = db;
            _logger = logger;
        }

        // ════════════════════════════════════════════════════════════════════
        // Event tracking
        // ════════════════════════════════════════════════════════════════════

        public async Task<Result<Guid>> TrackEventAsync(Guid? userId, TrackEventRequestDto request, CancellationToken ct = default)
        {
            try
            {
                if (request is null || string.IsNullOrWhiteSpace(request.EventType))
                    return Result<Guid>.Failure(ErrorCodes.BadRequest, "EventType is required.");

                if (!Enum.TryParse<ZansiPulseEventType>(request.EventType.Trim(), ignoreCase: true, out var eventType)
                    || !Enum.IsDefined(typeof(ZansiPulseEventType), eventType))
                    return Result<Guid>.Failure(ErrorCodes.BadRequest, $"Unknown EventType '{request.EventType}'.");

                var settings = await LoadSettingsAsync(ct);
                var weight = ResolveEventWeight(eventType, settings);
                var now = DateTime.UtcNow;

                var evt = new ZansiPulseEvent
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    EventType = eventType,
                    ListingId = request.ListingId,
                    SellerId = request.SellerId,
                    ShopId = request.ShopId,
                    CategoryId = request.CategoryId,
                    SubCategoryId = request.SubCategoryId,
                    SearchTerm = Trim(request.SearchTerm),
                    Province = Trim(request.Province),
                    City = Trim(request.City),
                    Price = request.Price,
                    Weight = weight,
                    MetadataJson = request.MetadataJson,
                    CreatedAt = now,
                };

                // The event is the source of truth — persist it FIRST, on its
                // own, so a hiccup updating derived metrics can never lose the
                // raw signal.
                _db.ZansiPulseEvents.Add(evt);
                await _db.SaveChangesAsync(ct);

                // Best-effort derived updates. Wrapped so metric drift (e.g. a
                // rare unique-index race on first-create) never fails the
                // track call; snapshots reconcile aggregates anyway.
                try
                {
                    await UpdateDerivedAsync(evt, settings, request.ResultCount, ct);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "ZansiPulse: derived-metric update failed for event {EventId} ({EventType}).", evt.Id, eventType);
                }

                return Result<Guid>.Success(evt.Id, "Event tracked.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ZansiPulse TrackEvent failed. UserId={UserId} Type={Type}", userId, request?.EventType);
                return Result<Guid>.Failure(ErrorCodes.Exception, "Could not track the event.");
            }
        }

        private async Task UpdateDerivedAsync(ZansiPulseEvent evt, Dictionary<string, string> settings, int? resultCount, CancellationToken ct)
        {
            var now = evt.CreatedAt;

            if (evt.ListingId.HasValue)
                await UpsertListingMetricAsync(evt.ListingId.Value, evt.EventType, now, ct);

            if (evt.SellerId.HasValue)
                await UpsertSellerMetricAsync(evt.SellerId.Value, evt.EventType, now, ct);

            if (evt.ShopId.HasValue)
                await UpsertShopMetricAsync(evt.ShopId.Value, evt.EventType, now, ct);

            if (evt.CategoryId.HasValue)
                await UpsertCategoryMetricAsync(evt.CategoryId.Value, evt.SubCategoryId, evt.EventType, now, ct);

            if (!string.IsNullOrWhiteSpace(evt.Province))
                await UpsertRegionMetricAsync(evt.Province!, evt.City, evt.EventType, now, ct);

            if (evt.EventType == ZansiPulseEventType.SearchTerm && !string.IsNullOrWhiteSpace(evt.SearchTerm))
                await UpsertSearchTermMetricAsync(evt, resultCount, now, ct);

            // Behavioural interest adjustment — only when we know the user AND
            // the category, and the event actually carries weight.
            if (evt.UserId.HasValue && evt.CategoryId.HasValue && evt.Weight != 0m)
                await AdjustInterestAsync(evt.UserId.Value, evt.CategoryId.Value, evt.SubCategoryId, evt.Weight, settings, now, ct);

            await _db.SaveChangesAsync(ct);
        }

        private async Task UpsertListingMetricAsync(Guid listingId, ZansiPulseEventType type, DateTime now, CancellationToken ct)
        {
            var m = await _db.ZansiPulseListingMetrics.FirstOrDefaultAsync(x => x.ListingId == listingId, ct);
            if (m is null)
            {
                m = new ZansiPulseListingMetric { Id = Guid.NewGuid(), ListingId = listingId };
                _db.ZansiPulseListingMetrics.Add(m);
            }

            switch (type)
            {
                case ZansiPulseEventType.ViewListing: m.TotalViews++; break;
                case ZansiPulseEventType.OpenListingDetail: m.TotalDetailOpens++; break;
                case ZansiPulseEventType.FavouriteListing: m.TotalFavourites++; break;
                case ZansiPulseEventType.ShareListing: m.TotalShares++; break;
                case ZansiPulseEventType.MessageSeller: m.TotalMessages++; break;
                case ZansiPulseEventType.ReportListing: m.TotalReports++; break;
            }

            if (IsPositiveEngagement(type)) m.LastEngagementAt = now;
            m.TrendingScore = ListingEngagementBase(m);
            m.UpdatedAt = now;
        }

        private async Task UpsertSellerMetricAsync(Guid sellerId, ZansiPulseEventType type, DateTime now, CancellationToken ct)
        {
            var m = await _db.ZansiPulseSellerMetrics.FirstOrDefaultAsync(x => x.SellerId == sellerId, ct);
            if (m is null)
            {
                m = new ZansiPulseSellerMetric { Id = Guid.NewGuid(), SellerId = sellerId, ResponsivenessScore = 50m };
                _db.ZansiPulseSellerMetrics.Add(m);
            }

            switch (type)
            {
                case ZansiPulseEventType.ViewListing:
                case ZansiPulseEventType.OpenListingDetail:
                case ZansiPulseEventType.OpenSellerProfile: m.TotalViews++; break;
                case ZansiPulseEventType.FavouriteListing: m.TotalFavourites++; break;
                case ZansiPulseEventType.MessageSeller: m.TotalMessages++; break;
                case ZansiPulseEventType.ReportListing: m.TotalReports++; break;
            }

            var pop = m.TotalViews + (m.TotalFavourites * 8) + (m.TotalMessages * 12);
            m.PopularityScore = Saturate100(pop, SellerPopularityHalf);
            m.TrustScore = Clamp(100m - (m.TotalReports * 10m), 0m, 100m);
            m.QualityScore = Math.Round((m.PopularityScore + m.TrustScore) / 2m, 4);
            m.UpdatedAt = now;
        }

        private async Task UpsertShopMetricAsync(Guid shopId, ZansiPulseEventType type, DateTime now, CancellationToken ct)
        {
            var m = await _db.ZansiPulseShopMetrics.FirstOrDefaultAsync(x => x.ShopId == shopId, ct);
            if (m is null)
            {
                m = new ZansiPulseShopMetric { Id = Guid.NewGuid(), ShopId = shopId };
                _db.ZansiPulseShopMetrics.Add(m);
            }

            switch (type)
            {
                case ZansiPulseEventType.ViewListing:
                case ZansiPulseEventType.OpenListingDetail:
                case ZansiPulseEventType.OpenShopProfile: m.TotalViews++; break;
                case ZansiPulseEventType.FavouriteListing: m.TotalFavourites++; break;
                case ZansiPulseEventType.ShareListing: m.TotalShares++; break;
                case ZansiPulseEventType.MessageSeller: m.TotalMessages++; break;
                case ZansiPulseEventType.ReportListing: m.TotalReports++; break;
            }

            var pop = m.TotalViews + (m.TotalFavourites * 8) + (m.TotalShares * 8) + (m.TotalMessages * 12);
            m.PopularityScore = Saturate100(pop, ShopPopularityHalf);
            m.QualityScore = Clamp(100m - (m.TotalReports * 10m), 0m, 100m);
            m.UpdatedAt = now;
        }

        private async Task UpsertCategoryMetricAsync(Guid categoryId, Guid? subCategoryId, ZansiPulseEventType type, DateTime now, CancellationToken ct)
        {
            var (start, end) = DayBucket(now);
            var m = await _db.ZansiPulseCategoryMetrics.FirstOrDefaultAsync(
                x => x.CategoryId == categoryId && x.SubCategoryId == subCategoryId
                     && x.PeriodType == ZansiPulsePeriodType.Daily && x.PeriodStart == start, ct);
            if (m is null)
            {
                m = new ZansiPulseCategoryMetric
                {
                    Id = Guid.NewGuid(),
                    CategoryId = categoryId,
                    SubCategoryId = subCategoryId,
                    PeriodType = ZansiPulsePeriodType.Daily,
                    PeriodStart = start,
                    PeriodEnd = end,
                };
                _db.ZansiPulseCategoryMetrics.Add(m);
            }

            ApplyDemandCounters(type, () => m.TotalViews++, () => m.TotalSearches++, () => m.TotalFavourites++, () => m.TotalMessages++);
            m.TrendingScore = DemandScore(m.TotalViews, m.TotalSearches, m.TotalFavourites, m.TotalMessages);
            m.UpdatedAt = now;
        }

        private async Task UpsertRegionMetricAsync(string province, string? city, ZansiPulseEventType type, DateTime now, CancellationToken ct)
        {
            var (start, end) = DayBucket(now);
            var m = await _db.ZansiPulseRegionMetrics.FirstOrDefaultAsync(
                x => x.Province == province && x.City == city
                     && x.PeriodType == ZansiPulsePeriodType.Daily && x.PeriodStart == start, ct);
            if (m is null)
            {
                m = new ZansiPulseRegionMetric
                {
                    Id = Guid.NewGuid(),
                    Province = province,
                    City = city,
                    PeriodType = ZansiPulsePeriodType.Daily,
                    PeriodStart = start,
                    PeriodEnd = end,
                };
                _db.ZansiPulseRegionMetrics.Add(m);
            }

            ApplyDemandCounters(type, () => m.TotalViews++, () => m.TotalSearches++, () => m.TotalFavourites++, () => m.TotalMessages++);
            m.TrendingScore = DemandScore(m.TotalViews, m.TotalSearches, m.TotalFavourites, m.TotalMessages);
            m.UpdatedAt = now;
        }

        private async Task UpsertSearchTermMetricAsync(ZansiPulseEvent evt, int? resultCount, DateTime now, CancellationToken ct)
        {
            var term = evt.SearchTerm!.Trim();
            var (start, end) = DayBucket(now);
            var m = await _db.ZansiPulseSearchTermMetrics.FirstOrDefaultAsync(
                x => x.SearchTerm == term && x.Province == evt.Province && x.City == evt.City
                     && x.CategoryId == evt.CategoryId
                     && x.PeriodType == ZansiPulsePeriodType.Daily && x.PeriodStart == start, ct);
            if (m is null)
            {
                m = new ZansiPulseSearchTermMetric
                {
                    Id = Guid.NewGuid(),
                    SearchTerm = term,
                    Province = evt.Province,
                    City = evt.City,
                    CategoryId = evt.CategoryId,
                    PeriodType = ZansiPulsePeriodType.Daily,
                    PeriodStart = start,
                    PeriodEnd = end,
                };
                _db.ZansiPulseSearchTermMetrics.Add(m);
            }

            m.SearchCount++;
            if (resultCount.HasValue)
            {
                m.ResultCount += resultCount.Value;
                // A zero-result search is a strong unmet-demand signal.
                if (resultCount.Value == 0) m.NoResultCount++;
            }
            m.UpdatedAt = now;
        }

        private async Task AdjustInterestAsync(Guid userId, Guid categoryId, Guid? subCategoryId, decimal weight, Dictionary<string, string> settings, DateTime now, CancellationToken ct)
        {
            var min = GetDecimal(settings, ZansiPulseDefaults.MinScoreKey, ZansiPulseDefaults.MinInterestScore);
            var max = GetDecimal(settings, ZansiPulseDefaults.MaxScoreKey, ZansiPulseDefaults.MaxInterestScore);

            var score = await _db.ZansiPulseUserInterestScores.FirstOrDefaultAsync(
                x => x.UserId == userId && x.CategoryId == categoryId && x.SubCategoryId == subCategoryId, ct);

            if (score is null)
            {
                score = new ZansiPulseUserInterestScore
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    CategoryId = categoryId,
                    SubCategoryId = subCategoryId,
                    Score = Clamp(weight, min, max),
                    Source = ZansiPulseInterestSource.Behaviour,
                    CreatedAt = now,
                    LastUpdatedAt = now,
                };
                _db.ZansiPulseUserInterestScores.Add(score);
            }
            else
            {
                score.Score = Clamp(score.Score + weight, min, max);
                // Onboarding seed that's since been nudged by behaviour → Mixed.
                if (score.Source == ZansiPulseInterestSource.Onboarding)
                    score.Source = ZansiPulseInterestSource.Mixed;
                score.LastUpdatedAt = now;
            }
        }

        // ════════════════════════════════════════════════════════════════════
        // User interests
        // ════════════════════════════════════════════════════════════════════

        public async Task<Result<List<UserInterestScoreDto>>> GetMyInterestsAsync(Guid userId, CancellationToken ct = default)
        {
            try
            {
                var scores = await _db.ZansiPulseUserInterestScores.AsNoTracking()
                    .Where(x => x.UserId == userId)
                    .OrderByDescending(x => x.Score)
                    .ToListAsync(ct);

                var dtos = await JoinInterestNamesAsync(scores, ct);
                return Result<List<UserInterestScoreDto>>.Success(dtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ZansiPulse GetMyInterests failed. UserId={UserId}", userId);
                return Result<List<UserInterestScoreDto>>.Failure(ErrorCodes.Exception, "Could not load interests.");
            }
        }

        public async Task<Result<List<UserInterestScoreDto>>> SaveOnboardingInterestsAsync(Guid userId, SaveInterestsRequestDto request, CancellationToken ct = default)
        {
            try
            {
                if (request?.CategoryIds is null || request.CategoryIds.Count == 0)
                    return Result<List<UserInterestScoreDto>>.Failure(ErrorCodes.BadRequest, "Select at least one category.");

                // Settings are TUNABLES with safe coded defaults (GetDecimal
                // falls back to ZansiPulseDefaults). Onboarding must NOT hard-fail
                // if the settings table is unavailable/empty — this is the one DB
                // dependency the working read path (GetMyInterests) doesn't have,
                // so load it DEFENSIVELY. A missing/locked ZansiPulseSettings can
                // never block a user from finishing onboarding.
                Dictionary<string, string> settings;
                try
                {
                    settings = await LoadSettingsAsync(ct);
                }
                catch (Exception sx)
                {
                    _logger.LogWarning(sx,
                        "ZansiPulse onboarding: settings load failed — using defaults. UserId={UserId}", userId);
                    settings = new Dictionary<string, string>();
                }
                var seed = GetDecimal(settings, ZansiPulseDefaults.OnboardingScoreKey, ZansiPulseDefaults.OnboardingInterestScore);
                var max = GetDecimal(settings, ZansiPulseDefaults.MaxScoreKey, ZansiPulseDefaults.MaxInterestScore);
                var now = DateTime.UtcNow;

                var ids = request.CategoryIds.Distinct().ToList();

                // Only seed categories that actually exist, so onboarding can't
                // create scores for stale/garbage ids.
                var validIds = await _db.SellerCategories.AsNoTracking()
                    .Where(c => ids.Contains(c.Id))
                    .Select(c => c.Id)
                    .ToListAsync(ct);

                if (validIds.Count == 0)
                    return Result<List<UserInterestScoreDto>>.Failure(ErrorCodes.BadRequest, "None of the selected categories were found.");

                var existing = await _db.ZansiPulseUserInterestScores
                    .Where(x => x.UserId == userId && x.SubCategoryId == null && validIds.Contains(x.CategoryId))
                    .ToListAsync(ct);
                // Dup-safe (group + first): a stray duplicate (UserId, CategoryId,
                // null) row from behavioural nudges must never crash onboarding
                // via a ToDictionary key collision.
                var byCategory = existing
                    .GroupBy(x => x.CategoryId)
                    .ToDictionary(g => g.Key, g => g.First());

                foreach (var categoryId in validIds)
                {
                    if (byCategory.TryGetValue(categoryId, out var row))
                    {
                        // Don't downgrade a stronger behavioural score; top up to the seed.
                        row.Score = Clamp(Math.Max(row.Score, seed), ZansiPulseDefaults.MinInterestScore, max);
                        if (row.Source == ZansiPulseInterestSource.Behaviour)
                            row.Source = ZansiPulseInterestSource.Mixed;
                        row.LastUpdatedAt = now;
                    }
                    else
                    {
                        _db.ZansiPulseUserInterestScores.Add(new ZansiPulseUserInterestScore
                        {
                            Id = Guid.NewGuid(),
                            UserId = userId,
                            CategoryId = categoryId,
                            SubCategoryId = null,
                            Score = Clamp(seed, ZansiPulseDefaults.MinInterestScore, max),
                            Source = ZansiPulseInterestSource.Onboarding,
                            CreatedAt = now,
                            LastUpdatedAt = now,
                        });
                    }
                }

                await _db.SaveChangesAsync(ct);
                return await GetMyInterestsAsync(userId, ct);
            }
            catch (Exception ex)
            {
                // Log enough to pinpoint the cause in UAT without leaking data:
                // exception type/message (via ex) + user + how many categories.
                _logger.LogError(ex,
                    "ZansiPulse SaveOnboardingInterests failed. UserId={UserId} CategoryCount={Count} ExceptionType={ExType}",
                    userId, request?.CategoryIds?.Count ?? 0, ex.GetType().Name);
                return Result<List<UserInterestScoreDto>>.Failure(ErrorCodes.Exception, "Could not save interests.");
            }
        }

        // ════════════════════════════════════════════════════════════════════
        // Recommendations
        // ════════════════════════════════════════════════════════════════════

        public async Task<Result<List<RecommendedListingDto>>> GetListingRecommendationsAsync(Guid userId, int take, CancellationToken ct = default)
        {
            try
            {
                take = Math.Clamp(take, 1, 100);
                var settings = await LoadSettingsAsync(ct);
                var now = DateTime.UtcNow;

                var weights = LoadRecWeights(settings);
                var interestByCategory = await LoadUserInterestByCategoryAsync(userId, ct);
                var (province, city) = await LoadUserRegionAsync(userId, ct);
                var excluded = await LoadNegativeListingIdsAsync(userId, ct);
                var norm = GetDecimal(settings, ZansiPulseDefaults.NormalizerKey, ZansiPulseDefaults.InterestNormalizer);

                // Visibility contract MUST mirror the canonical buyer feed
                // (ListingRepository.SearchAsync): Active status AND not
                // InStoreOnly. Without the second gate, a physical-store-only
                // catalog item could be recommended into Home/Explore where it
                // can't be bought. Merchant-approval is intentionally NOT
                // re-checked here — the existing feed relies on the invariant
                // "a listing only reaches Active once its seller is approved",
                // and ZansiPulse must stay consistent with that single source
                // of truth rather than introduce a divergent rule.
                var candidates = await _db.Listings.AsNoTracking()
                    .Where(l => l.Status == ListingStatus.Active
                        && l.AvailabilityMode != AvailabilityMode.InStoreOnly
                        // Drop seller-paused / shop-paused listings (independent pauses).
                        && ((l.ListingSource != ListingSource.ShopProfile
                                && l.Merchant != null
                                && l.Merchant.SellerVisibility == ZansiHustle.Shared.Enums.Merchants.SellerVisibilityStatus.Visible)
                            || (l.ListingSource == ListingSource.ShopProfile
                                && l.ShopProfile != null
                                && l.ShopProfile.VisibilityStatus == ZansiHustle.Shared.Enums.Shops.ShopVisibilityStatus.Visible)))
                    .OrderByDescending(l => l.CreatedAtUtc)
                    .Take(ListingCandidatePool)
                    .Include(l => l.Merchant)
                    .Include(l => l.SellerCategory)
                    .ToListAsync(ct);

                candidates = candidates.Where(l => !excluded.Contains(l.Id)).ToList();
                if (candidates.Count == 0)
                    return Result<List<RecommendedListingDto>>.Success(new List<RecommendedListingDto>());

                var candidateIds = candidates.Select(l => l.Id).ToList();
                var metricById = await _db.ZansiPulseListingMetrics.AsNoTracking()
                    .Where(m => candidateIds.Contains(m.ListingId))
                    .ToDictionaryAsync(m => m.ListingId, ct);

                var merchantIds = candidates.Select(l => l.MerchantId).Distinct().ToList();
                var sellerMetricById = await _db.ZansiPulseSellerMetrics.AsNoTracking()
                    .Where(m => merchantIds.Contains(m.SellerId))
                    .ToDictionaryAsync(m => m.SellerId, ct);

                var scored = candidates.Select(l =>
                {
                    var interest = l.SellerCategoryId.HasValue && interestByCategory.TryGetValue(l.SellerCategoryId.Value, out var s)
                        ? Clamp(s / norm, 0m, 1m) : 0m;
                    var location = LocationFactor(province, city, l.Province, l.City);
                    var engagement = metricById.TryGetValue(l.Id, out var lm)
                        ? Clamp(DecayedTrending(lm.TrendingScore, lm.LastEngagementAt, now) / ListingEngagementHalf, 0m, 1m) : 0m;
                    var sellerQuality = sellerMetricById.TryGetValue(l.MerchantId, out var sm)
                        ? Clamp(sm.QualityScore / 100m, 0m, 1m)
                        : (l.Merchant?.Rating is decimal r ? Clamp(r / 5m, 0m, 1m) : 0m);
                    var recency = (decimal)RecencyMultiplier(l.CreatedAtUtc, now);
                    var boost = (l.IsFeatured || l.IsBoosted) ? 1m : 0m;

                    var score = (weights.Interest * interest)
                              + (weights.Location * location)
                              + (weights.Engagement * engagement)
                              + (weights.SellerQuality * sellerQuality)
                              + (weights.Recency * recency)
                              + (weights.PlatformBoost * boost);

                    return new RecommendedListingDto
                    {
                        ListingId = l.Id,
                        Type = l.Type,
                        Title = l.Title,
                        Price = l.Price,
                        Currency = l.Currency,
                        Province = l.Province,
                        City = l.City,
                        ImageUrl = l.Images.FirstOrDefault(),
                        SellerCategoryId = l.SellerCategoryId,
                        CategoryName = l.SellerCategory?.Name,
                        MerchantId = l.MerchantId,
                        MerchantName = l.Merchant?.Name,
                        Score = Math.Round(score * 100m, 2),
                    };
                })
                .OrderByDescending(x => x.Score)
                .ThenByDescending(x => x.ListingId)
                .Take(take)
                .ToList();

                await LogRecommendationAsync(userId, "Listings",
                    interestByCategory.Count > 0 ? "Personalized" : "Fallback",
                    listingIds: scored.Select(x => x.ListingId), ct: ct);

                return Result<List<RecommendedListingDto>>.Success(scored);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ZansiPulse GetListingRecommendations failed. UserId={UserId}", userId);
                return Result<List<RecommendedListingDto>>.Failure(ErrorCodes.Exception, "Could not load recommendations.");
            }
        }

        public async Task<Result<List<RecommendedShopDto>>> GetShopRecommendationsAsync(Guid userId, int take, CancellationToken ct = default)
        {
            try
            {
                take = Math.Clamp(take, 1, 100);
                var now = DateTime.UtcNow;
                var interestByCategory = await LoadUserInterestByCategoryAsync(userId, ct);
                var (province, city) = await LoadUserRegionAsync(userId, ct);

                var shops = await _db.ShopProfiles.AsNoTracking()
                    .Where(s => s.Status == ShopProfileStatus.Active)
                    .OrderByDescending(s => s.FollowersCount)
                    .Take(ShopCandidatePool)
                    .ToListAsync(ct);

                if (shops.Count == 0)
                    return Result<List<RecommendedShopDto>>.Success(new List<RecommendedShopDto>());

                var shopIds = shops.Select(s => s.Id).ToList();
                var metricById = await _db.ZansiPulseShopMetrics.AsNoTracking()
                    .Where(m => shopIds.Contains(m.ShopId))
                    .ToDictionaryAsync(m => m.ShopId, ct);

                var scored = shops.Select(s =>
                {
                    var popularity = metricById.TryGetValue(s.Id, out var sm)
                        ? Clamp(sm.PopularityScore / 100m, 0m, 1m)
                        : Clamp(Saturate100(s.FollowersCount * 8, ShopPopularityHalf) / 100m, 0m, 1m);
                    var interest = s.SellerCategoryId.HasValue && interestByCategory.TryGetValue(s.SellerCategoryId.Value, out var iv)
                        ? Clamp(iv / ZansiPulseDefaults.InterestNormalizer, 0m, 1m) : 0m;
                    var rating = s.Rating is decimal r ? Clamp(r / 5m, 0m, 1m) : 0m;
                    var location = LocationFactor(province, city, s.Province, s.City);

                    var score = (0.40m * popularity) + (0.25m * interest) + (0.20m * rating) + (0.15m * location);

                    return new RecommendedShopDto
                    {
                        ShopId = s.Id,
                        Name = s.Name,
                        LogoUrl = s.LogoUrl,
                        Province = s.Province,
                        City = s.City,
                        Rating = s.Rating,
                        FollowersCount = s.FollowersCount,
                        Score = Math.Round(score * 100m, 2),
                    };
                })
                .OrderByDescending(x => x.Score)
                .ThenByDescending(x => x.FollowersCount)
                .Take(take)
                .ToList();

                await LogRecommendationAsync(userId, "Shops",
                    interestByCategory.Count > 0 ? "Personalized" : "Fallback",
                    shopIds: scored.Select(x => x.ShopId), ct: ct);

                return Result<List<RecommendedShopDto>>.Success(scored);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ZansiPulse GetShopRecommendations failed. UserId={UserId}", userId);
                return Result<List<RecommendedShopDto>>.Failure(ErrorCodes.Exception, "Could not load shop recommendations.");
            }
        }

        // ════════════════════════════════════════════════════════════════════
        // Trending
        // ════════════════════════════════════════════════════════════════════

        public async Task<Result<List<TrendingListingDto>>> GetTrendingListingsAsync(TrendingListingFilterDto filter, CancellationToken ct = default)
        {
            try
            {
                filter ??= new TrendingListingFilterDto();
                var take = Math.Clamp(filter.Take, 1, 100);
                var now = DateTime.UtcNow;
                DateTime? windowStart = filter.Period.HasValue ? LookbackStart(filter.Period.Value, now) : null;

                // Pull a generous slice by raw engagement, then re-rank in
                // memory with recency decay + apply listing-side filters. Same
                // visibility contract as the buyer feed: Active AND not
                // InStoreOnly (physical-store-only items never trend online).
                var query =
                    from m in _db.ZansiPulseListingMetrics.AsNoTracking()
                    join l in _db.Listings.AsNoTracking() on m.ListingId equals l.Id
                    where l.Status == ListingStatus.Active
                          && l.AvailabilityMode != AvailabilityMode.InStoreOnly
                          // Drop seller-paused / shop-paused listings (independent pauses).
                          && ((l.ListingSource != ListingSource.ShopProfile
                                  && l.Merchant != null
                                  && l.Merchant.SellerVisibility == ZansiHustle.Shared.Enums.Merchants.SellerVisibilityStatus.Visible)
                              || (l.ListingSource == ListingSource.ShopProfile
                                  && l.ShopProfile != null
                                  && l.ShopProfile.VisibilityStatus == ZansiHustle.Shared.Enums.Shops.ShopVisibilityStatus.Visible))
                    select new { m, l };

                if (!string.IsNullOrWhiteSpace(filter.Province))
                    query = query.Where(x => x.l.Province == filter.Province);
                if (!string.IsNullOrWhiteSpace(filter.City))
                    query = query.Where(x => x.l.City == filter.City);
                if (filter.CategoryId.HasValue)
                    query = query.Where(x => x.l.SellerCategoryId == filter.CategoryId);
                if (windowStart.HasValue)
                    query = query.Where(x => x.m.LastEngagementAt != null && x.m.LastEngagementAt >= windowStart);

                var rows = await query
                    .OrderByDescending(x => x.m.TrendingScore)
                    .Take(Math.Min(200, take * 5))
                    .ToListAsync(ct);

                // Batch-load display names so each card carries category +
                // seller context (no N+1, no follow-up fetch on the client).
                var catIds = rows.Where(x => x.l.SellerCategoryId.HasValue)
                    .Select(x => x.l.SellerCategoryId!.Value).Distinct().ToList();
                var merchantIds = rows.Select(x => x.l.MerchantId).Distinct().ToList();
                var catNames = catIds.Count == 0
                    ? new Dictionary<Guid, string>()
                    : await _db.SellerCategories.AsNoTracking()
                        .Where(c => catIds.Contains(c.Id)).ToDictionaryAsync(c => c.Id, c => c.Name, ct);
                var merchantNames = merchantIds.Count == 0
                    ? new Dictionary<Guid, string>()
                    : await _db.Merchants.AsNoTracking()
                        .Where(m => merchantIds.Contains(m.Id)).ToDictionaryAsync(m => m.Id, m => m.Name, ct);

                var result = rows.Select(x => new TrendingListingDto
                {
                    ListingId = x.l.Id,
                    Type = x.l.Type,
                    Title = x.l.Title,
                    Price = x.l.Price,
                    Currency = x.l.Currency,
                    Province = x.l.Province,
                    City = x.l.City,
                    ImageUrl = x.l.Images.FirstOrDefault(),
                    SellerCategoryId = x.l.SellerCategoryId,
                    CategoryName = x.l.SellerCategoryId.HasValue && catNames.TryGetValue(x.l.SellerCategoryId.Value, out var cn) ? cn : null,
                    MerchantId = x.l.MerchantId,
                    MerchantName = merchantNames.TryGetValue(x.l.MerchantId, out var mn) ? mn : null,
                    TrendingScore = Math.Round(DecayedTrending(x.m.TrendingScore, x.m.LastEngagementAt, now), 2),
                    TotalViews = x.m.TotalViews,
                    TotalFavourites = x.m.TotalFavourites,
                })
                .OrderByDescending(x => x.TrendingScore)
                .ThenByDescending(x => x.TotalViews)
                .Take(take)
                .ToList();

                return Result<List<TrendingListingDto>>.Success(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ZansiPulse GetTrendingListings failed.");
                return Result<List<TrendingListingDto>>.Failure(ErrorCodes.Exception, "Could not load trending listings.");
            }
        }

        public async Task<Result<List<TrendingCategoryDto>>> GetTrendingCategoriesAsync(ZansiPulsePeriodType period, string? province, string? city, int take, CancellationToken ct = default)
        {
            try
            {
                take = Math.Clamp(take, 1, 100);
                var (start, end) = (LookbackStart(period, DateTime.UtcNow), DateTime.UtcNow);
                var events = await ScanEventsAsync(start, end, Trim(province), Trim(city), ct);

                var result = events
                    .Where(e => e.CategoryId.HasValue)
                    .GroupBy(e => e.CategoryId!.Value)
                    .Select(g => BuildCategoryTrend(g.Key, g))
                    .OrderByDescending(x => x.TrendingScore)
                    .Take(take)
                    .ToList();

                await AttachCategoryNamesAsync(result, ct);
                return Result<List<TrendingCategoryDto>>.Success(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ZansiPulse GetTrendingCategories failed.");
                return Result<List<TrendingCategoryDto>>.Failure(ErrorCodes.Exception, "Could not load trending categories.");
            }
        }

        public async Task<Result<List<TrendingRegionDto>>> GetTrendingRegionsAsync(ZansiPulsePeriodType period, int take, CancellationToken ct = default)
        {
            try
            {
                take = Math.Clamp(take, 1, 100);
                var start = LookbackStart(period, DateTime.UtcNow);
                var events = await ScanEventsAsync(start, DateTime.UtcNow, null, null, ct);

                var result = events
                    .Where(e => !string.IsNullOrWhiteSpace(e.Province))
                    .GroupBy(e => new { Province = e.Province!, e.City })
                    .Select(g => BuildRegionTrend(g.Key.Province, g.Key.City, g))
                    .OrderByDescending(x => x.TrendingScore)
                    .Take(take)
                    .ToList();

                return Result<List<TrendingRegionDto>>.Success(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ZansiPulse GetTrendingRegions failed.");
                return Result<List<TrendingRegionDto>>.Failure(ErrorCodes.Exception, "Could not load trending regions.");
            }
        }

        // ════════════════════════════════════════════════════════════════════
        // Dashboard
        // ════════════════════════════════════════════════════════════════════

        public async Task<Result<DashboardOverviewDto>> GetDashboardOverviewAsync(ZansiPulsePeriodType period, int take, CancellationToken ct = default)
        {
            try
            {
                take = Math.Clamp(take, 1, 50);
                var now = DateTime.UtcNow;
                var start = LookbackStart(period, now);
                var events = await ScanEventsAsync(start, now, null, null, ct);

                var dto = new DashboardOverviewDto
                {
                    PeriodStart = start,
                    PeriodEnd = now,
                    TotalEvents = events.Count,
                };

                dto.TopCategories = events.Where(e => e.CategoryId.HasValue)
                    .GroupBy(e => e.CategoryId!.Value)
                    .Select(g => BuildCategoryTrend(g.Key, g))
                    .OrderByDescending(x => x.TrendingScore).Take(take).ToList();
                await AttachCategoryNamesAsync(dto.TopCategories, ct);

                dto.TopRegions = events.Where(e => !string.IsNullOrWhiteSpace(e.Province))
                    .GroupBy(e => new { Province = e.Province!, e.City })
                    .Select(g => BuildRegionTrend(g.Key.Province, g.Key.City, g))
                    .OrderByDescending(x => x.TrendingScore).Take(take).ToList();

                var trendingListings = await GetTrendingListingsAsync(new TrendingListingFilterDto { Take = take, Period = period }, ct);
                dto.TopListings = trendingListings.Data ?? new List<TrendingListingDto>();

                dto.TopSellers = await LoadTopSellersAsync(take, ct);

                dto.TopSearchTerms = events.Where(e => e.EventType == ZansiPulseEventType.SearchTerm && !string.IsNullOrWhiteSpace(e.SearchTerm))
                    .GroupBy(e => e.SearchTerm!.Trim().ToLowerInvariant())
                    .Select(g => new TopSearchTermDto { SearchTerm = g.Key, SearchCount = g.Count() })
                    .OrderByDescending(x => x.SearchCount).Take(take).ToList();

                dto.SupplyDemandGaps = await ComputeSupplyDemandAsync(events, null, null, take, ct);

                return Result<DashboardOverviewDto>.Success(dto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ZansiPulse GetDashboardOverview failed.");
                return Result<DashboardOverviewDto>.Failure(ErrorCodes.Exception, "Could not load dashboard overview.");
            }
        }

        public async Task<Result<List<SupplyDemandGapDto>>> GetSupplyDemandAsync(string? province, string? city, int take, CancellationToken ct = default)
        {
            try
            {
                take = Math.Clamp(take, 1, 100);
                var now = DateTime.UtcNow;
                var start = LookbackStart(ZansiPulsePeriodType.Monthly, now);
                var events = await ScanEventsAsync(start, now, Trim(province), Trim(city), ct);
                var gaps = await ComputeSupplyDemandAsync(events, Trim(province), Trim(city), take, ct);
                return Result<List<SupplyDemandGapDto>>.Success(gaps);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ZansiPulse GetSupplyDemand failed.");
                return Result<List<SupplyDemandGapDto>>.Failure(ErrorCodes.Exception, "Could not compute supply-demand gaps.");
            }
        }

        public async Task<Result<SnapshotResultDto>> RunSnapshotAsync(ZansiPulsePeriodType snapshotType, CancellationToken ct = default)
        {
            try
            {
                var now = DateTime.UtcNow;
                var start = LookbackStart(snapshotType, now);
                var events = await ScanEventsAsync(start, now, null, null, ct);

                var topCategories = events.Where(e => e.CategoryId.HasValue)
                    .GroupBy(e => e.CategoryId!.Value).Select(g => BuildCategoryTrend(g.Key, g))
                    .OrderByDescending(x => x.TrendingScore).Take(10).ToList();
                await AttachCategoryNamesAsync(topCategories, ct);

                var topRegions = events.Where(e => !string.IsNullOrWhiteSpace(e.Province))
                    .GroupBy(e => new { Province = e.Province!, e.City }).Select(g => BuildRegionTrend(g.Key.Province, g.Key.City, g))
                    .OrderByDescending(x => x.TrendingScore).Take(10).ToList();

                var topListings = (await GetTrendingListingsAsync(new TrendingListingFilterDto { Take = 10, Period = snapshotType }, ct)).Data
                                  ?? new List<TrendingListingDto>();
                var topSellers = await LoadTopSellersAsync(10, ct);
                var topSearchTerms = events.Where(e => e.EventType == ZansiPulseEventType.SearchTerm && !string.IsNullOrWhiteSpace(e.SearchTerm))
                    .GroupBy(e => e.SearchTerm!.Trim().ToLowerInvariant())
                    .Select(g => new TopSearchTermDto { SearchTerm = g.Key, SearchCount = g.Count() })
                    .OrderByDescending(x => x.SearchCount).Take(10).ToList();
                var gaps = await ComputeSupplyDemandAsync(events, null, null, 10, ct);

                // Persist the computed gaps for this period (replace any prior
                // rows for the same window so the table doesn't accumulate
                // duplicates on re-run).
                await _db.ZansiPulseSupplyDemandGaps
                    .Where(g => g.PeriodStart == start && g.PeriodEnd == now)
                    .ExecuteDeleteAsync(ct);
                foreach (var g in gaps)
                {
                    _db.ZansiPulseSupplyDemandGaps.Add(new ZansiPulseSupplyDemandGap
                    {
                        Id = Guid.NewGuid(),
                        CategoryId = g.CategoryId,
                        SubCategoryId = g.SubCategoryId,
                        Province = g.Province,
                        City = g.City,
                        SearchTerm = g.SearchTerm,
                        DemandScore = g.DemandScore,
                        SupplyCount = g.SupplyCount,
                        GapScore = g.GapScore,
                        RecommendedAction = g.RecommendedAction,
                        PeriodStart = start,
                        PeriodEnd = now,
                        UpdatedAt = now,
                    });
                }

                var snapshot = await _db.ZansiPulseSnapshots.FirstOrDefaultAsync(
                    s => s.SnapshotType == snapshotType && s.PeriodStart == start && s.PeriodEnd == now, ct);
                if (snapshot is null)
                {
                    snapshot = new ZansiPulseSnapshot
                    {
                        Id = Guid.NewGuid(),
                        SnapshotType = snapshotType,
                        PeriodStart = start,
                        PeriodEnd = now,
                        CreatedAt = now,
                    };
                    _db.ZansiPulseSnapshots.Add(snapshot);
                }

                snapshot.TotalEvents = events.Count;
                snapshot.TopCategoriesJson = Json(topCategories);
                snapshot.TopRegionsJson = Json(topRegions);
                snapshot.TopListingsJson = Json(topListings);
                snapshot.TopSellersJson = Json(topSellers);
                snapshot.TopSearchTermsJson = Json(topSearchTerms);
                snapshot.SupplyDemandGapsJson = Json(gaps);

                await _db.SaveChangesAsync(ct);

                return Result<SnapshotResultDto>.Success(new SnapshotResultDto
                {
                    Id = snapshot.Id,
                    SnapshotType = snapshotType.ToString(),
                    PeriodStart = start,
                    PeriodEnd = now,
                    TotalEvents = snapshot.TotalEvents,
                    CreatedAt = snapshot.CreatedAt,
                }, "Snapshot generated.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ZansiPulse RunSnapshot failed. Type={Type}", snapshotType);
                return Result<SnapshotResultDto>.Failure(ErrorCodes.Exception, "Could not generate snapshot.");
            }
        }

        // ════════════════════════════════════════════════════════════════════
        // Shared compute helpers
        // ════════════════════════════════════════════════════════════════════

        private async Task<List<SupplyDemandGapDto>> ComputeSupplyDemandAsync(List<EvtRow> events, string? province, string? city, int take, CancellationToken ct)
        {
            // Demand: weighted category interaction within the window.
            var demandByCategory = events
                .Where(e => e.CategoryId.HasValue)
                .GroupBy(e => e.CategoryId!.Value)
                .Select(g => new
                {
                    CategoryId = g.Key,
                    Demand = DemandScore(
                        g.Count(e => IsView(e.EventType)),
                        g.Count(e => IsSearch(e.EventType)),
                        g.Count(e => e.EventType == ZansiPulseEventType.FavouriteListing),
                        g.Count(e => e.EventType == ZansiPulseEventType.MessageSeller)),
                })
                .Where(x => x.Demand > 0)
                .ToList();

            if (demandByCategory.Count == 0)
                return new List<SupplyDemandGapDto>();

            var categoryIds = demandByCategory.Select(x => x.CategoryId).ToList();

            // Supply: count of ACTIVE listings per category (region-filtered
            // when asked). Counts all availability modes — this is an
            // admin-only sourcing metric measuring whether any seller serves
            // the demand, so in-store-only catalogue items legitimately count
            // as supply (unlike the buyer-facing recommendation/trending
            // surfaces, which exclude them).
            var supplyQuery = _db.Listings.AsNoTracking()
                .Where(l => l.Status == ListingStatus.Active && l.SellerCategoryId != null && categoryIds.Contains(l.SellerCategoryId!.Value));
            if (!string.IsNullOrWhiteSpace(province))
                supplyQuery = supplyQuery.Where(l => l.Province == province);
            if (!string.IsNullOrWhiteSpace(city))
                supplyQuery = supplyQuery.Where(l => l.City == city);

            var supplyByCategory = (await supplyQuery
                    .GroupBy(l => l.SellerCategoryId!.Value)
                    .Select(g => new { CategoryId = g.Key, Count = g.Count() })
                    .ToListAsync(ct))
                .ToDictionary(x => x.CategoryId, x => x.Count);

            var names = await _db.SellerCategories.AsNoTracking()
                .Where(c => categoryIds.Contains(c.Id))
                .ToDictionaryAsync(c => c.Id, c => c.Name, ct);

            var gaps = demandByCategory.Select(d =>
            {
                var supply = supplyByCategory.TryGetValue(d.CategoryId, out var c) ? c : 0;
                // Gap rises with demand, falls as supply grows. +1 avoids /0.
                var gap = Math.Round(d.Demand / (supply + 1), 4);
                return new SupplyDemandGapDto
                {
                    CategoryId = d.CategoryId,
                    CategoryName = names.TryGetValue(d.CategoryId, out var n) ? n : null,
                    Province = province,
                    City = city,
                    DemandScore = Math.Round(d.Demand, 4),
                    SupplyCount = supply,
                    GapScore = gap,
                    RecommendedAction = supply == 0
                        ? "No active listings for live demand — recruit sellers in this category."
                        : (gap >= 5m ? "Undersupplied — encourage more listings in this category." : null),
                };
            })
            .OrderByDescending(x => x.GapScore)
            .Take(take)
            .ToList();

            return gaps;
        }

        private async Task<List<TopSellerDto>> LoadTopSellersAsync(int take, CancellationToken ct)
        {
            var metrics = await _db.ZansiPulseSellerMetrics.AsNoTracking()
                .OrderByDescending(m => m.PopularityScore)
                .Take(take)
                .ToListAsync(ct);
            if (metrics.Count == 0) return new List<TopSellerDto>();

            var ids = metrics.Select(m => m.SellerId).ToList();
            var names = await _db.Merchants.AsNoTracking()
                .Where(m => ids.Contains(m.Id))
                .ToDictionaryAsync(m => m.Id, m => m.Name, ct);

            return metrics.Select(m => new TopSellerDto
            {
                SellerId = m.SellerId,
                Name = names.TryGetValue(m.SellerId, out var n) ? n : null,
                PopularityScore = m.PopularityScore,
                QualityScore = m.QualityScore,
                TotalViews = m.TotalViews,
                TotalFavourites = m.TotalFavourites,
                TotalListings = m.TotalListings,
            }).ToList();
        }

        private TrendingCategoryDto BuildCategoryTrend(Guid categoryId, IEnumerable<EvtRow> rows)
        {
            int views = 0, searches = 0, favourites = 0, messages = 0;
            foreach (var e in rows)
            {
                if (IsView(e.EventType)) views++;
                else if (IsSearch(e.EventType)) searches++;
                if (e.EventType == ZansiPulseEventType.FavouriteListing) favourites++;
                if (e.EventType == ZansiPulseEventType.MessageSeller) messages++;
            }
            return new TrendingCategoryDto
            {
                CategoryId = categoryId,
                TrendingScore = DemandScore(views, searches, favourites, messages),
                TotalViews = views,
                TotalSearches = searches,
                TotalFavourites = favourites,
                TotalMessages = messages,
            };
        }

        private TrendingRegionDto BuildRegionTrend(string province, string? city, IEnumerable<EvtRow> rows)
        {
            int views = 0, searches = 0, favourites = 0, messages = 0;
            foreach (var e in rows)
            {
                if (IsView(e.EventType)) views++;
                else if (IsSearch(e.EventType)) searches++;
                if (e.EventType == ZansiPulseEventType.FavouriteListing) favourites++;
                if (e.EventType == ZansiPulseEventType.MessageSeller) messages++;
            }
            return new TrendingRegionDto
            {
                Province = province,
                City = city,
                TrendingScore = DemandScore(views, searches, favourites, messages),
                TotalViews = views,
                TotalSearches = searches,
                TotalFavourites = favourites,
                TotalMessages = messages,
            };
        }

        private async Task AttachCategoryNamesAsync(List<TrendingCategoryDto> items, CancellationToken ct)
        {
            if (items.Count == 0) return;
            var ids = items.Select(i => i.CategoryId).Distinct().ToList();
            var names = await _db.SellerCategories.AsNoTracking()
                .Where(c => ids.Contains(c.Id))
                .ToDictionaryAsync(c => c.Id, c => c.Name, ct);
            foreach (var i in items)
                if (names.TryGetValue(i.CategoryId, out var n)) i.CategoryName = n;
        }

        private async Task<List<UserInterestScoreDto>> JoinInterestNamesAsync(List<ZansiPulseUserInterestScore> scores, CancellationToken ct)
        {
            if (scores.Count == 0) return new List<UserInterestScoreDto>();

            var catIds = scores.Select(s => s.CategoryId).Distinct().ToList();
            var subIds = scores.Where(s => s.SubCategoryId.HasValue).Select(s => s.SubCategoryId!.Value).Distinct().ToList();

            var catNames = await _db.SellerCategories.AsNoTracking()
                .Where(c => catIds.Contains(c.Id)).ToDictionaryAsync(c => c.Id, c => c.Name, ct);
            var subNames = subIds.Count == 0
                ? new Dictionary<Guid, string>()
                : await _db.SellerSubcategories.AsNoTracking()
                    .Where(c => subIds.Contains(c.Id)).ToDictionaryAsync(c => c.Id, c => c.Name, ct);

            return scores.Select(s => new UserInterestScoreDto
            {
                CategoryId = s.CategoryId,
                CategoryName = catNames.TryGetValue(s.CategoryId, out var cn) ? cn : null,
                SubCategoryId = s.SubCategoryId,
                SubCategoryName = s.SubCategoryId.HasValue && subNames.TryGetValue(s.SubCategoryId.Value, out var sn) ? sn : null,
                Score = s.Score,
                Source = s.Source.ToString(),
                LastUpdatedAt = s.LastUpdatedAt,
            }).ToList();
        }

        private async Task<Dictionary<Guid, decimal>> LoadUserInterestByCategoryAsync(Guid userId, CancellationToken ct)
        {
            var rows = await _db.ZansiPulseUserInterestScores.AsNoTracking()
                .Where(x => x.UserId == userId)
                .Select(x => new { x.CategoryId, x.Score })
                .ToListAsync(ct);
            // Best score per category (across any subcategories).
            return rows.GroupBy(x => x.CategoryId)
                       .ToDictionary(g => g.Key, g => g.Max(x => x.Score));
        }

        private async Task<(string? province, string? city)> LoadUserRegionAsync(Guid userId, CancellationToken ct)
        {
            var p = await _db.UserProfiles.AsNoTracking()
                .Where(x => x.UserId == userId)
                .Select(x => new { x.Province, x.City })
                .FirstOrDefaultAsync(ct);
            return (p?.Province, p?.City);
        }

        private async Task<HashSet<Guid>> LoadNegativeListingIdsAsync(Guid userId, CancellationToken ct)
        {
            var ids = await _db.ZansiPulseEvents.AsNoTracking()
                .Where(e => e.UserId == userId && e.ListingId != null
                    && (e.EventType == ZansiPulseEventType.HideListing
                        || e.EventType == ZansiPulseEventType.NotInterested
                        || e.EventType == ZansiPulseEventType.ReportListing))
                .Select(e => e.ListingId!.Value)
                .Distinct()
                .ToListAsync(ct);
            return ids.ToHashSet();
        }

        private async Task<List<EvtRow>> ScanEventsAsync(DateTime start, DateTime end, string? province, string? city, CancellationToken ct)
        {
            var query = _db.ZansiPulseEvents.AsNoTracking()
                .Where(e => e.CreatedAt >= start && e.CreatedAt < end);
            if (!string.IsNullOrWhiteSpace(province))
                query = query.Where(e => e.Province == province);
            if (!string.IsNullOrWhiteSpace(city))
                query = query.Where(e => e.City == city);

            return await query
                .OrderByDescending(e => e.CreatedAt)
                .Take(MaxScanEvents)
                .Select(e => new EvtRow
                {
                    EventType = e.EventType,
                    CategoryId = e.CategoryId,
                    Province = e.Province,
                    City = e.City,
                    SearchTerm = e.SearchTerm,
                })
                .ToListAsync(ct);
        }

        private async Task LogRecommendationAsync(Guid? userId, string type, string source,
            IEnumerable<Guid>? listingIds = null, IEnumerable<Guid>? shopIds = null, CancellationToken ct = default)
        {
            try
            {
                _db.ZansiPulseRecommendationLogs.Add(new ZansiPulseRecommendationLog
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    RecommendationType = type,
                    Source = source,
                    ListingIdsJson = listingIds is null ? null : Json(listingIds.ToList()),
                    ShopIdsJson = shopIds is null ? null : Json(shopIds.ToList()),
                    CreatedAt = DateTime.UtcNow,
                });
                await _db.SaveChangesAsync(ct);
            }
            catch (Exception ex)
            {
                // Logging a recommendation is non-critical — never fail the
                // response because the audit write hiccupped.
                _logger.LogWarning(ex, "ZansiPulse: recommendation-log write failed ({Type}).", type);
            }
        }

        // ════════════════════════════════════════════════════════════════════
        // Settings + pure helpers
        // ════════════════════════════════════════════════════════════════════

        private async Task<Dictionary<string, string>> LoadSettingsAsync(CancellationToken ct)
        {
            // Build the dictionary manually (last-wins) rather than via
            // ToDictionaryAsync: a unique index guards Key today, but a key
            // collision must never be able to throw and 500 a user-facing flow.
            var rows = await _db.ZansiPulseSettings.AsNoTracking()
                .Where(s => s.IsActive)
                .Select(s => new { s.Key, s.Value })
                .ToListAsync(ct);
            var dict = new Dictionary<string, string>(rows.Count);
            foreach (var r in rows) dict[r.Key] = r.Value;
            return dict;
        }

        private static decimal ResolveEventWeight(ZansiPulseEventType type, Dictionary<string, string> settings)
        {
            var key = ZansiPulseDefaults.EventWeightKey(type);
            var fallback = ZansiPulseDefaults.EventWeights.TryGetValue(type, out var w) ? w : 0m;
            return GetDecimal(settings, key, fallback);
        }

        private RecWeights LoadRecWeights(Dictionary<string, string> settings) => new()
        {
            Interest = GetDecimal(settings, ZansiPulseDefaults.RecWeightUserInterestKey, ZansiPulseDefaults.RecUserInterestMatch),
            Location = GetDecimal(settings, ZansiPulseDefaults.RecWeightLocationKey, ZansiPulseDefaults.RecLocationMatch),
            Engagement = GetDecimal(settings, ZansiPulseDefaults.RecWeightEngagementKey, ZansiPulseDefaults.RecListingEngagement),
            SellerQuality = GetDecimal(settings, ZansiPulseDefaults.RecWeightSellerQualityKey, ZansiPulseDefaults.RecSellerQuality),
            Recency = GetDecimal(settings, ZansiPulseDefaults.RecWeightRecencyKey, ZansiPulseDefaults.RecRecency),
            PlatformBoost = GetDecimal(settings, ZansiPulseDefaults.RecWeightPlatformBoostKey, ZansiPulseDefaults.RecPlatformBoost),
        };

        private static decimal GetDecimal(Dictionary<string, string> settings, string key, decimal fallback)
            => settings.TryGetValue(key, out var raw) && decimal.TryParse(raw, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var v)
                ? v : fallback;

        private static string? Trim(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();

        private static decimal Clamp(decimal v, decimal min, decimal max) => v < min ? min : (v > max ? max : v);

        /// <summary>Asymptotic 0..100 score: value/(value+half) scaled to 100.</summary>
        private static decimal Saturate100(decimal value, decimal half)
            => value <= 0 ? 0m : Math.Round(100m * value / (value + half), 4);

        private static decimal ListingEngagementBase(ZansiPulseListingMetric m)
            => (m.TotalViews * 1m) + (m.TotalDetailOpens * 3m) + (m.TotalFavourites * 8m)
             + (m.TotalShares * 8m) + (m.TotalMessages * 12m) - (m.TotalReports * 30m);

        private static decimal DemandScore(int views, int searches, int favourites, int messages)
            => (views * 1m) + (searches * 4m) + (favourites * 8m) + (messages * 12m);

        private static decimal DecayedTrending(decimal baseScore, DateTime? lastEngagement, DateTime now)
        {
            if (baseScore <= 0m) return 0m;
            var anchor = lastEngagement ?? now;
            return Math.Round(baseScore * (decimal)RecencyMultiplier(anchor, now), 4);
        }

        private static double RecencyMultiplier(DateTime when, DateTime now)
        {
            var days = (now - when).TotalDays;
            if (days < 0) days = 0;
            return 1d / (1d + (days / RecencyHalfLifeDays));
        }

        private static decimal LocationFactor(string? userProvince, string? userCity, string? itemProvince, string? itemCity)
        {
            if (!string.IsNullOrWhiteSpace(userCity) && !string.IsNullOrWhiteSpace(itemCity)
                && string.Equals(userCity, itemCity, StringComparison.OrdinalIgnoreCase))
                return 1m;
            if (!string.IsNullOrWhiteSpace(userProvince) && !string.IsNullOrWhiteSpace(itemProvince)
                && string.Equals(userProvince, itemProvince, StringComparison.OrdinalIgnoreCase))
                return 0.6m;
            return 0m;
        }

        private static void ApplyDemandCounters(ZansiPulseEventType type, Action onView, Action onSearch, Action onFavourite, Action onMessage)
        {
            if (IsView(type)) onView();
            else if (IsSearch(type)) onSearch();
            if (type == ZansiPulseEventType.FavouriteListing) onFavourite();
            if (type == ZansiPulseEventType.MessageSeller) onMessage();
        }

        private static bool IsView(ZansiPulseEventType t) => t is ZansiPulseEventType.ViewListing or ZansiPulseEventType.OpenListingDetail;
        private static bool IsSearch(ZansiPulseEventType t) => t is ZansiPulseEventType.SearchCategory or ZansiPulseEventType.SearchTerm;
        private static bool IsPositiveEngagement(ZansiPulseEventType t)
            => t is ZansiPulseEventType.ViewListing or ZansiPulseEventType.OpenListingDetail
                 or ZansiPulseEventType.FavouriteListing or ZansiPulseEventType.ShareListing
                 or ZansiPulseEventType.MessageSeller or ZansiPulseEventType.OrderIntent;

        private static (DateTime start, DateTime end) DayBucket(DateTime now)
        {
            var start = new DateTime(now.Year, now.Month, now.Day, 0, 0, 0, DateTimeKind.Utc);
            return (start, start.AddDays(1));
        }

        private static DateTime LookbackStart(ZansiPulsePeriodType period, DateTime now) => period switch
        {
            ZansiPulsePeriodType.Hourly => now.AddHours(-1),
            ZansiPulsePeriodType.Daily => now.AddDays(-1),
            ZansiPulsePeriodType.Weekly => now.AddDays(-7),
            ZansiPulsePeriodType.Monthly => now.AddMonths(-1),
            _ => now.AddDays(-1),
        };

        private static string Json<T>(T value) => JsonSerializer.Serialize(value);

        private struct RecWeights
        {
            public decimal Interest;
            public decimal Location;
            public decimal Engagement;
            public decimal SellerQuality;
            public decimal Recency;
            public decimal PlatformBoost;
        }

        private sealed class EvtRow
        {
            public ZansiPulseEventType EventType { get; set; }
            public Guid? CategoryId { get; set; }
            public string? Province { get; set; }
            public string? City { get; set; }
            public string? SearchTerm { get; set; }
        }
    }
}
