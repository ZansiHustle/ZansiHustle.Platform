using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ZansiHustle.Domain.Marketplace;

namespace ZansiHustle.Infrastructure.Data.Configurations.Marketplace
{
    public class MarketplaceListingConfiguration : IEntityTypeConfiguration<MarketplaceListing>
    {
        public void Configure(EntityTypeBuilder<MarketplaceListing> builder)
        {
            builder.ToTable("MarketplaceListings");

            builder.HasKey(x => x.Id);

            // Owner is a User, NOT a Merchant. Restrict-on-delete keeps
            // historical listings even if the user is deactivated.
            builder.HasIndex(x => x.OwnerUserId);
            builder.HasOne(x => x.Owner)
                .WithMany()
                .HasForeignKey(x => x.OwnerUserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(x => x.Status);
            builder.HasIndex(x => x.Category);
            builder.HasIndex(x => x.Province);
            builder.HasIndex(x => x.CreatedAtUtc);

            builder.Property(x => x.Title)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(x => x.Description)
                .HasMaxLength(4000)
                .IsRequired();

            builder.Property(x => x.Price)
                .HasPrecision(18, 2)
                .IsRequired();

            builder.Property(x => x.Currency)
                .IsRequired()
                .HasMaxLength(8);

            builder.Property(x => x.Category)
                .IsRequired()
                .HasMaxLength(60);

            builder.Property(x => x.Condition)
                .HasConversion<int>()
                .IsRequired();

            builder.Property(x => x.Province)
                .IsRequired()
                .HasMaxLength(60);

            builder.Property(x => x.Location)
                .HasMaxLength(120);

            builder.Property(x => x.Status)
                .HasConversion<int>()
                .IsRequired();

            // Denormalised review aggregate (mirrors Merchant/ShopProfile.Rating
            // precision exactly: decimal(5,2)). Source of truth is the Reviews
            // table; refreshed by ReviewService on each review write.
            builder.Property(x => x.Rating)
                .HasPrecision(5, 2);

            builder.Property(x => x.CreatedAtUtc)
                .IsRequired();

            builder.HasMany(x => x.Images)
                .WithOne(i => i.Listing)
                .HasForeignKey(i => i.ListingId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
