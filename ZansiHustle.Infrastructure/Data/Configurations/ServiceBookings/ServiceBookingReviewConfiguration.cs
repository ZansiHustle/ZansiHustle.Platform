using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ZansiHustle.Domain.Identity;
using ZansiHustle.Domain.ServiceBookings;

namespace ZansiHustle.Infrastructure.Data.Configurations.ServiceBookings
{
    /// <summary>
    /// EF mapping for <see cref="ServiceBookingReview"/>. Additive table backing
    /// the two-way service-booking review feature. One Active review per
    /// (BookingId, Direction) is enforced by a filtered unique index.
    /// </summary>
    public class ServiceBookingReviewConfiguration : IEntityTypeConfiguration<ServiceBookingReview>
    {
        public void Configure(EntityTypeBuilder<ServiceBookingReview> builder)
        {
            builder.ToTable("ServiceBookingReviews");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.BookingId).IsRequired();
            builder.Property(x => x.ReviewerUserId).IsRequired();
            builder.Property(x => x.RevieweeUserId).IsRequired();

            builder.Property(x => x.Direction)
                .HasConversion<int>()
                .IsRequired();

            builder.Property(x => x.Rating).IsRequired();
            builder.Property(x => x.Comment).HasMaxLength(1000);

            builder.Property(x => x.Status)
                .HasConversion<int>()
                .IsRequired();

            builder.Property(x => x.CreatedAtUtc).IsRequired();
            builder.Property(x => x.UpdatedAtUtc);
            builder.Property(x => x.DeletedAtUtc);

            // 1..5 — DB guard mirroring the polymorphic Reviews table.
            builder.ToTable(t => t.HasCheckConstraint(
                "CK_ServiceBookingReviews_Rating_1_5",
                "[Rating] BETWEEN 1 AND 5"));

            // Hot read path: all (Active) reviews for a booking.
            builder.HasIndex(x => new { x.BookingId, x.Status })
                .HasDatabaseName("IX_ServiceBookingReviews_Booking_Status");

            // One ACTIVE review per direction per booking. Filtered on the
            // integer value of ReviewStatus.Active (= 1) so a soft-deleted row
            // (Status = 3) never blocks a fresh review.
            builder.HasIndex(x => new { x.BookingId, x.Direction })
                .IsUnique()
                .HasFilter("[Status] = 1")
                .HasDatabaseName("UX_ServiceBookingReviews_Booking_Direction_Active");

            // FK to AspNetUsers (reviewer). Restrict on delete — match
            // ReviewConfiguration: deleting a user must not cascade-delete the
            // reviews they wrote (audit trail outweighs row count).
            builder.HasOne<User>()
                .WithMany()
                .HasForeignKey(x => x.ReviewerUserId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
