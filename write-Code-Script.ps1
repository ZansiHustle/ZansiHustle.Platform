$root = "P:\ZansiHustle\Softwares\ZansiHustle"

function Write-File {
    param(
        [string]$RelativePath,
        [string]$Content
    )

    $fullPath = Join-Path $root $RelativePath
    $dir = Split-Path $fullPath -Parent

    if (-not (Test-Path $dir)) {
        New-Item -Path $dir -ItemType Directory -Force | Out-Null
    }

    Set-Content -Path $fullPath -Value $Content -Encoding UTF8
    Write-Host "Wrote: $RelativePath" -ForegroundColor Green
}

# ============================================
# DASHBOARD DTOS
# ============================================

Write-File "ZansiHustle.Application\Dashboard\Dtos\PlatformDistributionDto.cs" @'
namespace ZansiHustle.Application.Dashboard.Dtos
{
    /// <summary>
    /// Represents a platform-based metric breakdown.
    /// </summary>
    public class PlatformDistributionDto
    {
        public string Platform { get; set; } = string.Empty;
        public int Count { get; set; }
        public long TotalFollowers { get; set; }
        public decimal TotalSpend { get; set; }
    }
}
'@

Write-File "ZansiHustle.Application\Dashboard\Dtos\ProvinceDistributionDto.cs" @'
namespace ZansiHustle.Application.Dashboard.Dtos
{
    /// <summary>
    /// Represents a province-based metric breakdown.
    /// </summary>
    public class ProvinceDistributionDto
    {
        public string Province { get; set; } = string.Empty;
        public int Count { get; set; }
    }
}
'@

Write-File "ZansiHustle.Application\Dashboard\Dtos\TrendPointDto.cs" @'
namespace ZansiHustle.Application.Dashboard.Dtos
{
    /// <summary>
    /// Represents a time-based trend point.
    /// </summary>
    public class TrendPointDto
    {
        public string Label { get; set; } = string.Empty;
        public int Count { get; set; }
        public long Reach { get; set; }
        public decimal Spend { get; set; }
    }
}
'@

Write-File "ZansiHustle.Application\Dashboard\Dtos\TeamActivityDto.cs" @'
namespace ZansiHustle.Application.Dashboard.Dtos
{
    /// <summary>
    /// Represents a team performance summary item.
    /// </summary>
    public class TeamActivityDto
    {
        public string TeamMember { get; set; } = string.Empty;
        public int Leads { get; set; }
        public int Conversions { get; set; }
        public int Pending { get; set; }
        public long Reach { get; set; }
        public decimal Spend { get; set; }
    }
}
'@

Write-File "ZansiHustle.Application\Dashboard\Dtos\CampaignPerformanceDto.cs" @'
using System;

namespace ZansiHustle.Application.Dashboard.Dtos
{
    /// <summary>
    /// Represents campaign performance on the dashboard.
    /// </summary>
    public class CampaignPerformanceDto
    {
        public Guid Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? CampaignType { get; set; }
        public string Status { get; set; } = string.Empty;
        public decimal Budget { get; set; }
        public long Reach { get; set; }
        public long Engagements { get; set; }
        public long Clicks { get; set; }
        public long Conversions { get; set; }
        public decimal Spend { get; set; }
    }
}
'@

Write-File "ZansiHustle.Application\Dashboard\Dtos\LaunchOpsSummaryDto.cs" @'
using System.Collections.Generic;

namespace ZansiHustle.Application.Dashboard.Dtos
{
    /// <summary>
    /// Represents launch and operations dashboard summary data.
    /// </summary>
    public class LaunchOpsSummaryDto
    {
        public int TotalAgents { get; set; }
        public int TotalAgentApplications { get; set; }
        public int PendingAgentApplications { get; set; }
        public int TotalSellerLeads { get; set; }
        public int PendingSellerLeads { get; set; }
        public int ApprovedSellerLeads { get; set; }
        public int VerifiedSellerLeads { get; set; }
        public int ConvertedSellerLeads { get; set; }

        public List<ProvinceDistributionDto> SellerLeadProvinceDistribution { get; set; } = new();
        public List<TeamActivityDto> TeamActivity { get; set; } = new();
    }
}
'@

