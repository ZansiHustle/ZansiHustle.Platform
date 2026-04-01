using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ZansiHustle.Domain.Podcasts;

namespace ZansiHustle.Infrastructure.Data.Configurations.Podcasts
{
    /// <summary>
    /// Configures the database mapping for the <see cref="PodcastAdFormat"/> entity.
    /// </summary>
    public class PodcastAdFormatConfiguration : IEntityTypeConfiguration<PodcastAdFormat>
    {
        /// <summary>
        /// Configures the entity.
        /// </summary>
        /// <param name="builder">The entity type builder.</param>
        public void Configure(EntityTypeBuilder<PodcastAdFormat> builder)
        {
            builder.ToTable("PodcastAdFormats");

            builder.HasKey(x => x.Id);

            builder.HasIndex(x => x.PodcastId);

            builder.Property(x => x.FormatName)
                .IsRequired()
                .HasMaxLength(120);

            builder.Property(x => x.CreatedAtUtc)
                .IsRequired();

            builder.HasOne(x => x.Podcast)
                .WithMany(x => x.AdFormats)
                .HasForeignKey(x => x.PodcastId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
