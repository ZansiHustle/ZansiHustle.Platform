using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ZansiHustle.Domain.Notifications;

namespace ZansiHustle.Infrastructure.Data.Configurations.Notifications
{
    /// <summary>
    /// EF mapping for <see cref="Notification"/>. Additive table. Two indexes
    /// for the bell + page hot paths: (UserId, CreatedAtUtc) for the newest-first
    /// list and (UserId, IsRead) for the unread badge count.
    /// </summary>
    public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
    {
        public void Configure(EntityTypeBuilder<Notification> builder)
        {
            builder.ToTable("Notifications");

            builder.HasKey(x => x.Id);

            builder.HasIndex(x => new { x.UserId, x.CreatedAtUtc });
            builder.HasIndex(x => new { x.UserId, x.IsRead });

            builder.Property(x => x.Type)
                .HasConversion<int>()
                .IsRequired();

            builder.Property(x => x.Title).HasMaxLength(200).IsRequired();
            builder.Property(x => x.Body).HasMaxLength(1000).IsRequired();
            builder.Property(x => x.DataJson).HasMaxLength(2000);

            builder.Property(x => x.CreatedAtUtc).IsRequired();
        }
    }
}