Write-File "ZansiHustle.Application\Dashboard\Dtos\MarketingSummaryDto.cs" @'
using System.Collections.Generic;

namespace ZansiHustle.Application.Dashboard.Dtos
{
    /// <summary>
    /// Represents marketing intelligence dashboard summary data.
    /// </summary>
    public class MarketingSummaryDto
    {
        public int TotalInfluencers { get; set; }
        public int ApprovedInfluencers { get; set; }
        public long TotalInfluencerReach { get; set; }
        public decimal ApprovedInfluencerSpend { get; set; }
        public int TotalPodcasts { get; set; }
        public int ActiveCampaigns { get; set; }

        public List<PlatformDistributionDto> PlatformDistribution { get; set; } = new();
        public List<ProvinceDistributionDto> InfluencerProvinceDistribution { get; set; } = new();
        public List<CampaignPerformanceDto> Campaigns { get; set; } = new();
        public List<TrendPointDto> Trends { get; set; } = new();
    }
}
'@

Write-File "ZansiHustle.Application\Dashboard\Dtos\SocialMediaSummaryDto.cs" @'
using System.Collections.Generic;

namespace ZansiHustle.Application.Dashboard.Dtos
{
    /// <summary>
    /// Represents social media dashboard summary data.
    /// </summary>
    public class SocialMediaSummaryDto
    {
        public long TotalReach { get; set; }
        public long TotalEngagements { get; set; }
        public long TotalClicks { get; set; }
        public long TotalConversions { get; set; }
        public decimal TotalSpend { get; set; }

        public List<PlatformDistributionDto> PlatformDistribution { get; set; } = new();
        public List<CampaignPerformanceDto> Campaigns { get; set; } = new();
        public List<TrendPointDto> Trends { get; set; } = new();
    }
}
'@

Write-File "ZansiHustle.Application\Dashboard\Dtos\BudgetSummaryDto.cs" @'
namespace ZansiHustle.Application.Dashboard.Dtos
{
    /// <summary>
    /// Represents budget dashboard summary data.
    /// </summary>
    public class BudgetSummaryDto
    {
        public decimal TotalDeposits { get; set; }
        public decimal TotalPayments { get; set; }
        public decimal TotalObligations { get; set; }
        public decimal TotalRefunds { get; set; }
        public decimal TotalAdjustments { get; set; }
        public decimal NetAvailable { get; set; }
    }
}
'@

# ============================================
# DASHBOARD REPOSITORY INTERFACES
# ============================================

Write-File "ZansiHustle.Application\Persistence\Dashboard\ILaunchOpsDashboardRepository.cs" @'
using System.Collections.Generic;
using System.Threading.Tasks;
using ZansiHustle.Application.Dashboard.Dtos;

namespace ZansiHustle.Application.Persistence.Dashboard
{
    /// <summary>
    /// Repository contract for launch operations dashboard queries.
    /// </summary>
    public interface ILaunchOpsDashboardRepository
    {
        Task<LaunchOpsSummaryDto> GetSummaryAsync();
        Task<List<ProvinceDistributionDto>> GetSellerLeadProvinceDistributionAsync();
        Task<List<TeamActivityDto>> GetTeamActivityAsync();
    }
}
'@

Write-File "ZansiHustle.Application\Persistence\Dashboard\IMarketingDashboardRepository.cs" @'
using System.Collections.Generic;
using System.Threading.Tasks;
using ZansiHustle.Application.Dashboard.Dtos;

namespace ZansiHustle.Application.Persistence.Dashboard
{
    /// <summary>
    /// Repository contract for marketing dashboard queries.
    /// </summary>
    public interface IMarketingDashboardRepository
    {
        Task<MarketingSummaryDto> GetMarketingSummaryAsync();
        Task<SocialMediaSummaryDto> GetSocialMediaSummaryAsync();
        Task<BudgetSummaryDto> GetBudgetSummaryAsync();
        Task<List<PlatformDistributionDto>> GetInfluencerPlatformDistributionAsync();
        Task<List<ProvinceDistributionDto>> GetInfluencerProvinceDistributionAsync();
        Task<List<CampaignPerformanceDto>> GetCampaignPerformanceAsync();
        Task<List<TrendPointDto>> GetCampaignTrendsAsync();
    }
}
'@

