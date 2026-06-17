using System;
using System.Threading.Tasks;
using ZansiHustle.Application.Seller.Earnings.Dtos;
using ZansiHustle.Shared.Results;

namespace ZansiHustle.Application.Seller.Earnings
{
    public interface ISellerEarningsService
    {
        /// <summary>
        /// Seller proceeds summary for the caller's own merchants over a range
        /// ("7d" | "30d" | "month" | "90d" | "all"). Server-authoritative — only
        /// PAID orders count, refunded/cancelled never count as available, and the
        /// caller only ever sees their own data (filtered by merchant ownership).
        /// </summary>
        Task<Result<SellerEarningsSummaryDto>> GetSummaryAsync(Guid sellerUserId, string? range);

        /// <summary>
        /// Clean ALL-TIME finance summary for the dashboard card: total earned
        /// (completed/eligible, net), paid out (completed payouts only), and
        /// pending payout (earned − paid out). Safe zeroes when nothing qualifies.
        /// </summary>
        Task<Result<SellerFinanceSummaryDto>> GetFinanceSummaryAsync(Guid sellerUserId);
    }
}
