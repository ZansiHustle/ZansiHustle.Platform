using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using ZansiHustle.Application.ContentTasks;
using ZansiHustle.Application.ContentTasks.Dtos;

namespace ZansiHustle.API.Controllers
{
    /// <summary>
    /// Exposes endpoints for content task management.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class ContentTasksController : ControllerBase
    {
        private readonly IContentTaskService _contentTaskService;

        /// <summary>
        /// Creates a new instance of the <see cref="ContentTasksController"/> class.
        /// </summary>
        public ContentTasksController(IContentTaskService contentTaskService)
        {
            _contentTaskService = contentTaskService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var result = await _contentTaskService.GetAllAsync();
            return Ok(result);
        }

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var result = await _contentTaskService.GetByIdAsync(id);
            return Ok(result);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateContentTaskRequestDto request)
        {
            var result = await _contentTaskService.CreateAsync(request);
            return Ok(result);
        }

        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateContentTaskRequestDto request)
        {
            var result = await _contentTaskService.UpdateAsync(id, request);
            return Ok(result);
        }

        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var result = await _contentTaskService.DeleteAsync(id);
            return Ok(result);
        }
    }
}
