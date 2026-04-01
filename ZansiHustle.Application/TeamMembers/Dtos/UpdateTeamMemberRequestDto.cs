using System.Collections.Generic;

namespace ZansiHustle.Application.TeamMembers.Dtos
{
    /// <summary>
    /// Request model used to update a team member based on the Users table.
    /// </summary>
    public class UpdateTeamMemberRequestDto
    {
        public string FullName { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public List<string> Roles { get; set; } = new();
        public bool TeamPortalEnabled { get; set; } = true;
        public bool AdminPortalEnabled { get; set; }
        public bool FinanceAccess { get; set; }
        public string Status { get; set; } = "active";
        public string? Department { get; set; }
        public string? Notes { get; set; }
    }
}
