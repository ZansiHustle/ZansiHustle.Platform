using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ZansiHustle.Application.Persistence.Orders;
using ZansiHustle.Domain.Orders;
using ZansiHustle.Infrastructure.Data;

namespace ZansiHustle.Infrastructure.Persistence.Orders
{
    public class OrderRepository : IOrderRepository
    {
        private readonly AppDbContext _context;

        public OrderRepository(AppDbContext context)
        {
            _context = context;
        }

        /// <inheritdoc />
        public async Task<Order?> GetByIdAsync(Guid id)
        {
            return await _context.Orders
                .Include(x => x.Merchant)
                .Include(x => x.Items)
                .FirstOrDefaultAsync(x => x.Id == id);
        }

        /// <inheritdoc />
        public async Task<List<Order>> GetByBuyerAsync(Guid buyerUserId)
        {
            return await _context.Orders
                .AsNoTracking()
                .Include(x => x.Merchant)
                .Include(x => x.Items)
                .Where(x => x.BuyerUserId == buyerUserId)
                .OrderByDescending(x => x.CreatedAtUtc)
                .ToListAsync();
        }

        /// <inheritdoc />
        public async Task<List<Order>> GetBySellerUserAsync(Guid sellerUserId)
        {
            return await _context.Orders
                .AsNoTracking()
                .Include(x => x.Merchant)
                .Include(x => x.Items)
                .Where(x => x.Merchant != null && x.Merchant.OwnerUserId == sellerUserId)
                .OrderByDescending(x => x.CreatedAtUtc)
                .ToListAsync();
        }

        /// <inheritdoc />
        public async Task<int> CountAwaitingSellerAcceptanceAsync(Guid sellerUserId)
        {
            // Actionable product requests: PAID orders parked at
            // AwaitingSellerAcceptance for a merchant this user owns. Lightweight
            // COUNT — no row materialisation. Same ownership filter as the seller
            // order list (no cross-seller leakage). Returns 0 for non-sellers.
            return await _context.Orders
                .AsNoTracking()
                .Where(x => x.Merchant != null
                    && x.Merchant.OwnerUserId == sellerUserId
                    && x.PaymentStatus == ZansiHustle.Shared.Enums.Orders.PaymentStatus.Paid
                    && x.Status == ZansiHustle.Shared.Enums.Orders.OrderStatus.AwaitingSellerAcceptance)
                .CountAsync();
        }

        /// <inheritdoc />
        public async Task<List<Order>> GetByMerchantAsync(Guid merchantId)
        {
            return await _context.Orders
                .AsNoTracking()
                .Include(x => x.Merchant)
                .Include(x => x.Items)
                .Where(x => x.MerchantId == merchantId)
                .OrderByDescending(x => x.CreatedAtUtc)
                .ToListAsync();
        }

        /// <inheritdoc />
        public async Task<bool> ExistsByCodeAsync(string code)
        {
            if (string.IsNullOrWhiteSpace(code))
                return false;

            var normalized = code.Trim();

            return await _context.Orders.AnyAsync(x => x.Code == normalized);
        }

        /// <inheritdoc />
        public async Task AddAsync(Order order)
        {
            ArgumentNullException.ThrowIfNull(order);

            await _context.Orders.AddAsync(order);
        }

        /// <inheritdoc />
        public void Update(Order order)
        {
            ArgumentNullException.ThrowIfNull(order);

            _context.Orders.Update(order);
        }

        /// <inheritdoc />
        public async Task<bool> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync() > 0;
        }
    }
}
