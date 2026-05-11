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

            entity.Property(x => x.ContactEmail).HasMaxLength(200);
            entity.Property(x => x.ContactPhoneNumber).HasMaxLength(40);
            entity.Property(x => x.WhatsAppNumber).HasMaxLength(40);

            entity.Property(x => x.Province).HasMaxLength(100);
            entity.Property(x => x.City).HasMaxLength(100);
            entity.Property(x => x.AddressLine1).HasMaxLength(200);

            entity.Property(x => x.Status).IsRequired();
            entity.Property(x => x.SubscriptionStatus).IsRequired();

            entity.Property(x => x.BillingProvider).HasMaxLength(120);
            entity.Property(x => x.BillingReference).HasMaxLength(200);
            entity.Property(x => x.SuspensionReason).HasMaxLength(500);

            entity.Property(x => x.CreatedAtUtc).IsRequired();

            // Slug must be globally unique — used in public URLs.
            entity.HasIndex(x => x.Slug).IsUnique().HasDatabaseName("UX_ShopProfiles_Slug");

            // One non-Suspended shop per merchant. Suspended rows are
            // excluded so re-opening a shop is possible without first
            // hard-deleting the old row (preserves history). SQL Server
            // filtered index syntax — Status 3 == ShopProfileStatus.Suspended.
            entity.HasIndex(x => x.MerchantId)
                .IsUnique()
                .HasFilter($"[Status] <> {(int)ShopProfileStatus.Suspended}")
                .HasDatabaseName("UX_ShopProfiles_Merchant_Active");

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