# ============================================
# DASHBOARD REPOSITORY IMPLEMENTATIONS
# ============================================

Write-File "ZansiHustle.Infrastructure\Persistence\Dashboard\LaunchOpsDashboardRepository.cs" @'
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ZansiHustle.Application.Dashboard.Dtos;
using ZansiHustle.Application.Persistence.Dashboard;
using ZansiHustle.Infrastructure.Data;
using ZansiHustle.Shared.Enums.AgentApplications;
using ZansiHustle.Shared.Enums.SellerLeads;

namespace ZansiHustle.Infrastructure.Persistence.Dashboard
{
    /// <summary>
    /// Repository implementation for launch operations dashboard queries.
    /// </summary>
    public class LaunchOpsDashboardRepository : ILaunchOpsDashboardRepository
    {
        private readonly AppDbContext _context;

        /// <summary>
        /// Creates a new instance of the <see cref="LaunchOpsDashboardRepository"/> class.
        /// </summary>
        public LaunchOpsDashboardRepository(AppDbContext context)
        {
            _context = context;
        }

        /// <inheritdoc />
        public async Task<LaunchOpsSummaryDto> GetSummaryAsync()
        {
            var totalAgents = await _context.Agents.CountAsync();
            var totalAgentApplications = await _context.AgentApplications.CountAsync();
            var pendingAgentApplications = await _context.AgentApplications.CountAsync(x => x.Status == AgentApplicationStatus.Pending);

            var totalSellerLeads = await _context.SellerLeads.CountAsync();
            var pendingSellerLeads = await _context.SellerLeads.CountAsync(x => x.ApprovalStatus == ApprovalStatus.Pending);
            var approvedSellerLeads = await _context.SellerLeads.CountAsync(x => x.ApprovalStatus == ApprovalStatus.Approved);
            var verifiedSellerLeads = await _context.SellerLeads.CountAsync(x => x.VerificationStatus == VerificationStatus.Verified);
            var convertedSellerLeads = await _context.SellerLeads.CountAsync(x => x.ConvertedSellerId != null);

            return new LaunchOpsSummaryDto
            {
                TotalAgents = totalAgents,
                TotalAgentApplications = totalAgentApplications,
                PendingAgentApplications = pendingAgentApplications,
                TotalSellerLeads = totalSellerLeads,
                PendingSellerLeads = pendingSellerLeads,
                ApprovedSellerLeads = approvedSellerLeads,
                VerifiedSellerLeads = verifiedSellerLeads,
                ConvertedSellerLeads = convertedSellerLeads,
                SellerLeadProvinceDistribution = await GetSellerLeadProvinceDistributionAsync(),
                TeamActivity = await GetTeamActivityAsync()
            };
        }

