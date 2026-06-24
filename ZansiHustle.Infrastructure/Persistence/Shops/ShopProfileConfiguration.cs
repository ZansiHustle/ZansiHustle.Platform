using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ZansiHustle.Domain.Merchants;
using ZansiHustle.Domain.Shops;
using ZansiHustle.Shared.Enums.Shops;

namespace ZansiHustle.Infrastructure.Persistence.Shops
{
    /// <summary>
    /// EF Core mapping for <see cref="ShopProfile"/>. Auto-discovered
    /// by <c>ApplyConfigurationsFromAssembly</c> in <c>AppDbContext</c>.
    /// </summary>
    public class ShopProfileConfiguration : IEntityTypeConfiguration<ShopProfile>
    {
        public void Configure(EntityTypeBuilder<ShopProfile> entity)
        {
            entity.ToTable("ShopProfiles");
            entity.HasKey(x => x.Id);

            entity.Property(x => x.MerchantId).IsRequired();
            entity.Property(x => x.Slug).IsRequired().HasMaxLength(80);
            entity.Property(x => x.Name).IsRequired().HasMaxLength(120);

            entity.Property(x => x.Description).HasMaxLength(2000);
            entity.Property(x => x.LogoUrl).HasMaxLength(500);
            entity.Property(x => x.BannerUrl).HasMaxLength(500);

            // Storefront theme preset key. Required with a DB-level default so
            // existing rows backfill to zansi_default on migration and new rows
            // without an explicit value are still valid.
            entity.Property(x => x.ThemePresetKey)
                .IsRequired()
                .HasMaxLength(40)
                .HasDefaultValue(ShopThemePresets.Default);

            // Storefront background mode (light/themed/dark). Required with a
            // DB-level default so existing rows backfill to "light" on migration.
            entity.Property(x => x.ThemeBackgroundMode)
                .IsRequired()
                .HasMaxLength(20)
                .HasDefaultValue(ShopThemeBackgroundModes.Default);

            entity.Property(x => x.ContactEmail).HasMaxLength(200);
            entity.Property(x => x.ContactPhoneNumber).HasMaxLength(40);
            entity.Property(x => x.WhatsAppNumber).HasMaxLength(40);

            entity.Property(x => x.Province).HasMaxLength(100);
            entity.Property(x => x.City).HasMaxLength(100);
            entity.Property(x => x.AddressLine1).HasMaxLength(200);

            entity.Property(x => x.Status).IsRequired();
            entity.Property(x => x.SubscriptionStatus).IsRequired();

            // Buyer-facing visibility (seller pause / hide all shop items).
            // Required int with a DB-level default so existing rows backfill to
            // Visible (1) on migration.
            entity.Property(x => x.VisibilityStatus)
                .HasConversion<int>()
                .IsRequired()
                .HasDefaultValue(ZansiHustle.Shared.Enums.Shops.ShopVisibilityStatus.Visible);
            entity.Property(x => x.VisibilityPauseReason).HasMaxLength(500);
            entity.HasIndex(x => x.VisibilityStatus);

            entity.Property(x => x.BillingProvider).HasMaxLength(120);
            entity.Property(x => x.BillingReference).HasMaxLength(200);
            entity.Property(x => x.SuspensionReason).HasMaxLength(500);

            // Review aggregates. Same precision as Merchant.Rating so a
            // single decimal projection works across both surfaces.
            // ReviewCount gets an explicit 0 default to keep "no reviews
            // yet" arithmetic correct on freshly-created shops without
            // a follow-up update.
            entity.Property(x => x.Rating).HasPrecision(5, 2);
            entity.Property(x => x.ReviewCount).HasDefaultValue(0);

            entity.Property(x => x.CreatedAtUtc).IsRequired();

            // Slug must be globally unique — used in public URLs.
            entity.HasIndex(x => x.Slug).IsUnique().HasDatabaseName("UX_ShopProfiles_Slug");

            // Merchant lookup index (NON-unique). Previously a filtered UNIQUE
            // index enforced "one non-Suspended shop per merchant" at the DB
            // level. That has been relaxed: Admin/SuperAdmin users may now run
            // multiple shops under one merchant, and a SQL Server filtered
            // index can't make a role-aware decision. The one-shop limit for
            // normal sellers is now enforced purely in the service layer
            // (ShopProfileService.CreateMineAsync). This index stays only to
            // keep GetActiveByMerchantAsync (MerchantId + Status) fast.
            entity.HasIndex(x => new { x.MerchantId, x.Status })
                .HasDatabaseName("IX_ShopProfiles_Merchant_Status");

            // Listing index — Portal /shops and public shop search hit
            // this for the typical (Status, CreatedAt desc) read.
            entity.HasIndex(x => new { x.Status, x.CreatedAtUtc })
                .HasDatabaseName("IX_ShopProfiles_Status_Created");

            // FK to Merchant. Restrict on delete: hard-deleting a
            // merchant should NOT silently drop the shop row (which
            // may carry billing references); the service layer
            // handles that explicitly when/if needed.
            entity.HasOne<Merchant>()
                .WithMany()
                .HasForeignKey(x => x.MerchantId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
