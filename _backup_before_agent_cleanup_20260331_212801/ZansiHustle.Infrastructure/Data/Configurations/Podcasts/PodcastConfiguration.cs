using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ZansiHustle.Domain.Podcasts;

namespace ZansiHustle.Infrastructure.Data.Configurations.Podcasts
{
    /// <summary>
    /// Configures the database mapping for the <see cref="Podcast"/> entity.
    /// </summary>
    public class PodcastConfiguration : IEntityTypeConfiguration<Podcast>
    {
        /// <summary>
        /// Configures the entity.
        /// </summary>
        /// <param name="builder">The entity type builder.</param>
        public void Configure(EntityTypeBuilder<Podcast> builder)
        {
            builder.ToTable("Podcasts");

            builder.HasKey(x => x.Id);

            builder.HasIndex(x => x.Code).IsUnique();
            builder.HasIndex(x => x.Name);
            builder.HasIndex(x => x.ContactEmail);
            builder.HasIndex(x => x.OutreachStatus);
            builder.HasIndex(x => x.ResponseStatus);

            builder.Property(x => x.Code)
                .IsRequired()
                .HasMaxLength(50);

            builder.Property(x => x.Name)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(x => x.HostName)
                .HasMaxLength(200);

            builder.Property(x => x.Country)
                .HasMaxLength(120);

            builder.Property(x => x.Region)
                .HasMaxLength(120);

            builder.Property(x => x.Niche)
                .HasMaxLength(120);

            builder.Property(x => x.WebsiteUrl)
                .HasMaxLength(500);

            builder.Property(x => x.ContactEmail)
                .HasMaxLength(256);

            builder.Property(x => x.MediaKitUrl)
                .HasMaxLength(500);

            builder.Property(x => x.Notes)
                .HasMaxLength(3000);

            builder.Property(x => x.QuotedPrice)
                .HasPrecision(18, 2);

            builder.Property(x => x.OutreachStatus)
                .HasConversion<int>()
                .IsRequired();

            builder.Property(x => x.ResponseStatus)
                .HasConversion<int>()
                .IsRequired();

            builder.Property(x => x.CreatedAtUtc)
                .IsRequired();

            builder.HasMany(x => x.AdFormats)
                .WithOne(x => x.Podcast)
                .HasForeignKey(x => x.PodcastId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