        /// <inheritdoc />
        public async Task<List<ProvinceDistributionDto>> GetSellerLeadProvinceDistributionAsync()
        {
            return await _context.SellerLeads
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
        public async Task<List<TeamActivityDto>> GetTeamActivityAsync()
        {
            return await _context.Agents
                .AsNoTracking()
                .Select(x => new TeamActivityDto
                {
                    TeamMember = x.FullName,
                    Leads = x.SellerLeads.Count(),
                    Conversions = x.SellerLeads.Count(s => s.ConvertedSellerId != null),
                    Pending = x.SellerLeads.Count(s => s.ApprovalStatus == ApprovalStatus.Pending),
                    Reach = 0,
                    Spend = 0m
                })
                .OrderByDescending(x => x.Leads)
                .ToListAsync();
        }
    }
}
'@

Write-File "ZansiHustle.Infrastructure\Persistence\Dashboard\MarketingDashboardRepository.cs" @'
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
'@

# ============================================
# DASHBOARD SERVICE INTERFACES
# ============================================

Write-File "ZansiHustle.Application\Dashboard\ILaunchOpsDashboardService.cs" @'
using System.Collections.Generic;
using System.Threading.Tasks;
using ZansiHustle.Application.Dashboard.Dtos;
using ZansiHustle.Shared.Results;

namespace ZansiHustle.Application.Dashboard
{
    /// <summary>
    /// Service contract for launch operations dashboard logic.
    /// </summary>
    public interface ILaunchOpsDashboardService
    {
        Task<Result<LaunchOpsSummaryDto>> GetSummaryAsync();
        Task<Result<List<ProvinceDistributionDto>>> GetSellerLeadProvinceDistributionAsync();
        Task<Result<List<TeamActivityDto>>> GetTeamActivityAsync();
    }
}
'@

Write-File "ZansiHustle.Application\Dashboard\IMarketingDashboardService.cs" @'
using System.Collections.Generic;
using System.Threading.Tasks;
using ZansiHustle.Application.Dashboard.Dtos;
using ZansiHustle.Shared.Results;

namespace ZansiHustle.Application.Dashboard
{
    /// <summary>
    /// Service contract for marketing dashboard logic.
    /// </summary>
    public interface IMarketingDashboardService
    {
        Task<Result<MarketingSummaryDto>> GetMarketingSummaryAsync();
        Task<Result<SocialMediaSummaryDto>> GetSocialMediaSummaryAsync();
        Task<Result<BudgetSummaryDto>> GetBudgetSummaryAsync();
        Task<Result<List<PlatformDistributionDto>>> GetInfluencerPlatformDistributionAsync();
        Task<Result<List<ProvinceDistributionDto>>> GetInfluencerProvinceDistributionAsync();
        Task<Result<List<CampaignPerformanceDto>>> GetCampaignPerformanceAsync();
        Task<Result<List<TrendPointDto>>> GetCampaignTrendsAsync();
    }
}
'@

# ============================================
# DASHBOARD SERVICE IMPLEMENTATIONS
# ============================================

Write-File "ZansiHustle.Application\Dashboard\LaunchOpsDashboardService.cs" @'
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ZansiHustle.Application.Dashboard.Dtos;
using ZansiHustle.Application.Persistence.Dashboard;
using ZansiHustle.Shared.Results;

namespace ZansiHustle.Application.Dashboard
{
    /// <summary>
    /// Provides business logic for launch operations dashboard queries.
    /// </summary>
    public class LaunchOpsDashboardService : ILaunchOpsDashboardService
    {
        private readonly ILaunchOpsDashboardRepository _launchOpsDashboardRepository;

        /// <summary>
        /// Creates a new instance of the <see cref="LaunchOpsDashboardService"/> class.
        /// </summary>
        public LaunchOpsDashboardService(ILaunchOpsDashboardRepository launchOpsDashboardRepository)
        {
            _launchOpsDashboardRepository = launchOpsDashboardRepository;
        }

        /// <inheritdoc />
        public async Task<Result<LaunchOpsSummaryDto>> GetSummaryAsync()
        {
            try
            {
                var data = await _launchOpsDashboardRepository.GetSummaryAsync();
                return Result<LaunchOpsSummaryDto>.Success(data, "Launch operations summary retrieved successfully.");
            }
            catch (Exception ex)
            {
                return Result<LaunchOpsSummaryDto>.Failure($"An error occurred while retrieving the launch operations summary. {ex.Message}");
            }
        }

        /// <inheritdoc />
        public async Task<Result<List<ProvinceDistributionDto>>> GetSellerLeadProvinceDistributionAsync()
        {
            try
            {
                var data = await _launchOpsDashboardRepository.GetSellerLeadProvinceDistributionAsync();
                return Result<List<ProvinceDistributionDto>>.Success(data, "Seller lead province distribution retrieved successfully.");
            }
            catch (Exception ex)
            {
                return Result<List<ProvinceDistributionDto>>.Failure($"An error occurred while retrieving seller lead province distribution. {ex.Message}");
            }
        }

