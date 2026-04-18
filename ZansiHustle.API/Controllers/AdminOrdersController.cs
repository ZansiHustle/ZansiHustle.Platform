using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ZansiHustle.Application.Admin.Orders;
using ZansiHustle.Shared.Queries;

namespace ZansiHustle.API.Controllers
{
    /// <summary>
    /// Cross-merchant admin Orders endpoints. Distinct from <see cref="OrdersController"/>
    /// which scopes to the current user (buyer or seller); this one powers the
    /// platform-wide admin grid.
    /// </summary>
    [Route("api/admin/orders")]
    [Authorize(Roles = "SuperAdmin,Admin,Partner,Accountant")]
    public class AdminOrdersController : BaseController
    {
        private readonly IAdminOrderService _service;

        public AdminOrdersController(IAdminOrderService service)
        {
            _service = service;
        }

        /// <summary>Paged orders list (most recent first). Supports status / search / date-range filters.</summary>
        [HttpGet]
        public async Task<IActionResult> GetOrders([FromQuery] PagedListQueryBase query)
        {
            var result = await _service.GetOrdersAsync(query);
            return ToActionResult(result);
        }

        /// <summary>Order KPI tiles (today / week / month counts + AOV).</summary>
        [HttpGet("kpis")]
        public async Task<IActionResult> GetKpis()
        {
            var result = await _service.GetKpisAsync();
            return ToActionResult(result);
        }
    }
}
