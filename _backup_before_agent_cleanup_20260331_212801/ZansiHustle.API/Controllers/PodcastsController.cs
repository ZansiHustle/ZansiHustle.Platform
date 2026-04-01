using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using ZansiHustle.Application.Podcasts;
using ZansiHustle.Application.Podcasts.Dtos;

namespace ZansiHustle.API.Controllers
{
    /// <summary>
    /// Exposes endpoints for podcast management.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class PodcastsController : ControllerBase
    {
        private readonly IPodcastService _podcastService;

        /// <summary>
        /// Creates a new instance of the <see cref="PodcastsController"/> class.
        /// </summary>
        public PodcastsController(IPodcastService podcastService)
        {
            _podcastService = podcastService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var result = await _podcastService.GetAllAsync();
            return Ok(result);
        }

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var result = await _podcastService.GetByIdAsync(id);
            return Ok(result);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreatePodcastRequestDto request)
        {
            var result = await _podcastService.CreateAsync(request);
            return Ok(result);
        }

        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdatePodcastRequestDto request)
        {
            var result = await _podcastService.UpdateAsync(id, request);
            return Ok(result);
        }

        [HttpPut("{id:guid}/status")]
        public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdatePodcastStatusRequestDto request)
        {
            var result = await _podcastService.UpdateStatusAsync(id, request);
            return Ok(result);
        }

        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var result = await _podcastService.DeleteAsync(id);
            return Ok(result);
        }
    }
}
