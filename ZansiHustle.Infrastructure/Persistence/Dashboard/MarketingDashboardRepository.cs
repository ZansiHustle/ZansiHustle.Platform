using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ZansiHustle.Application.Dashboard.Dtos;
using ZansiHustle.Application.Persistence.Dashboard;
using ZansiHustle.Infrastructure.Data;
using ZansiHustle.Shared.Enums.Budget;
using ZansiHustle.Shared.Enums.Campaigns;
using ZansiHustle.Shared.Enums.Influencers;

namespace ZansiHustle.Infrastructure.Persistence.Dashboard
{
    /// <summary>
    /// Repository implementation for marketing dashboard queries.
    /// </summary>
    public class MarketingDashboardRepository : IMarketingDashboardRepository
    {
        private readonly AppDbContext _context;

        /// <summary>
        /// Creates a new instance of the <see cref="MarketingDashboardRepository"/> class.
        /// </summary>
        public MarketingDashboardRepository(AppDbContext context)
        {
            _context = context;
        }

        /// <inheritdoc />
        public async Task<MarketingSummaryDto> GetMarketingSummaryAsync()
        {
            var totalInfluencers = await _context.Influencers.CountAsync();
            var approvedInfluencers = await _context.Influencers.CountAsync(x => x.Status == InfluencerStatus.Approved);
            var totalInfluencerReach = await _context.InfluencerPlatformAccounts.SumAsync(x => (long?)x.FollowersCount) ?? 0;
            var approvedInfluencerSpend = await _context.Influencers
                .Where(x => x.Status == InfluencerStatus.Approved)
                .SumAsync(x => (decimal?)x.Rate) ?? 0m;

            var totalPodcasts = await _context.Podcasts.CountAsync();
            var activeCampaigns = await _context.Campaigns.CountAsync(x => x.Status == CampaignStatus.Active);

            return new MarketingSummaryDto
            {
                TotalInfluencers = totalInfluencers,
                ApprovedInfluencers = approvedInfluencers,
                TotalInfluencerReach = totalInfluencerReach,
                ApprovedInfluencerSpend = approvedInfluencerSpend,
                TotalPodcasts = totalPodcasts,
                ActiveCampaigns = activeCampaigns,
                PlatformDistribution = await GetInfluencerPlatformDistributionAsync(),
                InfluencerProvinceDistribution = await GetInfluencerProvinceDistributionAsync(),
                Campaigns = await GetCampaignPerformanceAsync(),
                Trends = await GetCampaignTrendsAsync()
            };
        }

        /// <inheritdoc />
        public async Task<SocialMediaSummaryDto> GetSocialMediaSummaryAsync()
        {
            var snapshots = _context.CampaignMetricSnapshots.AsNoTracking();

            return new SocialMediaSummaryDto
            {
                TotalReach = await snapshots.SumAsync(x => (long?)x.Reach) ?? 0,
                TotalEngagements = await snapshots.SumAsync(x => (long?)x.Engagements) ?? 0,
                TotalClicks = await snapshots.SumAsync(x => (long?)x.Clicks) ?? 0,
                TotalConversions = await snapshots.SumAsync(x => (long?)x.Conversions) ?? 0,
                TotalSpend = await snapshots.SumAsync(x => (decimal?)x.Spend) ?? 0m,
                PlatformDistribution = await GetInfluencerPlatformDistributionAsync(),
                Campaigns = await GetCampaignPerformanceAsync(),
                Trends = await GetCampaignTrendsAsync()
            };
        }