        /// <inheritdoc />
        public async Task<Result<List<TeamActivityDto>>> GetTeamActivityAsync()
        {
            try
            {
                var data = await _launchOpsDashboardRepository.GetTeamActivityAsync();
                return Result<List<TeamActivityDto>>.Success(data, "Team activity retrieved successfully.");
            }
            catch (Exception ex)
            {
                return Result<List<TeamActivityDto>>.Failure($"An error occurred while retrieving team activity. {ex.Message}");
            }
        }
    }
}
'@

Write-File "ZansiHustle.Application\Dashboard\MarketingDashboardService.cs" @'
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ZansiHustle.Application.Dashboard.Dtos;
using ZansiHustle.Application.Persistence.Dashboard;
using ZansiHustle.Shared.Results;

namespace ZansiHustle.Application.Dashboard
{
    /// <summary>
    /// Provides business logic for marketing dashboard queries.
    /// </summary>
    public class MarketingDashboardService : IMarketingDashboardService
    {
        private readonly IMarketingDashboardRepository _marketingDashboardRepository;

        /// <summary>
        /// Creates a new instance of the <see cref="MarketingDashboardService"/> class.
        /// </summary>
        public MarketingDashboardService(IMarketingDashboardRepository marketingDashboardRepository)
        {
            _marketingDashboardRepository = marketingDashboardRepository;
        }

        /// <inheritdoc />
        public async Task<Result<MarketingSummaryDto>> GetMarketingSummaryAsync()
        {
            try
            {
                var data = await _marketingDashboardRepository.GetMarketingSummaryAsync();
                return Result<MarketingSummaryDto>.Success(data, "Marketing summary retrieved successfully.");
            }
            catch (Exception ex)
            {
                return Result<MarketingSummaryDto>.Failure($"An error occurred while retrieving the marketing summary. {ex.Message}");
            }
        }

        /// <inheritdoc />
        public async Task<Result<SocialMediaSummaryDto>> GetSocialMediaSummaryAsync()
        {
            try
            {
                var data = await _marketingDashboardRepository.GetSocialMediaSummaryAsync();
                return Result<SocialMediaSummaryDto>.Success(data, "Social media summary retrieved successfully.");
            }
            catch (Exception ex)
            {
                return Result<SocialMediaSummaryDto>.Failure($"An error occurred while retrieving the social media summary. {ex.Message}");
            }
        }

        /// <inheritdoc />
        public async Task<Result<BudgetSummaryDto>> GetBudgetSummaryAsync()
        {
            try
            {
                var data = await _marketingDashboardRepository.GetBudgetSummaryAsync();
                return Result<BudgetSummaryDto>.Success(data, "Budget summary retrieved successfully.");
            }
            catch (Exception ex)
            {
                return Result<BudgetSummaryDto>.Failure($"An error occurred while retrieving the budget summary. {ex.Message}");
            }
        }

        /// <inheritdoc />
        public async Task<Result<List<PlatformDistributionDto>>> GetInfluencerPlatformDistributionAsync()
        {
            try
            {
                var data = await _marketingDashboardRepository.GetInfluencerPlatformDistributionAsync();
                return Result<List<PlatformDistributionDto>>.Success(data, "Influencer platform distribution retrieved successfully.");
            }
            catch (Exception ex)
            {
                return Result<List<PlatformDistributionDto>>.Failure($"An error occurred while retrieving influencer platform distribution. {ex.Message}");
            }
        }

        /// <inheritdoc />
        public async Task<Result<List<ProvinceDistributionDto>>> GetInfluencerProvinceDistributionAsync()
        {
            try
            {
                var data = await _marketingDashboardRepository.GetInfluencerProvinceDistributionAsync();
                return Result<List<ProvinceDistributionDto>>.Success(data, "Influencer province distribution retrieved successfully.");
            }
            catch (Exception ex)
            {
                return Result<List<ProvinceDistributionDto>>.Failure($"An error occurred while retrieving influencer province distribution. {ex.Message}");
            }
        }

        /// <inheritdoc />
        public async Task<Result<List<CampaignPerformanceDto>>> GetCampaignPerformanceAsync()
        {
            try
            {
                var data = await _marketingDashboardRepository.GetCampaignPerformanceAsync();
                return Result<List<CampaignPerformanceDto>>.Success(data, "Campaign performance retrieved successfully.");
            }
            catch (Exception ex)
            {
                return Result<List<CampaignPerformanceDto>>.Failure($"An error occurred while retrieving campaign performance. {ex.Message}");
            }
        }

