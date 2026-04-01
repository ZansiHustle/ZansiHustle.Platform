using System;
using System.Collections.Generic;

namespace ZansiHustle.Application.TeamMembers.Dtos
{
    /// <summary>
    /// Team member DTO used by admin/portal screens.
    /// </summary>
    public class TeamMemberDto
    {
        public string Id { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public List<string> Roles { get; set; } = new();
        public bool TeamPortalEnabled { get; set; }
        public bool AdminPortalEnabled { get; set; }
        public bool FinanceAccess { get; set; }
        public string Status { get; set; } = "active";
        public string? Department { get; set; }
        public DateTime JoinedDateUtc { get; set; }
        public string? Notes { get; set; }
    }
}
