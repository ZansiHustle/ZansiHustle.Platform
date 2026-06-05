using System;
using System.Collections.Generic;

namespace ZansiHustle.Application.ZansiPulse.Dtos
{
    /// <summary>
    /// Payload for <c>POST /api/zansipulse/me/interests</c> — the categories a
    /// user picked during onboarding. Each becomes (or tops up) a strong
    /// starting interest score.
    /// </summary>
    public class SaveInterestsRequestDto
    {
        public List<Guid> CategoryIds { get; set; } = new();
    }

    /// <summary>One row of a user's interest profile.</summary>
    public class UserInterestScoreDto
    {
        public Guid CategoryId { get; set; }
        public string? CategoryName { get; set; }
        public Guid? SubCategoryId { get; set; }
        public string? SubCategoryName { get; set; }
        public decimal Score { get; set; }
        public string Source { get; set; } = string.Empty;
        public DateTime LastUpdatedAt { get; set; }
    }
}
