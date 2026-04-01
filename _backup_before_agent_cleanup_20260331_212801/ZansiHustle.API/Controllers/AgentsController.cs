using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using ZansiHustle.Application.Agents;
using ZansiHustle.Application.Agents.Dtos;

namespace ZansiHustle.API.Controllers
{
    /// <summary>
    /// Exposes endpoints for agent management.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class AgentsController : ControllerBase
    {
        private readonly IAgentService _agentService;

        /// <summary>
        /// Creates a new instance of the <see cref="AgentsController"/> class.
        /// </summary>
        public AgentsController(IAgentService agentService)
        {
            _agentService = agentService;
        }

        /// <summary>
        /// Gets all agents.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var result = await _agentService.GetAllAsync();

            return Ok(result);
        }

        /// <summary>
        /// Gets an agent by identifier.
        /// </summary>
        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var result = await _agentService.GetByIdAsync(id);

            return Ok(result);
        }

        /// <summary>
        /// Creates a new agent.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateAgentRequestDto request)
        {
            var result = await _agentService.CreateAsync(request);

            return Ok(result);
        }

        /// <summary>
        /// Updates an existing agent.
        /// </summary>
        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateAgentRequestDto request)
        {
            var result = await _agentService.UpdateAsync(id, request);

            return Ok(result);
        }

        /// <summary>
        /// Deletes an existing agent.
        /// </summary>
        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var result = await _agentService.DeleteAsync(id);

            return Ok(result);
        }
    }
}
