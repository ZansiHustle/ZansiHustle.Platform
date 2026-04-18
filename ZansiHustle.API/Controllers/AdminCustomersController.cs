using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ZansiHustle.Application.Admin.Customers;
using ZansiHustle.Shared.Queries;

namespace ZansiHustle.API.Controllers
{
    /// <summary>
    /// Admin Customers endpoints — customers are derived from Orders by
    /// grouping on BuyerUserId. Same role gating as the other admin dashboards.
    /// </summary>
    [Route("api/admin/customers")]
    [Authorize(Roles = "SuperAdmin,Admin,Partner,Accountant")]
    public class AdminCustomersController : BaseController
    {
        private readonly IAdminCustomerService _service;

        public AdminCustomersController(IAdminCustomerService service)
        {
            _service = service;
        }

        /// <summary>Paged customer list (most recent activity first). Supports status / search / date-range filters.</summary>
        [HttpGet]
        public async Task<IActionResult> GetCustomers([FromQuery] PagedListQueryBase query)
        {
            var result = await _service.GetCustomersAsync(query);
            return ToActionResult(result);
        }

        /// <summary>Customer KPI tiles.</summary>
        [HttpGet("kpis")]
        public async Task<IActionResult> GetKpis()
        {
            var result = await _service.GetKpisAsync();
            return ToActionResult(result);
        }
    }
}
