using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ZansiHustle.Shared.Results;

namespace ZansiHustle.Application.Blocks
{
    /// <summary>Body for <c>POST /api/blocks</c>.</summary>
    public class BlockUserRequestDto
    {
        public Guid BlockedUserId { get; set; }
        public string? Reason { get; set; }
    }

    /// <summary>A blocked user in the caller's block list.</summary>
    public class BlockedUserDto
    {
        public Guid BlockedUserId { get; set; }
        public string? BlockedUserName { get; set; }
        public DateTime CreatedAtUtc { get; set; }
    }

    /// <summary>
    /// User blocking (App Store Guideline 1.2). Directional block that prevents
    /// chat and lets the client hide the blocked user's content.
    /// </summary>
    public interface IUserBlockService
    {
        Task<Result> BlockAsync(Guid blockerUserId, BlockUserRequestDto request);
        Task<Result> UnblockAsync(Guid blockerUserId, Guid blockedUserId);
        Task<Result<List<BlockedUserDto>>> GetMyBlocksAsync(Guid blockerUserId);

        /// <summary>
        /// True if either user has blocked the other (used to gate chat both
        /// directions). Infrastructure/chat calls this.
        /// </summary>
        Task<bool> IsBlockedEitherWayAsync(Guid userA, Guid userB);
    }
}