        /// <inheritdoc />
        public async Task<Result<List<TrendPointDto>>> GetCampaignTrendsAsync()
        {
            try
            {
                var data = await _marketingDashboardRepository.GetCampaignTrendsAsync();
                return Result<List<TrendPointDto>>.Success(data, "Campaign trends retrieved successfully.");
            }
            catch (Exception ex)
            {
                return Result<List<TrendPointDto>>.Failure($"An error occurred while retrieving campaign trends. {ex.Message}");
            }
        }
    }
}
'@

# ============================================
# DASHBOARD CONTROLLER
# ============================================

Write-File "ZansiHustle.API\Controllers\DashboardController.cs" @'
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using ZansiHustle.Application.Dashboard;

namespace ZansiHustle.API.Controllers
{
    /// <summary>
    /// Exposes dashboard summary and analytics endpoints.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class DashboardController : ControllerBase
    {
        private readonly ILaunchOpsDashboardService _launchOpsDashboardService;
        private readonly IMarketingDashboardService _marketingDashboardService;

        /// <summary>
        /// Creates a new instance of the <see cref="DashboardController"/> class.
        /// </summary>
        public DashboardController(
            ILaunchOpsDashboardService launchOpsDashboardService,
            IMarketingDashboardService marketingDashboardService)
        {
            _launchOpsDashboardService = launchOpsDashboardService;
            _marketingDashboardService = marketingDashboardService;
        }

        [HttpGet("launch-ops-summary")]
        public async Task<IActionResult> GetLaunchOpsSummary()
        {
            var result = await _launchOpsDashboardService.GetSummaryAsync();
            return Ok(result);
        }

        [HttpGet("seller-lead-province-distribution")]
        public async Task<IActionResult> GetSellerLeadProvinceDistribution()
        {
            var result = await _launchOpsDashboardService.GetSellerLeadProvinceDistributionAsync();
            return Ok(result);
        }

        [HttpGet("team-activity")]
        public async Task<IActionResult> GetTeamActivity()
        {
            var result = await _launchOpsDashboardService.GetTeamActivityAsync();
            return Ok(result);
        }

        [HttpGet("marketing-summary")]
        public async Task<IActionResult> GetMarketingSummary()
        {
            var result = await _marketingDashboardService.GetMarketingSummaryAsync();
            return Ok(result);
        }

        [HttpGet("social-summary")]
        public async Task<IActionResult> GetSocialSummary()
        {
            var result = await _marketingDashboardService.GetSocialMediaSummaryAsync();
            return Ok(result);
        }

        [HttpGet("budget-summary")]
        public async Task<IActionResult> GetBudgetSummary()
        {
            var result = await _marketingDashboardService.GetBudgetSummaryAsync();
            return Ok(result);
        }

        [HttpGet("influencer-platform-distribution")]
        public async Task<IActionResult> GetInfluencerPlatformDistribution()
        {
            var result = await _marketingDashboardService.GetInfluencerPlatformDistributionAsync();
            return Ok(result);
        }

        [HttpGet("influencer-province-distribution")]
        public async Task<IActionResult> GetInfluencerProvinceDistribution()
        {
            var result = await _marketingDashboardService.GetInfluencerProvinceDistributionAsync();
            return Ok(result);
        }

        [HttpGet("campaign-performance")]
        public async Task<IActionResult> GetCampaignPerformance()
        {
            var result = await _marketingDashboardService.GetCampaignPerformanceAsync();
            return Ok(result);
        }

        [HttpGet("campaign-trends")]
        public async Task<IActionResult> GetCampaignTrends()
        {
            var result = await _marketingDashboardService.GetCampaignTrendsAsync();
            return Ok(result);
        }
    }
}
'@

Write-Host ""
Write-Host "Dashboard layer written successfully." -ForegroundColor Cyan
Write-Host "Next:" -ForegroundColor Yellow
Write-Host "1. Register dashboard repositories and services in ServiceExtensions.cs" -ForegroundColor Yellow
Write-Host "2. Build solution" -ForegroundColor Yellow
Write-Host "3. Fix any Result signature mismatches if needed" -ForegroundColor Yellow