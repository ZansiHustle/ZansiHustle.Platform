using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ZansiHustle.Application.Common.Interfaces.Shared;
using ZansiHustle.Application.ZansiPulse;
using ZansiHustle.Application.ZansiPulse.Dtos;
using ZansiHustle.Shared.Enums.ZansiPulse;
using ZansiHustle.Shared.Errors;
using ZansiHustle.Shared.Results;

namespace ZansiHustle.API.Controllers
{
    /// <summary>
    /// ZansiPulse — the ZansiHustle intelligence layer. Phase 1 surface:
    /// event tracking, user interests, personalized recommendations,
    /// trending, and the CEO/admin dashboard.
    ///
    /// All endpoints require authentication (class-level <c>[Authorize]</c>).
    /// User endpoints act on the signed-in caller; dashboard / snapshot
    /// endpoints additionally require an internal role.
    /// </summary>
    [ApiController]
    [Authorize]
    [Route("api/zansipulse")]
    public class ZansiPulseController : BaseController
    {
        // Internal roles allowed to read dashboard analytics — mirrors the
        // admin-read set used by UsersController.
        private const string DashboardRoles = "SuperAdmin,Admin,Partner,TeamManager";
        // Tighter set for the write/recompute action.
        private const string SnapshotRoles = "SuperAdmin,Admin";

        private readonly IZansiPulseService _pulse;
        private readonly ICurrentUserService _currentUser;

        public ZansiPulseController(IZansiPulseService pulse, ICurrentUserService currentUser)
        {
            _pulse = pulse;
            _currentUser = currentUser;
        }

        private bool TryGetUserId(out Guid userId)
        {
            userId = _currentUser.UserId ?? Guid.Empty;
            return userId != Guid.Empty;
        }

        // ── Event tracking ──────────────────────────────────────────────────

        /// <summary>
        /// Tracks a user interaction (view, search, favourite, share, message,
        /// profile open, order intent, hide/not-interested, report). Records a
        /// ZansiPulseEvent and updates derived metrics + interest scores.
        /// </summary>
        [HttpPost("events/track")]
        public async Task<IActionResult> TrackEvent([FromBody] TrackEventRequestDto request, CancellationToken ct)
        {
            if (!TryGetUserId(out var userId))
                return ToActionResult(Result<Guid>.Failure(ErrorCodes.Unauthorized, "Sign in to track activity."));
            return ToActionResult(await _pulse.TrackEventAsync(userId, request, ct));
        }

        // ── User interests ──────────────────────────────────────────────────

        /// <summary>Returns the signed-in user's current interest scores.</summary>
        [HttpGet("me/interests")]
        public async Task<IActionResult> GetMyInterests(CancellationToken ct)
        {
            if (!TryGetUserId(out var userId))
                return ToActionResult(Result<List<UserInterestScoreDto>>.Failure(ErrorCodes.Unauthorized, "Sign in to view your interests."));
            return ToActionResult(await _pulse.GetMyInterestsAsync(userId, ct));
        }

        /// <summary>Saves onboarding-selected interests as strong starting scores.</summary>
        [HttpPost("me/interests")]
        public async Task<IActionResult> SaveMyInterests([FromBody] SaveInterestsRequestDto request, CancellationToken ct)
        {
            if (!TryGetUserId(out var userId))
                return ToActionResult(Result<List<UserInterestScoreDto>>.Failure(ErrorCodes.Unauthorized, "Sign in to save your interests."));
            return ToActionResult(await _pulse.SaveOnboardingInterestsAsync(userId, request, ct));
        }

        // ── Recommendations ─────────────────────────────────────────────────

        /// <summary>Personalized recommended listings for the signed-in user.</summary>
        [HttpGet("recommendations/listings")]
        public async Task<IActionResult> RecommendListings([FromQuery] int take = 20, CancellationToken ct = default)
        {
            if (!TryGetUserId(out var userId))
                return ToActionResult(Result<List<RecommendedListingDto>>.Failure(ErrorCodes.Unauthorized, "Sign in for recommendations."));
            return ToActionResult(await _pulse.GetListingRecommendationsAsync(userId, take, ct));
        }

