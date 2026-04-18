using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ZansiHustle.Application.Analytics;

namespace ZansiHustle.API.Controllers
{
    /// <summary>
    /// Admin analytics endpoints — platform-wide KPIs and chart series consumed
    /// by the portal's Analytics page. Role gating mirrors Fundraising: partner
    /// admins and accountants can read, no one else.
    /// </summary>
    [Route("api/admin/analytics")]
    [Authorize(Roles = "SuperAdmin,Admin,Partner,Accountant")]
    public class AdminAnalyticsController : BaseController
    {
        private readonly IAnalyticsService _service;

        public AdminAnalyticsController(IAnalyticsService service)
        {
            _service = service;
        }

        /// <summary>KPI snapshot (GMV, order counts, customer metrics).</summary>
        [HttpGet("kpis")]
        public async Task<IActionResult> GetKpis()
        {
            var result = await _service.GetKpisAsync();
            return ToActionResult(result);
        }

        /// <summary>Monthly revenue trend for the last <paramref name="months"/> months (default 7).</summary>
        [HttpGet("revenue-trend")]
        public async Task<IActionResult> GetRevenueTrend([FromQuery] int months = 7)
        {
            var result = await _service.GetRevenueTrendAsync(months);
            return ToActionResult(result);
        }

        /// <summary>Per-province aggregate (sellers, shops, paid revenue).</summary>
        [HttpGet("regions")]
        public async Task<IActionResult> GetRegions()
        {
            var result = await _service.GetRegionBreakdownAsync();
            return ToActionResult(result);
        }

        /// <summary>Seller-category revenue share.</summary>
        [HttpGet("categories")]
        public async Task<IActionResult> GetCategories()
        {
            var result = await _service.GetCategoryBreakdownAsync();
            return ToActionResult(result);
        }
    }
}
