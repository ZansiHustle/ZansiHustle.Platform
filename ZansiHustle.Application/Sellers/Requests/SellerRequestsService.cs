using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ZansiHustle.Application.Persistence.Orders;
using ZansiHustle.Application.Persistence.ServiceBookings;
using ZansiHustle.Shared.Errors;
using ZansiHustle.Shared.Results;

namespace ZansiHustle.Application.Sellers.Requests
{
    /// <summary>
    /// Aggregates the two lightweight seller-request counts (product orders
    /// awaiting acceptance + service bookings awaiting provider acceptance) with
    /// two COUNT queries — no full-list materialisation.
    /// </summary>
    public sealed class SellerRequestsService : ISellerRequestsService
    {
        private readonly IOrderRepository _orders;
        private readonly IServiceBookingRepository _bookings;
        private readonly ILogger<SellerRequestsService> _logger;

        public SellerRequestsService(
            IOrderRepository orders, IServiceBookingRepository bookings, ILogger<SellerRequestsService> logger)
        {
            _orders = orders;
            _bookings = bookings;
            _logger = logger;
        }

        public async Task<Result<SellerRequestCountDto>> GetPendingCountAsync(Guid sellerUserId)
        {
            try
            {
                var productOrders = await _orders.CountAwaitingSellerAcceptanceAsync(sellerUserId);
                var serviceBookings = await _bookings.CountRequestedForSellerAsync(sellerUserId);

                return Result<SellerRequestCountDto>.Success(new SellerRequestCountDto
                {
                    ProductOrders = productOrders,
                    ServiceBookings = serviceBookings,
                    Total = productOrders + serviceBookings,
                }, "Seller request count retrieved.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to count seller pending requests for {SellerUserId}.", sellerUserId);
                return Result<SellerRequestCountDto>.Failure(ErrorCodes.Exception, "Could not load your request count.");
            }
        }
    }
}
