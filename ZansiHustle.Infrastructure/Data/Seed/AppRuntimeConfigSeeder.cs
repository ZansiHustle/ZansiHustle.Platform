using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ZansiHustle.Domain.AppConfigs;
using ZansiHustle.Infrastructure.Data;

namespace ZansiHustle.Infrastructure.Data.Seed
{
    /// <summary>
    /// Seeds the default remote app-control flags into <c>AppRuntimeConfigs</c>
    /// on startup. Idempotent: only inserts keys that are missing, so operator
    /// toggles to existing rows are never overwritten. All boolean flags default
    /// to ENABLED (BooleanValue = true) so seeding never disables a feature.
    /// </summary>
    public static class AppRuntimeConfigSeeder
    {
        public static async Task SeedAsync(AppDbContext db)
        {
            var existingKeys = await db.AppRuntimeConfigs
                .Select(c => c.Key)
                .ToListAsync();
            var present = existingKeys.ToHashSet(StringComparer.OrdinalIgnoreCase);

            var defaults = BuildDefaults();
            var now = DateTime.UtcNow;
            var sort = 0;
            var added = false;

            foreach (var d in defaults)
            {
                sort += 10;
                if (present.Contains(d.Key)) continue;
                db.AppRuntimeConfigs.Add(new AppRuntimeConfig
                {
                    Id = Guid.NewGuid(),
                    Key = d.Key,
                    DisplayName = d.DisplayName,
                    Description = d.Description,
                    Category = d.Category,
                    ValueType = "Boolean",
                    BooleanValue = true,
                    DisabledTitle = d.DisabledTitle,
                    DisabledMessage = d.DisabledMessage,
                    IsPublic = true,
                    IsActive = true,
                    SortOrder = sort,
                    CreatedAtUtc = now,
                    UpdatedAtUtc = now,
                });
                added = true;
            }

            if (added)
                await db.SaveChangesAsync();
        }

        private sealed record Seed(
            string Key, string DisplayName, string Description, string Category,
            string DisabledTitle, string DisabledMessage);

        private static List<Seed> BuildDefaults() => new()
        {
            // ── Customer ───────────────────────────────────────────────────────
            new("productPurchasingEnabled", "Product purchasing",
                "Allow customers to add products to cart, buy and checkout.", "Customer",
                "Buying is temporarily closed",
                "ZansiHustle is currently closed for buying while we prepare our shops. Please check again soon."),
            new("serviceBookingEnabled", "Service booking",
                "Allow customers to book services.", "Customer",
                "Bookings are temporarily closed",
                "Service bookings are currently paused while we improve the experience. Please check again soon."),

            // ── Messaging ────────────────────────────────────────────────────────
            new("marketplaceMessagingEnabled", "Marketplace messaging",
                "Allow customers to message marketplace sellers.", "Messaging",
                "Messaging is temporarily unavailable",
                "Marketplace messaging is currently paused. Please try again later."),
            new("bookingMessagingEnabled", "Booking messaging",
                "Allow customers to message service providers about bookings.", "Messaging",
                "Messaging is temporarily unavailable",
                "Booking messaging is currently paused. Please try again later."),

            // ── Marketplace / seller creation ────────────────────────────────────
            new("marketplaceListingCreationEnabled", "Marketplace listing creation",
                "Allow users to create quick marketplace listings.", "Marketplace",
                "Listing creation is paused",
                "Creating marketplace listings is temporarily paused. Please check again soon."),
            new("sellerProductCreationEnabled", "Seller product creation",
                "Allow sellers to create products.", "Seller",
                "Adding products is paused",
                "Adding new products is temporarily paused. Please check again soon."),
            new("sellerServiceCreationEnabled", "Seller service creation",
                "Allow sellers to create services.", "Seller",
                "Adding services is paused",
                "Adding new services is temporarily paused. Please check again soon."),
            new("sellerShopCreationEnabled", "Seller shop creation",
                "Allow sellers to create shops.", "Seller",
                "Shop creation is paused",
                "Creating shops is temporarily paused. Please check again soon."),

            // ── System (extra useful controls, future-friendly) ──────────────────
            new("reviewsEnabled", "Reviews",
                "Allow customers to leave reviews.", "System",
                "Reviews are paused",
                "Reviews are temporarily unavailable. Please try again later."),
            new("favoritesEnabled", "Favorites",
                "Allow customers to save favorites.", "System",
                "Favorites are paused",
                "Saving favorites is temporarily unavailable. Please try again later."),
            new("orderTrackingEnabled", "Order tracking",
                "Allow customers to track orders.", "System",
                "Order tracking is paused",
                "Order tracking is temporarily unavailable. Please try again later."),
        };
    }
}
