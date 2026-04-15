using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using ZansiHustle.Application.Admin.Seeding;
using ZansiHustle.Shared.Errors;
using ZansiHustle.Shared.Results;

namespace ZansiHustle.API.Controllers
{
    /// <summary>
    /// Admin-only operations: platform seeding, diagnostic endpoints, etc.
    /// All routes are restricted by role and blocked in Production.
    /// </summary>
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public class AdminController : BaseController
    {
        private readonly IUatSeederService _uatSeeder;
        private readonly IWebHostEnvironment _env;

        public AdminController(IUatSeederService uatSeeder, IWebHostEnvironment env)
        {
            _uatSeeder = uatSeeder;
            _env = env;
        }

        /// <summary>
        /// Seeds realistic UAT/dev data (users, merchants, listings, orders).
        /// Idempotent — existing records are detected by code/slug/email and skipped.
        /// Blocked in Production.
        /// </summary>
        [HttpPost("seed/uat")]
        [ProducesResponseType(typeof(Result<UatSeedSummaryDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> SeedUat(CancellationToken cancellationToken)
        {
            if (_env.IsProduction())
                return ToActionResult(Result<UatSeedSummaryDto>.Failure(ErrorCodes.Forbidden, "UAT seeding is disabled in Production."));

            var result = await _uatSeeder.SeedAllAsync(cancellationToken);
            return ToActionResult(result);
        }
    }
}
