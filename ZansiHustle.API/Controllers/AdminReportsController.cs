using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ZansiHustle.Application.Common.Interfaces.Shared;
using ZansiHustle.Application.Reports;
using ZansiHustle.Application.Reports.Dtos;
using ZansiHustle.Shared.Enums.Reports;
using ZansiHustle.Shared.Errors;
using ZansiHustle.Shared.Results;

namespace ZansiHustle.API.Controllers
{
    /// <summary>
    /// Admin moderation queue for user content reports (App Store Guideline 1.2:
    /// the platform must be able to view reports and act on them).
    /// </summary>
    [Route("api/admin/reports")]
    [Authorize(Roles = "SuperAdmin,Admin,Partner,Support,Moderator")]
    public class AdminReportsController : BaseController
    {
        private readonly IReportService _reportService;
        private readonly ICurrentUserService _currentUserService;

        public AdminReportsController(IReportService reportService, ICurrentUserService currentUserService)
        {
            _reportService = reportService;
            _currentUserService = currentUserService;
        }

        /// <summary>Paged moderation queue. Optional <c>status</c> filter.</summary>
        [HttpGet]
        [ProducesResponseType(typeof(Result<ReportPageDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> List(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 25,
            [FromQuery] ReportStatus? status = null)
        {
            var result = await _reportService.ListAsync(page, pageSize, status);
            return ToActionResult(result);
        }

        /// <summary>Resolve a report (ActionTaken / Dismissed) with a note.</summary>
        [HttpPost("{id:guid}/resolve")]
        [ProducesResponseType(typeof(Result), StatusCodes.Status200OK)]
        public async Task<IActionResult> Resolve(Guid id, [FromBody] ResolveReportRequestDto request)
        {
            var moderatorId = _currentUserService.UserId;
            if (!moderatorId.HasValue)
                return ToActionResult(Result.Failure(ErrorCodes.Unauthorized, "User identifier not found in token."));

            var result = await _reportService.ResolveAsync(id, moderatorId.Value, request);
            return ToActionResult(result);
        }
    }
}
