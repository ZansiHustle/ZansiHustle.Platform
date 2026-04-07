using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using ZansiHustle.Application.Influencers;
using ZansiHustle.Application.Influencers.Dtos;

namespace ZansiHustle.API.Controllers
{
    /// <summary>
    /// Exposes endpoints for influencer management.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class InfluencersController : ControllerBase
    {
        private readonly IInfluencerService _influencerService;

        /// <summary>
        /// Creates a new instance of the <see cref="InfluencersController"/> class.
        /// </summary>
        public InfluencersController(IInfluencerService influencerService)
        {
            _influencerService = influencerService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var result = await _influencerService.GetAllAsync();
            return Ok(result);
        }

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var result = await _influencerService.GetByIdAsync(id);
            return Ok(result);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateInfluencerRequestDto request)
        {
            var result = await _influencerService.CreateAsync(request);
            return Ok(result);
        }

        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateInfluencerRequestDto request)
        {
            var result = await _influencerService.UpdateAsync(id, request);

            // Ensure consistent response format
            if (!result.IsSuccess)
            {
                return Ok(new { isSuccess = false, message = result.Message, code = result.Code, data = (object)null });
            }

            return Ok(new { isSuccess = true, message = result.Message, data = result.Data });
        }

        [HttpPut("{id:guid}/status")]
        public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdateInfluencerStatusRequestDto request)
        {
            var result = await _influencerService.UpdateStatusAsync(id, request);
            return Ok(result);
        }

        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var result = await _influencerService.DeleteAsync(id);
            return Ok(result);
        }
    }
}
