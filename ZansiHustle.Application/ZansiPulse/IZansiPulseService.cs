using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ZansiHustle.Application.ZansiPulse.Dtos;
using ZansiHustle.Shared.Enums.ZansiPulse;
using ZansiHustle.Shared.Results;

namespace ZansiHustle.Application.ZansiPulse
{
    /// <summary>
    /// ZansiPulse — the ZansiHustle intelligence layer. Phase 1 contract:
    /// deterministic event capture, interest scoring, recommendations,
    /// trending, supply/demand analysis and dashboard rollups. No AI/ML and
    /// no randomness — every output is reproducible from the data. The shape
    /// is built so a real model can slot in behind these same methods later.
    ///
    /// All methods return the standard <see cref="Result"/> envelope and never
    /// throw to the caller — failures are logged and surfaced as
    /// <c>EXCEPTION</c> results.
    /// </summary>
    public interface IZansiPulseService
    {
        // ── Event tracking ──────────────────────────────────────────────────
        /// <summary>
        /// Records a user interaction as a <c>ZansiPulseEvent</c> and updates
        /// the affected metrics + interest score in the same unit of work.
        /// <paramref name="userId"/> is null for anonymous/system signals.
        /// Returns the new event id.
        /// </summary>
        Task<Result<Guid>> TrackEventAsync(Guid? userId, TrackEventRequestDto request, CancellationToken ct = default);

        // ── User interests ──────────────────────────────────────────────────
        Task<Result<List<UserInterestScoreDto>>> GetMyInterestsAsync(Guid userId, CancellationToken ct = default);
        Task<Result<List<UserInterestScoreDto>>> SaveOnboardingInterestsAsync(Guid userId, SaveInterestsRequestDto request, CancellationToken ct = default);

        // ── Recommendations ─────────────────────────────────────────────────
        Task<Result<List<RecommendedListingDto>>> GetListingRecommendationsAsync(Guid userId, int take, CancellationToken ct = default);
        Task<Result<List<RecommendedShopDto>>> GetShopRecommendationsAsync(Guid userId, int take, CancellationToken ct = default);

        // ── Trending ────────────────────────────────────────────────────────
        Task<Result<List<TrendingListingDto>>> GetTrendingListingsAsync(TrendingListingFilterDto filter, CancellationToken ct = default);
        Task<Result<List<TrendingCategoryDto>>> GetTrendingCategoriesAsync(ZansiPulsePeriodType period, string? province, string? city, int take, CancellationToken ct = default);
        Task<Result<List<TrendingRegionDto>>> GetTrendingRegionsAsync(ZansiPulsePeriodType period, int take, CancellationToken ct = default);

        // ── Dashboard (portal / CEO admin) ──────────────────────────────────
        Task<Result<DashboardOverviewDto>> GetDashboardOverviewAsync(ZansiPulsePeriodType period, int take, CancellationToken ct = default);
        Task<Result<List<SupplyDemandGapDto>>> GetSupplyDemandAsync(string? province, string? city, int take, CancellationToken ct = default);
        Task<Result<SnapshotResultDto>> RunSnapshotAsync(ZansiPulsePeriodType snapshotType, CancellationToken ct = default);
    }
}
