using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ZansiHustle.Application.TeamMembers;
using ZansiHustle.Application.TeamMembers.Dtos;
using ZansiHustle.Shared.Results;

namespace ZansiHustle.API.Controllers
{
    /// <summary>
    /// Exposes endpoints for team member management using the Users table.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class TeamMembersController : ControllerBase
    {
        private readonly ITeamMemberService _teamMemberService;

        public TeamMembersController(ITeamMemberService teamMemberService)
        {
            _teamMemberService = teamMemberService;
        }

        [HttpGet]
        [ProducesResponseType(typeof(Result<List<TeamMemberDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAll()
        {
            var result = await _teamMemberService.GetAllAsync();
            return Ok(result);
        }

        [HttpGet("{id}")]
        [ProducesResponseType(typeof(Result<TeamMemberDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetById(string id)
        {
            var result = await _teamMemberService.GetByIdAsync(id);
            return Ok(result);
        }

        [HttpPost]
        [ProducesResponseType(typeof(Result<TeamMemberDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> Create([FromBody] CreateTeamMemberRequestDto request)
        {
            var result = await _teamMemberService.CreateAsync(request);
            return Ok(result);
        }

        [HttpPut("{id}")]
        [ProducesResponseType(typeof(Result<TeamMemberDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> Update(string id, [FromBody] UpdateTeamMemberRequestDto request)
        {
            var result = await _teamMemberService.UpdateAsync(id, request);
            return Ok(result);
        }

        [HttpDelete("{id}")]
        [ProducesResponseType(typeof(Result), StatusCodes.Status200OK)]
        public async Task<IActionResult> Delete(string id)
        {
            var result = await _teamMemberService.DeleteAsync(id);
            return Ok(result);
        }
    }
}
