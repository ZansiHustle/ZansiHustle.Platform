using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ZansiHustle.Domain.Media;

namespace ZansiHustle.Infrastructure.Data.Configurations.Media
{
    public class MediaAssetConfiguration : IEntityTypeConfiguration<MediaAsset>
    {
        public void Configure(EntityTypeBuilder<MediaAsset> builder)
        {
            builder.ToTable("MediaAssets");

            builder.HasKey(x => x.Id);

            // Owner-shaped lookups dominate this table — admin loading a
            // merchant's verification docs, a listing's gallery, etc.
            builder.HasIndex(x => new { x.OwnerEntityType, x.OwnerEntityId });
            builder.HasIndex(x => new { x.OwnerEntityType, x.OwnerEntityId, x.Purpose });
            builder.HasIndex(x => x.UploadedByUserId);
            builder.HasIndex(x => x.Status);
            // Globally unique storage key — a finalize call for the same key
            // twice should not be possible.
            builder.HasIndex(x => x.StorageKey).IsUnique();

            builder.Property(x => x.OwnerEntityType).HasConversion<int>().IsRequired();
            builder.Property(x => x.Kind).HasConversion<int>().IsRequired();
            builder.Property(x => x.Purpose).HasConversion<int>().IsRequired();
            builder.Property(x => x.Visibility).HasConversion<int>().IsRequired();
            builder.Property(x => x.Status).HasConversion<int>().IsRequired();

            builder.Property(x => x.StorageContainer).IsRequired().HasMaxLength(50);
            builder.Property(x => x.StorageKey).IsRequired().HasMaxLength(400);
            builder.Property(x => x.FileName).IsRequired().HasMaxLength(300);
            builder.Property(x => x.ContentType).IsRequired().HasMaxLength(120);
            builder.Property(x => x.ThumbnailStorageKey).HasMaxLength(400);
            builder.Property(x => x.RejectionReason).HasMaxLength(500);

            builder.Property(x => x.CreatedAtUtc).IsRequired();
        }
    }
}
