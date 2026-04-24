using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ZansiHustle.Application.Admin.Users;
using ZansiHustle.Application.Admin.Users.Dtos;
using ZansiHustle.Application.Common.Paging;
using ZansiHustle.Shared.Results;

namespace ZansiHustle.API.Controllers
{
    /// <summary>
    /// Admin-only global Users view. Exposes a paginated / filterable
    /// directory of every user in the system (buyers, merchants,
    /// agents, team, admin) with their roles and merchant summary.
    ///
    /// Access control: SuperAdmin / Admin / Partner / TeamManager. An
    /// Agent, Merchant, or Buyer must never hit these endpoints — the
    /// payload enumerates the full user base and leaking it to
    /// non-internal roles would be a major compliance issue.
    /// </summary>
    [Route("api/users")]
    [Authorize(Roles = "SuperAdmin,Admin,Partner,TeamManager")]
    public class UsersController : BaseController
    {
        private readonly IAdminUserService _service;

        public UsersController(IAdminUserService service)
        {
            _service = service;
        }

        /// <summary>
        /// Paged list of users with filters. Supports:
        /// <c>?userType=Admin|TeamMember|Agent|Merchant|Buyer</c>,
        /// <c>?merchantType=Seller|ServiceProvider|StoreOwner</c>
        /// (only meaningful with userType=Merchant),
        /// <c>?role=&lt;any Identity role name&gt;</c>,
        /// <c>?status=active|suspended|inactive</c>,
        /// <c>?search=&lt;name|email|phone&gt;</c>, plus <c>page</c> and
        /// <c>pageSize</c> (clamped to 200).
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(Result<PagedResult<UserListItemDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetUsers([FromQuery] UserQueryRequestDto query)
        {
            var result = await _service.GetUsersAsync(query);
            return ToActionResult(result);
        }

        /// <summary>Audience-split KPI tiles: totals per userType bucket.</summary>
        [HttpGet("kpis")]
        [ProducesResponseType(typeof(Result<UsersKpisDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetKpis()
        {
            var result = await _service.GetKpisAsync();
            return ToActionResult(result);
        }

        /// <summary>
        /// Full detail for a single user. Uses the same classification +
        /// merchant summary as the list view so callers can drill in
        /// without having to re-derive the user's audience bucket.
        /// </summary>
        [HttpGet("{id:guid}")]
        [ProducesResponseType(typeof(Result<UserListItemDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetById(Guid id)
        {
            var result = await _service.GetByIdAsync(id);
            return ToActionResult(result);
        }
    }
}