        /// <inheritdoc />
        public async Task<BudgetSummaryDto> GetBudgetSummaryAsync()
        {
            var transactions = _context.BudgetTransactions.AsNoTracking();

            var totalDeposits = await transactions.Where(x => x.TransactionType == BudgetTransactionType.Deposit).SumAsync(x => (decimal?)x.Amount) ?? 0m;
            var totalPayments = await transactions.Where(x => x.TransactionType == BudgetTransactionType.Payment).SumAsync(x => (decimal?)x.Amount) ?? 0m;
            var totalObligations = await transactions.Where(x => x.TransactionType == BudgetTransactionType.Obligation).SumAsync(x => (decimal?)x.Amount) ?? 0m;
            var totalRefunds = await transactions.Where(x => x.TransactionType == BudgetTransactionType.Refund).SumAsync(x => (decimal?)x.Amount) ?? 0m;
            var totalAdjustments = await transactions.Where(x => x.TransactionType == BudgetTransactionType.Adjustment).SumAsync(x => (decimal?)x.Amount) ?? 0m;

            return new BudgetSummaryDto
            {
                TotalDeposits = totalDeposits,
                TotalPayments = totalPayments,
                TotalObligations = totalObligations,
                TotalRefunds = totalRefunds,
                TotalAdjustments = totalAdjustments,
                NetAvailable = totalDeposits - totalPayments - totalObligations + totalRefunds + totalAdjustments
            };
        }

        /// <inheritdoc />
        public async Task<List<PlatformDistributionDto>> GetInfluencerPlatformDistributionAsync()
        {
            return await _context.InfluencerPlatformAccounts
                .AsNoTracking()
                .GroupBy(x => x.Platform)
                .Select(g => new PlatformDistributionDto
                {
                    Platform = g.Key.ToString(),
                    Count = g.Count(),
                    TotalFollowers = g.Sum(x => x.FollowersCount),
                    TotalSpend = 0m
                })
                .OrderByDescending(x => x.TotalFollowers)
                .ToListAsync();
        }

        /// <inheritdoc />
        public async Task<List<ProvinceDistributionDto>> GetInfluencerProvinceDistributionAsync()
        {
            return await _context.Influencers
                .AsNoTracking()
                .Where(x => !string.IsNullOrWhiteSpace(x.Province))
                .GroupBy(x => x.Province!)
                .Select(g => new ProvinceDistributionDto
                {
                    Province = g.Key,
                    Count = g.Count()
                })
                .OrderByDescending(x => x.Count)
                .ToListAsync();
        }

        /// <inheritdoc />
        public async Task<List<CampaignPerformanceDto>> GetCampaignPerformanceAsync()
        {
            return await _context.Campaigns
                .AsNoTracking()
                .Select(x => new CampaignPerformanceDto
                {
                    Id = x.Id,
                    Code = x.Code,
                    Name = x.Name,
                    CampaignType = x.CampaignType,
                    Status = x.Status.ToString(),
                    Budget = x.Budget,
                    Reach = x.MetricSnapshots.Sum(m => (long?)m.Reach) ?? 0,
                    Engagements = x.MetricSnapshots.Sum(m => (long?)m.Engagements) ?? 0,
                    Clicks = x.MetricSnapshots.Sum(m => (long?)m.Clicks) ?? 0,
                    Conversions = x.MetricSnapshots.Sum(m => (long?)m.Conversions) ?? 0,
                    Spend = x.MetricSnapshots.Sum(m => (decimal?)m.Spend) ?? 0m
                })
                .OrderByDescending(x => x.Reach)
                .ToListAsync();
        }

        /// <inheritdoc />
        public async Task<List<TrendPointDto>> GetCampaignTrendsAsync()
        {
            return await _context.CampaignMetricSnapshots
                .AsNoTracking()
                .GroupBy(x => new { x.SnapshotDateUtc.Year, x.SnapshotDateUtc.Month })
                .Select(g => new TrendPointDto
                {
                    Label = $"{g.Key.Year}-{g.Key.Month:D2}",
                    Count = g.Count(),
                    Reach = g.Sum(x => (long?)x.Reach) ?? 0,
                    Spend = g.Sum(x => (decimal?)x.Spend) ?? 0m
                })
                .OrderBy(x => x.Label)
                .ToListAsync();
        }
    }
}
