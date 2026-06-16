using System;
using System.Threading.Tasks;
using ZansiHustle.Shared.Results;

namespace ZansiHustle.Application.Sellers.Requests
{
    /// <summary>
    /// Lightweight seller "actionable requests" counter — drives the Sell-tab
    /// badge. Counts only items needing the seller/provider's accept/reject
    /// decision (product orders awaiting acceptance + service bookings awaiting
    /// provider acceptance). Scoped to the caller's own merchants; a non-seller
    /// safely gets zeros.
    /// </summary>
    public interface ISellerRequestsService
    {
        Task<Result<SellerRequestCountDto>> GetPendingCountAsync(Guid sellerUserId);
    }

    /// <summary>Counts of the seller's actionable pending requests.</summary>
    public sealed class SellerRequestCountDto
    {
        public int Total { get; set; }
        public int ProductOrders { get; set; }
        public int ServiceBookings { get; set; }
    }
}
