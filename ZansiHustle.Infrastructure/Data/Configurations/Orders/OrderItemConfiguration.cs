using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ZansiHustle.Domain.Orders;

namespace ZansiHustle.Infrastructure.Data.Configurations.Orders
{
    public class OrderItemConfiguration : IEntityTypeConfiguration<OrderItem>
    {
        public void Configure(EntityTypeBuilder<OrderItem> builder)
        {
            builder.ToTable("OrderItems");

            builder.HasKey(x => x.Id);

            builder.HasIndex(x => x.OrderId);
            builder.HasIndex(x => x.ListingId);
            builder.HasIndex(x => x.ListingType);

            builder.Property(x => x.TitleSnapshot)
                .IsRequired()
                .HasMaxLength(250);

            builder.Property(x => x.ImageSnapshot)
                .HasMaxLength(1000);

            builder.Property(x => x.UnitPrice)
                .HasPrecision(18, 2)
                .IsRequired();

            builder.Property(x => x.LineTotal)
                .HasPrecision(18, 2)
                .IsRequired();

            builder.Property(x => x.Quantity)
                .IsRequired();

            builder.Property(x => x.ListingType)
                .HasConversion<int>()
                .IsRequired();

            builder.Property(x => x.CreatedAtUtc)
                .IsRequired();

            // Keep the order item even if the underlying listing is deleted —
            // the snapshot fields carry the business-relevant data forward.
            // Merchant delete still cascades through Listings, but OrderItems
            // survive because ListingId becomes null.
            builder.HasOne(x => x.Listing)
                .WithMany()
                .HasForeignKey(x => x.ListingId)
                .OnDelete(DeleteBehavior.SetNull);
        }
    }
}
