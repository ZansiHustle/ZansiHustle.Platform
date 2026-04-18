using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ZansiHustle.Application.Admin.Affiliates;
using ZansiHustle.Shared.Queries;

namespace ZansiHustle.API.Controllers
{
    /// <summary>
    /// Admin Affiliates endpoints — top-level referral network view. Same
    /// role gate as the other admin modules.
    /// </summary>
    [Route("api/admin/affiliates")]
    [Authorize(Roles = "SuperAdmin,Admin,Partner,Accountant")]
    public class AdminAffiliatesController : BaseController
    {
        private readonly IAdminAffiliateService _service;

        public AdminAffiliatesController(IAdminAffiliateService service)
        {
            _service = service;
        }

        /// <summary>Affiliate KPI tiles.</summary>
        [HttpGet("kpis")]
        public async Task<IActionResult> GetKpis()
        {
            var result = await _service.GetKpisAsync();
            return ToActionResult(result);
        }

        /// <summary>Paged affiliate list. Supports status / search / date-range filters.</summary>
        [HttpGet]
        public async Task<IActionResult> GetAffiliates([FromQuery] PagedListQueryBase query)
        {
            var result = await _service.GetAffiliatesAsync(query);
            return ToActionResult(result);
        }

        /// <summary>Monthly performance trend — referrals, conversions, commission.</summary>
        [HttpGet("performance-trend")]
        public async Task<IActionResult> GetPerformanceTrend([FromQuery] int months = 7)
        {
            var result = await _service.GetPerformanceTrendAsync(months);
            return ToActionResult(result);
        }
    }
}
