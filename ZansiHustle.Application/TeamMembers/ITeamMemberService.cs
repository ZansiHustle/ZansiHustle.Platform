using System.Collections.Generic;
using System.Threading.Tasks;
using ZansiHustle.Application.TeamMembers.Dtos;
using ZansiHustle.Shared.Results;

namespace ZansiHustle.Application.TeamMembers
{
    /// <summary>
    /// Service contract for team member CRUD operations using the Users table.
    /// </summary>
    public interface ITeamMemberService
    {
        Task<Result<List<TeamMemberDto>>> GetAllAsync();
        Task<Result<TeamMemberDto>> GetByIdAsync(string id);
        Task<Result<TeamMemberDto>> CreateAsync(CreateTeamMemberRequestDto request);
        Task<Result<TeamMemberDto>> UpdateAsync(string id, UpdateTeamMemberRequestDto request);
        Task<Result> DeleteAsync(string id);
    }
}
