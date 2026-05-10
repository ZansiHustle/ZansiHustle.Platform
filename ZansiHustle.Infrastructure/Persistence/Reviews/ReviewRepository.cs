using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ZansiHustle.Application.Persistence.Reviews;
using ZansiHustle.Domain.Reviews;
using ZansiHustle.Infrastructure.Data;
using ZansiHustle.Shared.Enums.Reviews;

namespace ZansiHustle.Infrastructure.Persistence.Reviews
{
    public class ReviewRepository : IReviewRepository
    {
        private readonly AppDbContext _context;

        public ReviewRepository(AppDbContext context)
        {
            _context = context;
        }

        /// <inheritdoc />
        public Task<Review?> GetByIdAsync(Guid id)
            => _context.Reviews.FirstOrDefaultAsync(r => r.Id == id);

        /// <inheritdoc />
        public async Task<List<Review>> GetActiveForTargetAsync(ReviewTargetType targetType, Guid targetId)
        {
            return await _context.Reviews
                .AsNoTracking()
                .Where(r => r.TargetType == targetType
                            && r.TargetId == targetId
                            && r.Status == ReviewStatus.Active)
                .OrderByDescending(r => r.CreatedAtUtc)
                .ToListAsync();
        }

        /// <inheritdoc />
        public Task<Review?> GetActiveByReviewerAsync(
            ReviewTargetType targetType, Guid targetId, Guid reviewerUserId)
        {
            return _context.Reviews.FirstOrDefaultAsync(r =>
                r.TargetType == targetType
                && r.TargetId == targetId
                && r.ReviewerUserId == reviewerUserId
                && r.Status == ReviewStatus.Active);
        }

        /// <inheritdoc />
        public async Task<(decimal? Average, int Count)> GetSummaryAsync(
            ReviewTargetType targetType, Guid targetId)
        {
            // Project to int + count in one round-trip. We compute the
            // average client-side from the sum/count to avoid SQL
            // Server's average-of-int integer-truncation surprise and
            // to keep the precision logic explicit.
            var totals = await _context.Reviews
                .Where(r => r.TargetType == targetType
                            && r.TargetId == targetId
                            && r.Status == ReviewStatus.Active)
                .GroupBy(_ => 1)
                .Select(g => new
                {
                    Sum = g.Sum(x => x.Rating),
                    Count = g.Count(),
                })
                .FirstOrDefaultAsync();

            if (totals == null || totals.Count == 0)
                return (null, 0);

            var avg = Math.Round((decimal)totals.Sum / totals.Count, 2, MidpointRounding.AwayFromZero);
            return (avg, totals.Count);
        }

        /// <inheritdoc />
        public async Task<Dictionary<Guid, ReviewerDisplayInfo>> GetReviewerDisplaysAsync(
            IEnumerable<Guid> userIds)
        {
            var ids = userIds.Distinct().ToList();
            if (ids.Count == 0) return new();

            // Left-join Users + UserProfiles so users without a
            // profile row still resolve to their first/last name.
            var rows = await (
                from u in _context.Users
                where ids.Contains(u.Id)
                from p in _context.UserProfiles.Where(p => p.UserId == u.Id).DefaultIfEmpty()
                select new
                {
                    u.Id,
                    u.FirstName,
                    u.LastName,
                    AvatarUrl = p == null ? null : p.ProfileImageUrl,
                }
            ).ToListAsync();

            return rows.ToDictionary(
                r => r.Id,
                r =>
                {
                    var name = $"{r.FirstName} {r.LastName}".Trim();
                    if (string.IsNullOrWhiteSpace(name)) name = "Anonymous";
                    return new ReviewerDisplayInfo(name, r.AvatarUrl);
                });
        }

        /// <inheritdoc />
        public async Task AddAsync(Review review)
            => await _context.Reviews.AddAsync(review);

        /// <inheritdoc />
        public void Update(Review review)
            => _context.Reviews.Update(review);

        /// <inheritdoc />
        public async Task<bool> SaveChangesAsync()
            => await _context.SaveChangesAsync() > 0;
    }
}
