using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ZansiHustle.Domain.Orders;

namespace ZansiHustle.Application.Persistence.Orders
{
    public interface IOrderRepository
    {
        Task<Order?> GetByIdAsync(Guid id);
        Task<List<Order>> GetByBuyerAsync(Guid buyerUserId);
        Task<List<Order>> GetBySellerUserAsync(Guid sellerUserId);
        Task<List<Order>> GetByMerchantAsync(Guid merchantId);
        Task<bool> ExistsByCodeAsync(string code);
        Task AddAsync(Order order);
        void Update(Order order);
        Task<bool> SaveChangesAsync();
    }
}
