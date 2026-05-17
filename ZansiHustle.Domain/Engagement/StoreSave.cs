using System;
using ZansiHustle.Domain.Identity;
using ZansiHustle.Domain.Merchants;

namespace ZansiHustle.Domain.Engagement
{
    /// <summary>
    /// "Save for later" of a physical-store <see cref="Merchant"/>
    /// (a Merchant whose <c>Type</c> is <c>PhysicalStore</c>). Distinct
    /// vocabulary from "like" (products / services) and "follow"
    /// (shops) — users are bookmarking a real-world place they intend
    /// to visit.
    ///
    /// Foreign-keys to <see cref="Merchant"/> directly (PhysicalStores
    /// aren't a separate entity in this codebase; they're Merchants
    /// with <c>Type == PhysicalStore</c>). The service layer is
    /// responsible for refusing a save against a Merchant whose Type
    /// isn't PhysicalStore — there's no DB constraint on Type because
    /// EF would have to load the merchant anyway, and a service-layer
    /// check produces a clean <c>BAD_REQUEST</c> instead of a CHECK
    /// constraint surprise.
    ///
    /// One row per (UserId, MerchantId). Saved count is denormalised
    /// onto <see cref="Merchant.SavesCount"/> and updated
    /// transactionally with this row.
    /// </summary>
    public class StoreSave
    {
        public Guid Id { get; set; }

        public Guid UserId { get; set; }
        public User? User { get; set; }

        public Guid MerchantId { get; set; }
        public Merchant? Merchant { get; set; }

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    }
}
