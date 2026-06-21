using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ZansiHustle.Application.Finance;
using ZansiHustle.Application.Finance.Dtos;
using ZansiHustle.Application.Persistence.ServiceBookings;
using ZansiHustle.Application.Seller.Earnings;
using ZansiHustle.Domain.Finance;
using ZansiHustle.Domain.Orders;
using ZansiHustle.Infrastructure.Data;
using ZansiHustle.Shared.Enums.Finance;
using ZansiHustle.Shared.Enums.Listings;
using ZansiHustle.Shared.Enums.Orders;
using ZansiHustle.Shared.Enums.ServiceBookings;

namespace ZansiHustle.Infrastructure.Finance
{
    /// <summary>
    /// Idempotent reconcile/backfill of the seller + platform finance ledgers.
    /// Uses the SHARED <see cref="SellerFeeCalculator"/> so the books match the
    /// seller dashboard exactly. Decimal-only money math. Each order is wrapped in
    /// try/catch so one bad row never aborts the batch. Excluded (cancelled /
    /// rejected / cancelled-booking) orders get NO seller net credit — correct,
    /// the seller has no available proceeds for reversed work. Full refund-reversal
    /// ledger entries are a documented follow-up.
    /// </summary>
    public sealed class LedgerReconcileService : ILedgerReconcileService
    {
        private readonly AppDbContext _db;
        private readonly IServiceBookingRepository _bookings;

        public LedgerReconcileService(AppDbContext db, IServiceBookingRepository bookings)
        {
            _db = db;
            _bookings = bookings;
        }

        public async Task<LedgerReconcileResultDto> ReconcileAsync(Guid? batchId = null)
        {
            var batch = batchId ?? Guid.NewGuid();
            var result = new LedgerReconcileResultDto { BatchId = batch };

            var paid = await _db.Orders
                .AsNoTracking()
                .Include(o => o.Items)
                .Where(o => o.PaymentStatus == PaymentStatus.Paid)
                .ToListAsync();

            // Booking statuses for service orders — one batched query (no N+1).
            var serviceOrderIds = paid
                .Where(o => o.Items.Any(i => i.ListingType == ListingType.Service))
                .Select(o => o.Id)
                .ToList();
            IReadOnlyDictionary<Guid, ServiceBookingStatus> bookingStatuses =
                serviceOrderIds.Count > 0
                    ? await _bookings.GetStatusesByOrderIdsAsync(serviceOrderIds)
                    : new Dictionary<Guid, ServiceBookingStatus>();

            // Owning seller-user per merchant (Merchant.OwnerUserId), one query.
            var merchantIds = paid.Select(o => o.MerchantId).Distinct().ToList();
            var ownerByMerchant = await _db.Merchants
                .AsNoTracking()
                .Where(m => merchantIds.Contains(m.Id))
                .Select(m => new { m.Id, m.OwnerUserId })
                .ToDictionaryAsync(m => m.Id, m => m.OwnerUserId);

            // Existing seller-credit + platform rows (idempotency pre-filter; the
            // filtered unique index is the hard backstop against races).
            var existingSellerOrderIds = await _db.SellerLedgerEntries
                .AsNoTracking()
                .Where(e => e.EntryType == SellerLedgerEntryType.SellerNetCredit && e.OrderId != null)
                .Select(e => e.OrderId!.Value)
                .ToListAsync();
            var sellerCreditPresent = new HashSet<Guid>(existingSellerOrderIds);

            var existingPlatform = await _db.PlatformLedgerEntries
                .AsNoTracking()
                .Where(e => e.OrderId != null)
                .Select(e => new { OrderId = e.OrderId!.Value, e.EntryType })
                .ToListAsync();
            var platformPresent = new HashSet<(Guid, PlatformLedgerEntryType)>(
                existingPlatform.Select(e => (e.OrderId, e.EntryType)));

            foreach (var order in paid)
            {
                try
                {
                    result.OrdersProcessed++;

                    var isService = order.Items.Any(i => i.ListingType == ListingType.Service);

                    // ── Eligibility (v1: skip-and-exclude refunds) ──────────────
                    if (isService)
                    {
                        var bs = bookingStatuses.TryGetValue(order.Id, out var s)
                            ? s : ServiceBookingStatus.Requested;
                        if (bs == ServiceBookingStatus.Rejected || bs == ServiceBookingStatus.Cancelled)
                        {
                            result.SkippedExcluded++;
                            continue;
                        }
                    }
                    else if (order.Status == OrderStatus.Cancelled)
                    {
                        result.SkippedExcluded++;
                        continue;
                    }

                    // ── Idempotency: already booked? ────────────────────────────
                    if (sellerCreditPresent.Contains(order.Id))
                    {
                        result.SkippedAlreadyPresent++;
                        continue;
                    }

                    // ── Seller-user resolution ──────────────────────────────────
                    if (!ownerByMerchant.TryGetValue(order.MerchantId, out var ownerUserId)
                        || ownerUserId is null || ownerUserId.Value == Guid.Empty)
                    {
                        result.SkippedExcluded++;
                        continue;
                    }

                    var currency = string.IsNullOrWhiteSpace(order.Currency) ? "ZAR" : order.Currency;
                    var eligibleBase = order.Subtotal;
                    var fees = SellerFeeCalculator.Compute(eligibleBase);
                    var deliveryFee = order.DeliveryFee ?? 0m;
                    var occurredAt = order.CompletedAtUtc ?? order.CreatedAtUtc;

                    var completed = isService
                        ? (bookingStatuses.TryGetValue(order.Id, out var cs)
                            && cs == ServiceBookingStatus.Completed)
                        : order.Status == OrderStatus.Completed;
                    var status = completed ? SellerLedgerStatus.Available : SellerLedgerStatus.Pending;
                    var sourceType = isService
                        ? LedgerSourceType.ServiceBooking : LedgerSourceType.ProductOrder;

                    // ── Seller net credit (one row per order) ───────────────────
                    _db.SellerLedgerEntries.Add(new SellerLedgerEntry
                    {
                        Id = Guid.NewGuid(),
                        SellerUserId = ownerUserId.Value,
                        MerchantId = order.MerchantId,
                        OrderId = order.Id,
                        ServiceBookingId = null,
                        SourceType = sourceType,
                        EntryType = SellerLedgerEntryType.SellerNetCredit,
                        GrossAmount = order.Total,
                        EligibleBaseAmount = eligibleBase,
                        GatewayFeeAmount = fees.GatewayFee,
                        PlatformFeeAmount = fees.PlatformFee,
                        SellerNetAmount = fees.SellerNet,
                        DeliveryFeeAmount = deliveryFee,
                        Currency = currency,
                        Status = status,
                        OccurredAtUtc = occurredAt,
                        CreatedAtUtc = DateTime.UtcNow,
                        IsBackfilled = true,
                        BackfillBatchId = batch
                    });

                    // ── Platform rows (unique per (OrderId, EntryType)) ─────────
                    var createdPlatform = 0;
                    createdPlatform += AddPlatformIfAbsent(
                        platformPresent, order, sourceType,
                        PlatformLedgerEntryType.PlatformFeeRevenue, fees.PlatformFee, currency, occurredAt, batch);
                    createdPlatform += AddPlatformIfAbsent(
                        platformPresent, order, sourceType,
                        PlatformLedgerEntryType.GatewayFeeCost, fees.GatewayFee, currency, occurredAt, batch);
                    if (deliveryFee > 0m)
                    {
                        createdPlatform += AddPlatformIfAbsent(
                            platformPresent, order, sourceType,
                            PlatformLedgerEntryType.DeliveryFeePassThrough, deliveryFee, currency, occurredAt, batch);
                    }

                    try
                    {
                        await _db.SaveChangesAsync();
                        sellerCreditPresent.Add(order.Id);
                        result.SellerEntriesCreated++;
                        result.PlatformEntriesCreated += createdPlatform;
                    }
                    catch (DbUpdateException)
                    {
                        // Unique-index race: another run booked this order first.
                        // Detach the pending adds and count as already-present.
                        DetachPending();
                        sellerCreditPresent.Add(order.Id);
                        result.SkippedAlreadyPresent++;
                    }
                }
                catch (Exception)
                {
                    DetachPending();
                    result.Failures++;
                }
            }

            return result;
        }

