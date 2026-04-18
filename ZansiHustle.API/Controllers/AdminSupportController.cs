using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ZansiHustle.Application.Admin.Support;
using ZansiHustle.Shared.Queries;

namespace ZansiHustle.API.Controllers
{
    /// <summary>
    /// Admin Support endpoints — ticket queue, disputes, agent roster, and
    /// dashboard series. Distinct from <see cref="SupportController"/> which
    /// handles the public contact form.
    /// </summary>
    [Route("api/admin/support")]
    [Authorize(Roles = "SuperAdmin,Admin,Partner,Accountant")]
    public class AdminSupportController : BaseController
    {
        private readonly IAdminSupportService _service;

        public AdminSupportController(IAdminSupportService service)
        {
            _service = service;
        }

        /// <summary>Support KPI tiles (totals, open/resolved, SLA, CSAT, resolution time).</summary>
        [HttpGet("kpis")]
        public async Task<IActionResult> GetKpis()
        {
            var result = await _service.GetKpisAsync();
            return ToActionResult(result);
        }

        /// <summary>Paged ticket queue. Supports status / priority / search / date-range filters.</summary>
        [HttpGet("tickets")]
        public async Task<IActionResult> GetTickets([FromQuery] TicketsListQuery query)
        {
            var result = await _service.GetTicketsAsync(query);
            return ToActionResult(result);
        }

        /// <summary>Paged dispute queue. Supports status / search / date-range filters.</summary>
        [HttpGet("disputes")]
        public async Task<IActionResult> GetDisputes([FromQuery] PagedListQueryBase query)
        {
            var result = await _service.GetDisputesAsync(query);
            return ToActionResult(result);
        }

        /// <summary>Support agent roster.</summary>
        [HttpGet("agents")]
        public async Task<IActionResult> GetAgents()
        {
            var result = await _service.GetAgentsAsync();
            return ToActionResult(result);
        }

        /// <summary>Backlog trend (opened / resolved / escalated per day).</summary>
        [HttpGet("backlog-trend")]
        public async Task<IActionResult> GetBacklogTrend([FromQuery] int days = 14)
        {
            var result = await _service.GetBacklogTrendAsync(days);
            return ToActionResult(result);
        }

        /// <summary>Recent escalation events feed.</summary>
        [HttpGet("recent-escalations")]
        public async Task<IActionResult> GetRecentEscalations([FromQuery] int limit = 10)
        {
            var result = await _service.GetRecentEscalationsAsync(limit);
            return ToActionResult(result);
        }
    }
}
