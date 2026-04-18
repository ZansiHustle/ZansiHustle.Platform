using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ZansiHustle.Application.Admin.Payments;

namespace ZansiHustle.API.Controllers
{
    /// <summary>
    /// Admin Payments endpoints — platform-wide revenue and payout views.
    /// Distinct from the existing <see cref="PaymentsController"/>, which
    /// handles Paystack init/verify/webhook for end-user checkout.
    /// </summary>
    [Route("api/admin/payments")]
    [Authorize(Roles = "SuperAdmin,Admin,Partner,Accountant")]
    public class AdminPaymentsController : BaseController
    {
        private readonly IAdminPaymentService _service;

        public AdminPaymentsController(IAdminPaymentService service)
        {
            _service = service;
        }

        /// <summary>Payment KPI tiles (paid / pending / failed / refunded totals + counts).</summary>
        [HttpGet("kpis")]
        public async Task<IActionResult> GetKpis()
        {
            var result = await _service.GetKpisAsync();
            return ToActionResult(result);
        }

        /// <summary>Merchant payout queue. Returns an empty list today — see repository.</summary>
        [HttpGet("payout-queue")]
        public async Task<IActionResult> GetPayoutQueue([FromQuery] int limit = 100)
        {
            var result = await _service.GetPayoutQueueAsync(limit);
            return ToActionResult(result);
        }
    }
}
