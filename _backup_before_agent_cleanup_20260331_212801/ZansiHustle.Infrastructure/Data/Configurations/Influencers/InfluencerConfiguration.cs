using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ZansiHustle.Domain.Influencers;

namespace ZansiHustle.Infrastructure.Data.Configurations.Influencers
{
    /// <summary>
    /// Configures the database mapping for the <see cref="Influencer"/> entity.
    /// </summary>
    public class InfluencerConfiguration : IEntityTypeConfiguration<Influencer>
    {
        /// <summary>
        /// Configures the entity.
        /// </summary>
        /// <param name="builder">The entity type builder.</param>
        public void Configure(EntityTypeBuilder<Influencer> builder)
        {
            builder.ToTable("Influencers");

            builder.HasKey(x => x.Id);

            builder.HasIndex(x => x.Code).IsUnique();
            builder.HasIndex(x => x.Email);
            builder.HasIndex(x => x.PhoneNumber);
            builder.HasIndex(x => x.Status);
            builder.HasIndex(x => x.Niche);

            builder.Property(x => x.Code)
                .IsRequired()
                .HasMaxLength(50);

            builder.Property(x => x.FullName)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(x => x.Niche)
                .HasMaxLength(120);

            builder.Property(x => x.Province)
                .HasMaxLength(150);

            builder.Property(x => x.City)
                .HasMaxLength(150);

            builder.Property(x => x.Email)
                .HasMaxLength(256);

            builder.Property(x => x.PhoneNumber)
                .HasMaxLength(30);

            builder.Property(x => x.Notes)
                .HasMaxLength(3000);

            builder.Property(x => x.Rate)
                .HasPrecision(18, 2);

            builder.Property(x => x.Status)
                .HasConversion<int>()
                .IsRequired();

            builder.Property(x => x.CreatedAtUtc)
                .IsRequired();

            builder.HasMany(x => x.PlatformAccounts)
                .WithOne(x => x.Influencer)
                .HasForeignKey(x => x.InfluencerId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
