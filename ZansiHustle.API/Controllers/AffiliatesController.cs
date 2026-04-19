using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ZansiHustle.Application.Common.Interfaces.Shared;
using ZansiHustle.Application.Referrals;
using ZansiHustle.Application.Referrals.Dtos;

namespace ZansiHustle.API.Controllers
{
    /// <summary>
    /// Standalone referral / affiliate system endpoints. Mounted under
    /// /api/affiliates for product clarity (the table is AffiliateProfiles
    /// and the user-facing language is "affiliate").
    ///
    /// Three audience layers in one controller:
    ///  - "/me" — authenticated user (any role) gets/creates their profile
    ///  - "/resolve" + "/clicks" — public, no auth, used by /join/{slug}
    ///  - "/referrals" — authenticated user records their own join attribution
    ///  - "/{id}" — admin lookup
    /// </summary>
    [Route("api/affiliates")]
    public class AffiliatesController : BaseController
    {
        private readonly IReferralService _service;
        private readonly ICurrentUserService _currentUser;

        public AffiliatesController(IReferralService service, ICurrentUserService currentUser)
        {
            _service = service;
            _currentUser = currentUser;
        }

        /// <summary>
        /// Returns the signed-in user's affiliate profile, creating one with
        /// a generated slug on first call. Any authenticated user can have a
        /// profile — affiliate-eligibility (whether they're surfaced as an
        /// agent, etc.) is a separate role concern.
        /// </summary>
        [HttpGet("me")]
        [Authorize]
        public async Task<IActionResult> GetMine()
        {
            var result = await _service.GetOrCreateMineAsync();
            return ToActionResult(result);
        }

        /// <summary>Admin lookup by id.</summary>
        [HttpGet("{id:guid}")]
        [Authorize(Roles = "SuperAdmin,Admin,Partner")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var result = await _service.GetByIdAsync(id);
            return ToActionResult(result);
        }

        /// <summary>
        /// Public — resolve a /join/{slug} to its affiliate metadata. Used
        /// by the join landing page to prefill+lock the registration form.
        /// </summary>
        [HttpGet("resolve/{slug}")]
        [AllowAnonymous]
        public async Task<IActionResult> Resolve(string slug)
        {
            var result = await _service.ResolveBySlugAsync(slug);
            return ToActionResult(result);
        }

        /// <summary>
        /// Public — record a click against an affiliate code. Always returns
        /// success even on unknown codes so the public endpoint doesn't leak
        /// which slugs exist.
        /// </summary>
        [HttpPost("clicks")]
        [AllowAnonymous]
        public async Task<IActionResult> RecordClick([FromBody] RecordReferralClickRequestDto request)
        {
            var ipHash = ReferralService.HashIp(GetClientIp());
            var result = await _service.RecordClickAsync(request, ipHash);
            return ToActionResult(result);
        }

        /// <summary>
        /// Authenticated — record a referral relationship for the current
        /// user (the referred user). The controller pulls ReferredUserId
        /// from the JWT so a malicious caller can't forge attribution for
        /// somebody else.
        /// </summary>
        [HttpPost("referrals")]
        [Authorize]
        public async Task<IActionResult> RecordReferral([FromBody] RecordReferralRequestDto request)
        {
            if (!_currentUser.UserId.HasValue)
                return Unauthorized();
            var result = await _service.RecordReferralAsync(_currentUser.UserId.Value, request);
            return ToActionResult(result);
        }

        private string? GetClientIp()
        {
            // Honour standard proxy headers but cap to first hop.
            var fwd = Request.Headers["X-Forwarded-For"].ToString();
            if (!string.IsNullOrWhiteSpace(fwd))
                return fwd.Split(',')[0].Trim();
            return HttpContext.Connection.RemoteIpAddress?.ToString();
        }
    }
}
