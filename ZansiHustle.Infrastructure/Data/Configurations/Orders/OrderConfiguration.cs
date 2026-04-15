using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ZansiHustle.Domain.Orders;

namespace ZansiHustle.Infrastructure.Data.Configurations.Orders
{
    public class OrderConfiguration : IEntityTypeConfiguration<Order>
    {
        public void Configure(EntityTypeBuilder<Order> builder)
        {
            builder.ToTable("Orders");

            builder.HasKey(x => x.Id);

            builder.HasIndex(x => x.Code).IsUnique();
            builder.HasIndex(x => x.BuyerUserId);
            builder.HasIndex(x => x.MerchantId);
            builder.HasIndex(x => x.Status);
            builder.HasIndex(x => x.PaymentStatus);
            builder.HasIndex(x => x.CreatedAtUtc);

            builder.Property(x => x.Code)
                .IsRequired()
                .HasMaxLength(60);

            builder.Property(x => x.BuyerName).HasMaxLength(200);
            builder.Property(x => x.BuyerEmail).HasMaxLength(256);
            builder.Property(x => x.BuyerPhone).HasMaxLength(30);

            builder.Property(x => x.Status)
                .HasConversion<int>()
                .IsRequired();

            builder.Property(x => x.PaymentStatus)
                .HasConversion<int>()
                .IsRequired();

            builder.Property(x => x.Subtotal)
                .HasPrecision(18, 2)
                .IsRequired();

            builder.Property(x => x.Total)
                .HasPrecision(18, 2)
                .IsRequired();

            builder.Property(x => x.Currency)
                .IsRequired()
                .HasMaxLength(8);

            builder.Property(x => x.DeliveryAddress).HasMaxLength(500);
            builder.Property(x => x.Notes).HasMaxLength(2000);
            builder.Property(x => x.CancellationReason).HasMaxLength(500);

            builder.Property(x => x.CreatedAtUtc).IsRequired();

            builder.HasOne(x => x.Merchant)
                .WithMany()
                .HasForeignKey(x => x.MerchantId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(x => x.Items)
                .WithOne(i => i.Order)
                .HasForeignKey(i => i.OrderId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
