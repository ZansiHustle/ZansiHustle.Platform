using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using ZansiHustle.Application.AgentApplications;
using ZansiHustle.Application.AgentApplications.Dtos;

namespace ZansiHustle.API.Controllers
{
    /// <summary>
    /// Exposes endpoints for agent application management.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class AgentApplicationsController : ControllerBase
    {
        private readonly IAgentApplicationService _agentApplicationService;

        /// <summary>
        /// Creates a new instance of the <see cref="AgentApplicationsController"/> class.
        /// </summary>
        public AgentApplicationsController(IAgentApplicationService agentApplicationService)
        {
            _agentApplicationService = agentApplicationService;
        }

        /// <summary>
        /// Gets all agent applications.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var result = await _agentApplicationService.GetAllAsync();

            return Ok(result);
        }

        /// <summary>
        /// Gets an agent application by identifier.
        /// </summary>
        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var result = await _agentApplicationService.GetByIdAsync(id);

            return Ok(result);
        }

        /// <summary>
        /// Creates a new agent application.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateAgentApplicationRequestDto request)
        {
            var result = await _agentApplicationService.CreateAsync(request);

            return Ok(result);
        }

        /// <summary>
        /// Reviews an agent application.
        /// </summary>
        [HttpPut("{id:guid}/review")]
        public async Task<IActionResult> Review(Guid id, [FromBody] ReviewAgentApplicationRequestDto request)
        {
            var result = await _agentApplicationService.ReviewAsync(id, request);

            return Ok(result);
        }

        /// <summary>
        /// Deletes an agent application.
        /// </summary>
        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var result = await _agentApplicationService.DeleteAsync(id);

            return Ok(result);
        }
    }
}