        public async Task<PlatformFinanceSummaryDto> GetPlatformSummaryAsync()
        {
            var platformFee = await SumPlatformAsync(PlatformLedgerEntryType.PlatformFeeRevenue);
            var gatewayCost = await SumPlatformAsync(PlatformLedgerEntryType.GatewayFeeCost);
            var delivery = await SumPlatformAsync(PlatformLedgerEntryType.DeliveryFeePassThrough);

            var sellerPayable = await _db.SellerLedgerEntries
                .AsNoTracking()
                .Where(e => e.EntryType == SellerLedgerEntryType.SellerNetCredit
                    && (e.Status == SellerLedgerStatus.Pending || e.Status == SellerLedgerStatus.Available))
                .SumAsync(e => (decimal?)e.SellerNetAmount) ?? 0m;

            return new PlatformFinanceSummaryDto
            {
                Currency = "ZAR",
                PlatformFeeRevenue = platformFee,
                GatewayFeeCost = gatewayCost,
                DeliveryFeePassThrough = delivery,
                SellerNetPayable = sellerPayable,
                // Gateway is a COST, not profit — net retained is platform fee only.
                NetPlatformRetained = platformFee
            };
        }

        // ── Helpers ──────────────────────────────────────────────────────────

        private int AddPlatformIfAbsent(
            HashSet<(Guid, PlatformLedgerEntryType)> present,
            Order order,
            LedgerSourceType sourceType,
            PlatformLedgerEntryType entryType,
            decimal amount,
            string currency,
            DateTime occurredAt,
            Guid batch)
        {
            if (present.Contains((order.Id, entryType))) return 0;

            _db.PlatformLedgerEntries.Add(new PlatformLedgerEntry
            {
                Id = Guid.NewGuid(),
                OrderId = order.Id,
                ServiceBookingId = null,
                SourceType = sourceType,
                EntryType = entryType,
                Amount = amount,
                Currency = currency,
                OccurredAtUtc = occurredAt,
                CreatedAtUtc = DateTime.UtcNow,
                IsBackfilled = true,
                BackfillBatchId = batch
            });
            present.Add((order.Id, entryType));
            return 1;
        }

        private async Task<decimal> SumPlatformAsync(PlatformLedgerEntryType type) =>
            await _db.PlatformLedgerEntries
                .AsNoTracking()
                .Where(e => e.EntryType == type)
                .SumAsync(e => (decimal?)e.Amount) ?? 0m;

        /// <summary>Detach any Added ledger rows still pending after a failed save,
        /// so the next order's SaveChanges doesn't re-attempt them.</summary>
        private void DetachPending()
        {
            foreach (var entry in _db.ChangeTracker
                .Entries()
                .Where(e => e.State == EntityState.Added &&
                    (e.Entity is SellerLedgerEntry || e.Entity is PlatformLedgerEntry))
                .ToList())
            {
                entry.State = EntityState.Detached;
            }
        }
    }
}
