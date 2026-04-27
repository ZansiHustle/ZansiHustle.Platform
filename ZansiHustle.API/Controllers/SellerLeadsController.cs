using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using ZansiHustle.Application.SellerLeads;
using ZansiHustle.Application.SellerLeads.Dtos;
using ZansiHustle.Shared.Results;

namespace ZansiHustle.API.Controllers
{
    /// <summary>
    /// Exposes endpoints for seller lead management.
    ///
    /// Per-action authorization (the controller-level <c>[Authorize]</c>
    /// keeps it logged-in-only for the broad surface so Agents can
    /// self-submit leads via POST + read /mine — but every elevated
    /// management action carries an explicit role allow-list).
    ///
    /// The management allow-list is the same as <c>AgentsController</c>:
    /// SuperAdmin, Admin, Partner, MarketplaceGrowthAssociate,
    /// TeamManager. Agents themselves are intentionally NOT in this
    /// list — they may create/read their own leads but never approve,
    /// verify, convert, or delete leads.
    /// </summary>
    [ApiController]
    [Authorize]
    [Route("api/[controller]")]
    public class SellerLeadsController : ControllerBase
    {
        private const string LeadOpsRoles = "SuperAdmin,Admin,Partner,MarketplaceGrowthAssociate,TeamManager";

        private readonly ISellerLeadService _sellerLeadService;

        /// <summary>
        /// Creates a new instance of the <see cref="SellerLeadsController"/> class.
        /// </summary>
        public SellerLeadsController(ISellerLeadService sellerLeadService)
        {
            _sellerLeadService = sellerLeadService;
        }

        /// <summary>
        /// Gets all seller leads. Admin-tier + Marketplace Growth only —
        /// returning every lead in the system to a logged-in Agent
        /// would leak other agents' pipelines.
        /// </summary>
        [HttpGet]
        [Authorize(Roles = LeadOpsRoles)]
        public async Task<IActionResult> GetAll()
        {
            var result = await _sellerLeadService.GetAllAsync();

            return Ok(result);
        }

        /// <summary>
        /// Gets the signed-in user's own seller leads (most recent first).
        /// Used by the merchant portal to gate /merchant/onboarding and
        /// surface "application pending" state.
        /// </summary>
        [HttpGet("mine")]
        public async Task<IActionResult> GetMine()
        {
            var result = await _sellerLeadService.GetMineAsync();

            return Ok(result);
        }

        /// <summary>
        /// Gets a seller lead by identifier. Admin-tier + Marketplace
        /// Growth only — agents see their own leads via /mine, never
        /// arbitrary IDs.
        /// </summary>
        [HttpGet("{id:guid}")]
        [Authorize(Roles = LeadOpsRoles)]
        public async Task<IActionResult> GetById(Guid id)
        {
            var result = await _sellerLeadService.GetByIdAsync(id);

            return Ok(result);
        }

        /// <summary>
        /// Creates a new seller lead.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateSellerLeadRequestDto request)
        {
            var result = await _sellerLeadService.CreateAsync(request);

            return Ok(result);
        }

        /// <summary>
        /// Creates a new seller lead from public website (no authentication required).
        /// </summary>
        [HttpPost("public")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(Result<SellerLeadDetailsDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> CreatePublic([FromBody] CreatePublicSellerLeadRequestDto request)
        {
            var result = await _sellerLeadService.CreatePublicAsync(request);

            return Ok(result);
        }

        /// <summary>
        /// Updates an existing seller lead. Admin-tier + Marketplace Growth.
        /// </summary>
        [HttpPut("{id:guid}")]
        [Authorize(Roles = LeadOpsRoles)]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateSellerLeadRequestDto request)
        {
            var result = await _sellerLeadService.UpdateAsync(id, request);

            return Ok(result);
        }

        /// <summary>
        /// Reviews a seller lead — this is the approve/reject path that
        /// triggers agent crediting downstream. Admin-tier + Marketplace
        /// Growth only; agents must never approve their own pipeline.
        /// </summary>
        [HttpPut("{id:guid}/review")]
        [Authorize(Roles = LeadOpsRoles)]
        public async Task<IActionResult> Review(Guid id, [FromBody] ReviewSellerLeadRequestDto request)
        {
            var result = await _sellerLeadService.ReviewAsync(id, request);

            return Ok(result);
        }

        /// <summary>
        /// Updates seller lead verification status. Admin-tier + Marketplace Growth.
        /// </summary>
        [HttpPut("{id:guid}/verify")]
        [Authorize(Roles = LeadOpsRoles)]
        public async Task<IActionResult> Verify(Guid id, [FromBody] VerifySellerLeadRequestDto request)
        {
            var result = await _sellerLeadService.VerifyAsync(id, request);

            return Ok(result);
        }

        /// <summary>
        /// Converts a seller lead to a live seller reference. Admin-tier
        /// + Marketplace Growth only — this mutation creates merchant
        /// records.
        /// </summary>
        [HttpPut("{id:guid}/convert")]
        [Authorize(Roles = LeadOpsRoles)]
        public async Task<IActionResult> Convert(Guid id, [FromBody] ConvertSellerLeadRequestDto request)
        {
            var result = await _sellerLeadService.ConvertAsync(id, request);

            return Ok(result);
        }

        /// <summary>
        /// Deletes a seller lead. Admin-tier + Marketplace Growth.
        /// </summary>
        [HttpDelete("{id:guid}")]
        [Authorize(Roles = LeadOpsRoles)]
        public async Task<IActionResult> Delete(Guid id)
        {
            var result = await _sellerLeadService.DeleteAsync(id);

            return Ok(result);
        }
    }
}
