using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ZansiHustle.Domain.Influencers;

namespace ZansiHustle.Infrastructure.Data.Configurations.Influencers
{
    /// <summary>
    /// Configures the database mapping for the <see cref="InfluencerPlatformAccount"/> entity.
    /// </summary>
    public class InfluencerPlatformAccountConfiguration : IEntityTypeConfiguration<InfluencerPlatformAccount>
    {
        /// <summary>
        /// Configures the entity.
        /// </summary>
        /// <param name="builder">The entity type builder.</param>
        public void Configure(EntityTypeBuilder<InfluencerPlatformAccount> builder)
        {
            builder.ToTable("InfluencerPlatformAccounts");

            builder.HasKey(x => x.Id);

            builder.HasIndex(x => x.InfluencerId);
            builder.HasIndex(x => x.Platform);

            builder.Property(x => x.Platform)
                .HasConversion<int>()
                .IsRequired();

            builder.Property(x => x.Handle)
                .HasMaxLength(250);

            builder.Property(x => x.Url)
                .HasMaxLength(500);

            builder.Property(x => x.CreatedAtUtc)
                .IsRequired();

            builder.HasOne(x => x.Influencer)
                .WithMany(x => x.PlatformAccounts)
                .HasForeignKey(x => x.InfluencerId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
