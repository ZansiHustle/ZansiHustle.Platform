using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ZansiHustle.Application.Blocks;
using ZansiHustle.Domain.Blocks;
using ZansiHustle.Domain.Identity;
using ZansiHustle.Infrastructure.Data;
using ZansiHustle.Shared.Errors;
using ZansiHustle.Shared.Results;

namespace ZansiHustle.Infrastructure.Blocks
{
    /// <summary>DB-direct user-block service. See <see cref="IUserBlockService"/>.</summary>
    public sealed class UserBlockService : IUserBlockService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<UserBlockService> _logger;

        public UserBlockService(AppDbContext context, ILogger<UserBlockService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<Result> BlockAsync(Guid blockerUserId, BlockUserRequestDto request)
        {
            try
            {
                if (request is null || request.BlockedUserId == Guid.Empty)
                    return Result.Failure(ErrorCodes.BadRequest, "A user to block is required.");
                if (request.BlockedUserId == blockerUserId)
                    return Result.Failure(ErrorCodes.BadRequest, "You can't block yourself.");

                var already = await _context.Set<UserBlock>().AnyAsync(b =>
                    b.BlockerUserId == blockerUserId && b.BlockedUserId == request.BlockedUserId);
                if (already)
                    return Result.Success("User already blocked.");

                await _context.Set<UserBlock>().AddAsync(new UserBlock
                {
                    Id = Guid.NewGuid(),
                    BlockerUserId = blockerUserId,
                    BlockedUserId = request.BlockedUserId,
                    Reason = string.IsNullOrWhiteSpace(request.Reason) ? null : request.Reason.Trim(),
                    CreatedAtUtc = DateTime.UtcNow,
                });
                await _context.SaveChangesAsync();
                return Result.Success("User blocked.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Block failed ({Blocker} -> {Blocked}).", blockerUserId, request?.BlockedUserId);
                return Result.Failure(ErrorCodes.Exception, "Couldn't block this user. Please try again.");
            }
        }

        public async Task<Result> UnblockAsync(Guid blockerUserId, Guid blockedUserId)
        {
            try
            {
                var row = await _context.Set<UserBlock>().FirstOrDefaultAsync(b =>
                    b.BlockerUserId == blockerUserId && b.BlockedUserId == blockedUserId);
                if (row is null)
                    return Result.Success("User was not blocked.");

                _context.Set<UserBlock>().Remove(row);
                await _context.SaveChangesAsync();
                return Result.Success("User unblocked.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unblock failed ({Blocker} -> {Blocked}).", blockerUserId, blockedUserId);
                return Result.Failure(ErrorCodes.Exception, "Couldn't unblock this user. Please try again.");
            }
        }

        public async Task<Result<List<BlockedUserDto>>> GetMyBlocksAsync(Guid blockerUserId)
        {
            try
            {
                var blocks = await _context.Set<UserBlock>()
                    .AsNoTracking()
                    .Where(b => b.BlockerUserId == blockerUserId)
                    .OrderByDescending(b => b.CreatedAtUtc)
                    .ToListAsync();

                var ids = blocks.Select(b => b.BlockedUserId).Distinct().ToList();
                var names = await _context.Set<User>()
                    .AsNoTracking()
                    .Where(u => ids.Contains(u.Id))
                    .Select(u => new { u.Id, Name = ((u.FirstName ?? "") + " " + (u.LastName ?? "")).Trim() })
                    .ToDictionaryAsync(x => x.Id, x => x.Name);

                var items = blocks.Select(b => new BlockedUserDto
                {
                    BlockedUserId = b.BlockedUserId,
                    BlockedUserName = names.TryGetValue(b.BlockedUserId, out var n) && n.Length > 0 ? n : null,
                    CreatedAtUtc = b.CreatedAtUtc,
                }).ToList();

                return Result<List<BlockedUserDto>>.Success(items);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load block list for {User}.", blockerUserId);
                return Result<List<BlockedUserDto>>.Failure(ErrorCodes.Exception, "Couldn't load your blocked users.");
            }
        }

        public Task<bool> IsBlockedEitherWayAsync(Guid userA, Guid userB)
        {
            return _context.Set<UserBlock>().AnyAsync(b =>
                (b.BlockerUserId == userA && b.BlockedUserId == userB) ||
                (b.BlockerUserId == userB && b.BlockedUserId == userA));
        }
    }
}
