using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ZansiHustle.Application.Common.Interfaces.Shared;
using ZansiHustle.Application.Reports;
using ZansiHustle.Application.Reports.Dtos;
using ZansiHustle.Shared.Errors;
using ZansiHustle.Shared.Results;

namespace ZansiHustle.API.Controllers
{
    /// <summary>
    /// User-facing content reporting (App Store Guideline 1.2). Any signed-in
    /// user can report objectionable content; reports land in the admin
    /// moderation queue (see <c>AdminReportsController</c>).
    /// </summary>
    [Route("api/reports")]
    [Authorize]
    public class ReportsController : BaseController
    {
        private readonly IReportService _reportService;
        private readonly ICurrentUserService _currentUserService;

        public ReportsController(IReportService reportService, ICurrentUserService currentUserService)
        {
            _reportService = reportService;
            _currentUserService = currentUserService;
        }

        /// <summary>Report a piece of user-generated content.</summary>
        [HttpPost]
        [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status200OK)]
        public async Task<IActionResult> Create([FromBody] CreateReportRequestDto request)
        {
            var userId = _currentUserService.UserId;
            if (!userId.HasValue)
                return ToActionResult(Result<Guid>.Failure(ErrorCodes.Unauthorized, "User identifier not found in token."));

            var result = await _reportService.CreateAsync(userId.Value, request);
            return ToActionResult(result);
        }
    }
}
