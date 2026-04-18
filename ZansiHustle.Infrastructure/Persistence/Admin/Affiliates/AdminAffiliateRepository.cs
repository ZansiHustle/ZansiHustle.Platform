using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using ZansiHustle.Application.Admin.Affiliates.Dtos;
using ZansiHustle.Application.Persistence.Admin.Affiliates;
using ZansiHustle.Infrastructure.Data;
using ZansiHustle.Application.Common.Paging;
using ZansiHustle.Shared.Queries;
using ZansiHustle.Shared.Results;

namespace ZansiHustle.Infrastructure.Persistence.Admin.Affiliates
{
    /// <summary>
    /// Admin Affiliates queries.
    ///
    /// No Affiliate aggregate in the domain yet — real-login mode honestly
    /// returns zero counts and an empty paged envelope so product decisions
    /// aren't corrupted by fake data. Swap in real aggregations here once
    /// the entity lands.
    /// </summary>
    public class AdminAffiliateRepository : IAdminAffiliateRepository
    {
        private readonly AppDbContext _context;

        public AdminAffiliateRepository(AppDbContext context)
        {
            _context = context;
            _ = _context; // retained for future wiring
        }

        public Task<AffiliatesKpisDto> GetKpisAsync()
        {
            return Task.FromResult(new AffiliatesKpisDto
            {
                TotalAffiliates = 0,
                ActiveAffiliates = 0,
                TotalReferrals = 0,
                TotalConversions = 0,
                TotalCommissions = 0m,
                TotalEarnings = 0m,
                ConversionRate = 0m,
            });
        }

        public Task<PagedResult<AdminAffiliateListItemDto>> GetPagedAsync(PagedListQueryBase query)
        {
            return Task.FromResult(new PagedResult<AdminAffiliateListItemDto>
            {
                Items = new List<AdminAffiliateListItemDto>(),
                Total = 0,
                Page = query.Page,
                PageSize = query.PageSize,
            });
        }

        public Task<List<AffiliatePerformancePointDto>> GetPerformanceTrendAsync(int months)
        {
            var now = DateTime.UtcNow;
            var windowStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc)
                .AddMonths(-(months - 1));

            var points = new List<AffiliatePerformancePointDto>(months);
            for (var i = 0; i < months; i++)
            {
                var marker = windowStart.AddMonths(i);
                points.Add(new AffiliatePerformancePointDto
                {
                    Month = marker.ToString("MMM", CultureInfo.InvariantCulture),
                    Referrals = 0,
                    Conversions = 0,
                    Commission = 0m,
                });
            }
            return Task.FromResult(points);
        }
    }
}
