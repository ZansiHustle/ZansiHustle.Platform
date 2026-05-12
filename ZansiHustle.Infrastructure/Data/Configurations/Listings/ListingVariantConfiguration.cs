using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ZansiHustle.Domain.Listings;

namespace ZansiHustle.Infrastructure.Data.Configurations.Listings
{
    /// <summary>
    /// EF Core mapping for <see cref="ListingVariant"/>.
    ///
    /// Cascade-deleted with the parent listing: deleting a listing
    /// removes all its variants in one DB round-trip rather than
    /// leaving orphan rows. Buyer-facing reads always go through the
    /// parent listing, so the variants table is never queried in
    /// isolation; the (ListingId, SortOrder) index supports the
    /// "load all variants for this listing in display order" hot path
    /// used by the listing-detail loader.
    /// </summary>
    public class ListingVariantConfiguration : IEntityTypeConfiguration<ListingVariant>
    {
        public void Configure(EntityTypeBuilder<ListingVariant> builder)
        {
            builder.ToTable("ListingVariants");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Name)
                .IsRequired()
                .HasMaxLength(80);

            builder.Property(x => x.Description)
                .HasMaxLength(300);

            builder.Property(x => x.Price)
                .HasPrecision(18, 2);

            builder.Property(x => x.Sku)
                .HasMaxLength(64);

            builder.Property(x => x.IsActive)
                .HasDefaultValue(true);

            builder.Property(x => x.UsesCustomPrice)
                .HasDefaultValue(false);

            builder.Property(x => x.SortOrder)
                .HasDefaultValue(0);

            builder.Property(x => x.CreatedAtUtc)
                .IsRequired();

            builder.HasIndex(x => new { x.ListingId, x.SortOrder });

            // FK is configured from the dependent side. Cascade ensures
            // a Listings DELETE removes all its variant rows — important
            // because the public detail endpoint always loads variants
            // via the parent and orphans would otherwise linger.
            builder.HasOne(x => x.Listing)
                .WithMany(l => l.Variants)
                .HasForeignKey(x => x.ListingId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
