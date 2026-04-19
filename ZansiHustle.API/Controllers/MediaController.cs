using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ZansiHustle.Application.Media;
using ZansiHustle.Application.Media.Dtos;
using ZansiHustle.Shared.Enums.Media;

namespace ZansiHustle.API.Controllers
{
    /// <summary>
    /// Media metadata endpoints. Actual blob bytes never flow through this
    /// controller — the client uploads directly to the signed URL returned
    /// by /upload-tokens, and reads via the signed URL on the asset DTO.
    /// </summary>
    [Route("api/media")]
    [Authorize]
    public class MediaController : BaseController
    {
        private readonly IMediaService _service;

        public MediaController(IMediaService service)
        {
            _service = service;
        }

        /// <summary>Issue an upload ticket + create the Pending row.</summary>
        [HttpPost("upload-tokens")]
        public async Task<IActionResult> IssueUpload([FromBody] IssueUploadRequestDto request)
            => ToActionResult(await _service.IssueUploadAsync(request));

        /// <summary>Confirm the blob is in storage; flips Pending → Uploaded/PendingReview.</summary>
        [HttpPost("{id:guid}/finalize")]
        public async Task<IActionResult> Finalize(Guid id)
            => ToActionResult(await _service.FinalizeAsync(id));

        /// <summary>Asset metadata + a fresh signed read URL.</summary>
        [HttpGet("{id:guid}")]
        public async Task<IActionResult> Get(Guid id)
            => ToActionResult(await _service.GetAsync(id));

        /// <summary>List media for a polymorphic owner. Caller authorization is the consumer's responsibility.</summary>
        [HttpGet("owner/{ownerType:int}/{ownerId:guid}")]
        public async Task<IActionResult> ListByOwner(int ownerType, Guid ownerId)
            => ToActionResult(await _service.GetByOwnerAsync((OwnerEntityType)ownerType, ownerId));

        /// <summary>Admin review for verification-class assets (KYC).</summary>
        [HttpPut("{id:guid}/review")]
        [Authorize(Roles = "SuperAdmin,Admin,Partner,Support")]
        public async Task<IActionResult> Review(Guid id, [FromBody] ReviewMediaRequestDto request)
            => ToActionResult(await _service.ReviewAsync(id, request));
    }
}
