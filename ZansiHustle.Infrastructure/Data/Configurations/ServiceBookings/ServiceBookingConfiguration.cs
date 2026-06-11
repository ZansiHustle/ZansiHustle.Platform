using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ZansiHustle.Domain.ServiceBookings;

namespace ZansiHustle.Infrastructure.Data.Configurations.ServiceBookings
{
    /// <summary>
    /// EF mapping for <see cref="ServiceBooking"/>. Additive table; existing
    /// orders/listings are unaffected. The (MerchantId, StartAtUtc) index is the
    /// hot path for the availability overlap query.
    /// </summary>
    public class ServiceBookingConfiguration : IEntityTypeConfiguration<ServiceBooking>
    {
        public void Configure(EntityTypeBuilder<ServiceBooking> builder)
        {
            builder.ToTable("ServiceBookings");

            builder.HasKey(x => x.Id);

            builder.HasIndex(x => new { x.MerchantId, x.StartAtUtc });
            builder.HasIndex(x => x.ListingId);
            builder.HasIndex(x => x.OrderId);
            builder.HasIndex(x => x.CustomerUserId);
            builder.HasIndex(x => x.Status);

            builder.Property(x => x.Mode)
                .HasConversion<int>()
                .IsRequired();

            builder.Property(x => x.Status)
                .HasConversion<int>()
                .IsRequired();

            builder.Property(x => x.EstimatedDurationMinutes).IsRequired();
            builder.Property(x => x.BufferMinutes).IsRequired();

            builder.Property(x => x.StartAtUtc).IsRequired();
            builder.Property(x => x.EndAtUtc).IsRequired();
            builder.Property(x => x.CreatedAtUtc).IsRequired();

            builder.Property(x => x.RejectionReasonCode).HasMaxLength(50);
            builder.Property(x => x.RejectionReasonText).HasMaxLength(500);

            builder.Property(x => x.BuyerFormattedAddress).HasMaxLength(500);
            builder.Property(x => x.BuyerAddressLine1).HasMaxLength(250);
            builder.Property(x => x.BuyerPlaceId).HasMaxLength(200);
            builder.Property(x => x.ProviderLocationSnapshot).HasMaxLength(500);
            builder.Property(x => x.Notes).HasMaxLength(2000);

            builder.Property(x => x.BuyerLatitude).HasPrecision(9, 6);
            builder.Property(x => x.BuyerLongitude).HasPrecision(9, 6);

            builder.Property(x => x.BaseServiceAmount).HasPrecision(18, 2);
            builder.Property(x => x.HouseCallSurcharge).HasPrecision(18, 2);
            builder.Property(x => x.TravelFee).HasPrecision(18, 2);

            // Booking dies with its order (cascade). Listing/Merchant use
            // NoAction to avoid multiple-cascade-paths on SQL Server and to
            // preserve booking history if a catalog row is later removed.
            builder.HasOne(x => x.Order)
                .WithMany()
                .HasForeignKey(x => x.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(x => x.Listing)
                .WithMany()
                .HasForeignKey(x => x.ListingId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.HasOne(x => x.Merchant)
                .WithMany()
                .HasForeignKey(x => x.MerchantId)
                .OnDelete(DeleteBehavior.NoAction);
        }
    }
}
