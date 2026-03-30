using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using ZansiHustle.Application.Dashboard;

namespace ZansiHustle.API.Controllers
{
    /// <summary>
    /// Exposes dashboard summary and analytics endpoints.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class DashboardController : ControllerBase
    {
        private readonly ILaunchOpsDashboardService _launchOpsDashboardService;
        private readonly IMarketingDashboardService _marketingDashboardService;

        /// <summary>
        /// Creates a new instance of the <see cref="DashboardController"/> class.
        /// </summary>
        public DashboardController(ILaunchOpsDashboardService launchOpsDashboardService, IMarketingDashboardService marketingDashboardService)
        {
            _launchOpsDashboardService = launchOpsDashboardService;
            _marketingDashboardService = marketingDashboardService;
        }

        [HttpGet("launch-ops-summary")]
        public async Task<IActionResult> GetLaunchOpsSummary()
        {
            var result = await _launchOpsDashboardService.GetSummaryAsync();
            return Ok(result);
        }

        [HttpGet("seller-lead-province-distribution")]
        public async Task<IActionResult> GetSellerLeadProvinceDistribution()
        {
            var result = await _launchOpsDashboardService.GetSellerLeadProvinceDistributionAsync();
            return Ok(result);
        }

        [HttpGet("team-activity")]
        public async Task<IActionResult> GetTeamActivity()
        {
            var result = await _launchOpsDashboardService.GetTeamActivityAsync();
            return Ok(result);
        }

        [HttpGet("marketing-summary")]
        public async Task<IActionResult> GetMarketingSummary()
        {
            var result = await _marketingDashboardService.GetMarketingSummaryAsync();
            return Ok(result);
        }

        [HttpGet("social-summary")]
        public async Task<IActionResult> GetSocialSummary()
        {
            var result = await _marketingDashboardService.GetSocialMediaSummaryAsync();
            return Ok(result);
        }

        [HttpGet("budget-summary")]
        public async Task<IActionResult> GetBudgetSummary()
        {
            var result = await _marketingDashboardService.GetBudgetSummaryAsync();
            return Ok(result);
        }

        [HttpGet("influencer-platform-distribution")]
        public async Task<IActionResult> GetInfluencerPlatformDistribution()
        {
            var result = await _marketingDashboardService.GetInfluencerPlatformDistributionAsync();
            return Ok(result);
        }

        [HttpGet("influencer-province-distribution")]
        public async Task<IActionResult> GetInfluencerProvinceDistribution()
        {
            var result = await _marketingDashboardService.GetInfluencerProvinceDistributionAsync();
            return Ok(result);
        }

        [HttpGet("campaign-performance")]
        public async Task<IActionResult> GetCampaignPerformance()
        {
            var result = await _marketingDashboardService.GetCampaignPerformanceAsync();
            return Ok(result);
        }

        [HttpGet("campaign-trends")]
        public async Task<IActionResult> GetCampaignTrends()
        {
            var result = await _marketingDashboardService.GetCampaignTrendsAsync();
            return Ok(result);
        }
    }
}
