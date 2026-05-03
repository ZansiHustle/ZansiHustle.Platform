using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ZansiHustle.Domain.Marketplace;

namespace ZansiHustle.Infrastructure.Data.Configurations.Marketplace
{
    public class MarketplaceListingImageConfiguration : IEntityTypeConfiguration<MarketplaceListingImage>
    {
        public void Configure(EntityTypeBuilder<MarketplaceListingImage> builder)
        {
            builder.ToTable("MarketplaceListingImages");

            builder.HasKey(x => x.Id);

            // Common access path: list a listing's images sorted by SortOrder.
            builder.HasIndex(x => new { x.ListingId, x.SortOrder });

            builder.Property(x => x.Url)
                .IsRequired()
                .HasMaxLength(1000);

            builder.Property(x => x.SortOrder)
                .IsRequired();

            builder.Property(x => x.CreatedAtUtc)
                .IsRequired();

            // FK + cascade delete is declared on the parent
            // configuration (MarketplaceListingConfiguration) so EF only
            // emits one relationship.
        }
    }
}
