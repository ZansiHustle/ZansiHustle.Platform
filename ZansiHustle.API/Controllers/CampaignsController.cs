using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using ZansiHustle.Application.Campaigns;
using ZansiHustle.Application.Campaigns.Dtos;

namespace ZansiHustle.API.Controllers
{
    /// <summary>
    /// Exposes endpoints for campaign management.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class CampaignsController : ControllerBase
    {
        private readonly ICampaignService _campaignService;

        /// <summary>
        /// Creates a new instance of the <see cref="CampaignsController"/> class.
        /// </summary>
        public CampaignsController(ICampaignService campaignService)
        {
            _campaignService = campaignService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var result = await _campaignService.GetAllAsync();
            return Ok(result);
        }

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var result = await _campaignService.GetByIdAsync(id);
            return Ok(result);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateCampaignRequestDto request)
        {
            var result = await _campaignService.CreateAsync(request);
            return Ok(result);
        }

        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateCampaignRequestDto request)
        {
            var result = await _campaignService.UpdateAsync(id, request);
            return Ok(result);
        }

        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var result = await _campaignService.DeleteAsync(id);
            return Ok(result);
        }
    }
}
