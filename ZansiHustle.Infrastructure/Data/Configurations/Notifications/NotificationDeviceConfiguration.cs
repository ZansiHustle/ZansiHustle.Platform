using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ZansiHustle.Domain.Notifications;

namespace ZansiHustle.Infrastructure.Data.Configurations.Notifications
{
    /// <summary>
    /// EF mapping for <see cref="NotificationDevice"/>. Additive table. A push
    /// player id is unique per provider (one owner at a time — re-registering
    /// re-homes it); UserId is indexed for delivery lookups.
    /// </summary>
    public class NotificationDeviceConfiguration : IEntityTypeConfiguration<NotificationDevice>
    {
        public void Configure(EntityTypeBuilder<NotificationDevice> builder)
        {
            builder.ToTable("NotificationDevices");

            builder.HasKey(x => x.Id);

            builder.HasIndex(x => new { x.UserId, x.IsActive });
            builder.HasIndex(x => new { x.Provider, x.PlayerId }).IsUnique();

            builder.Property(x => x.Provider)
                .HasConversion<int>()
                .IsRequired();

            builder.Property(x => x.Platform)
                .HasConversion<int>()
                .IsRequired();

            builder.Property(x => x.PlayerId).HasMaxLength(256).IsRequired();
            builder.Property(x => x.AppVersion).HasMaxLength(50);
            builder.Property(x => x.DeviceName).HasMaxLength(150);

            builder.Property(x => x.CreatedAtUtc).IsRequired();
            builder.Property(x => x.LastSeenAtUtc).IsRequired();
        }
    }
}