        /// <summary>Personalized recommended shops for the signed-in user.</summary>
        [HttpGet("recommendations/shops")]
        public async Task<IActionResult> RecommendShops([FromQuery] int take = 20, CancellationToken ct = default)
        {
            if (!TryGetUserId(out var userId))
                return ToActionResult(Result<List<RecommendedShopDto>>.Failure(ErrorCodes.Unauthorized, "Sign in for recommendations."));
            return ToActionResult(await _pulse.GetShopRecommendationsAsync(userId, take, ct));
        }

        // ── Trending ────────────────────────────────────────────────────────

        /// <summary>Trending listings, optionally filtered by region / category / period.</summary>
        [HttpGet("trending/listings")]
        public async Task<IActionResult> TrendingListings(
            [FromQuery] string? province,
            [FromQuery] string? city,
            [FromQuery] Guid? categoryId,
            [FromQuery] ZansiPulsePeriodType? period,
            [FromQuery] int take = 20,
            CancellationToken ct = default)
        {
            var filter = new TrendingListingFilterDto
            {
                Province = province,
                City = city,
                CategoryId = categoryId,
                Period = period,
                Take = take,
            };
            return ToActionResult(await _pulse.GetTrendingListingsAsync(filter, ct));
        }

        /// <summary>Trending categories for a period, optionally filtered by region.</summary>
        [HttpGet("trending/categories")]
        public async Task<IActionResult> TrendingCategories(
            [FromQuery] ZansiPulsePeriodType period = ZansiPulsePeriodType.Daily,
            [FromQuery] string? province = null,
            [FromQuery] string? city = null,
            [FromQuery] int take = 20,
            CancellationToken ct = default)
        {
            return ToActionResult(await _pulse.GetTrendingCategoriesAsync(period, province, city, take, ct));
        }

        /// <summary>Trending regions for a period.</summary>
        [HttpGet("trending/regions")]
        public async Task<IActionResult> TrendingRegions(
            [FromQuery] ZansiPulsePeriodType period = ZansiPulsePeriodType.Daily,
            [FromQuery] int take = 20,
            CancellationToken ct = default)
        {
            return ToActionResult(await _pulse.GetTrendingRegionsAsync(period, take, ct));
        }

        // ── Dashboard (portal / CEO admin) ──────────────────────────────────

        /// <summary>Basic CEO/admin dashboard summary for a period.</summary>
        [HttpGet("dashboard/overview")]
        [Authorize(Roles = DashboardRoles)]
        public async Task<IActionResult> DashboardOverview(
            [FromQuery] ZansiPulsePeriodType period = ZansiPulsePeriodType.Daily,
            [FromQuery] int take = 10,
            CancellationToken ct = default)
        {
            return ToActionResult(await _pulse.GetDashboardOverviewAsync(period, take, ct));
        }

        /// <summary>Supply vs demand gaps based on demand signals vs active supply.</summary>
        [HttpGet("dashboard/supply-demand")]
        [Authorize(Roles = DashboardRoles)]
        public async Task<IActionResult> SupplyDemand(
            [FromQuery] string? province = null,
            [FromQuery] string? city = null,
            [FromQuery] int take = 20,
            CancellationToken ct = default)
        {
            return ToActionResult(await _pulse.GetSupplyDemandAsync(province, city, take, ct));
        }

        /// <summary>Generates / refreshes the dashboard snapshot for a period.</summary>
        [HttpPost("admin/run-snapshot")]
        [Authorize(Roles = SnapshotRoles)]
        public async Task<IActionResult> RunSnapshot(
            [FromQuery] ZansiPulsePeriodType snapshotType = ZansiPulsePeriodType.Daily,
            CancellationToken ct = default)
        {
            return ToActionResult(await _pulse.RunSnapshotAsync(snapshotType, ct));
        }
    }
}
