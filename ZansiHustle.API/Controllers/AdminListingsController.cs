using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ZansiHustle.Application.Admin.Listings;
using ZansiHustle.Application.Admin.Listings.Dtos;

namespace ZansiHustle.API.Controllers
{
    /// <summary>
    /// Cross-merchant admin Listings endpoints. Distinct from the buyer-facing
    /// listings controller (which hard-filters to Active / visible / online
    /// items); this one powers the platform-wide admin grid and returns ALL
    /// listings regardless of status, source, availability, or visibility.
    /// </summary>
    [Route("api/admin/listings")]
    [Authorize(Roles = "SuperAdmin,Admin,Partner,Accountant")]
    public class AdminListingsController : BaseController
    {
        private readonly IAdminListingService _service;

        public AdminListingsController(IAdminListingService service)
        {
            _service = service;
        }

        /// <summary>Paged listings list (most recent first). Supports status / type / source / search / date-range filters.</summary>
        [HttpGet]
        public async Task<IActionResult> GetListings([FromQuery] AdminListingQuery query)
        {
            var result = await _service.GetListingsAsync(query);
            return ToActionResult(result);
        }

        /// <summary>Listing KPI tiles (total / products / services / active / draft counts).</summary>
        [HttpGet("kpis")]
        public async Task<IActionResult> GetKpis()
        {
            var result = await _service.GetKpisAsync();
            return ToActionResult(result);
        }
    }
}
