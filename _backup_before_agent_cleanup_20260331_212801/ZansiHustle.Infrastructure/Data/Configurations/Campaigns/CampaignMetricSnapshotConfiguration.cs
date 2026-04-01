using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ZansiHustle.Domain.Campaigns;

namespace ZansiHustle.Infrastructure.Data.Configurations.Campaigns
{
    /// <summary>
    /// Configures the database mapping for the <see cref="CampaignMetricSnapshot"/> entity.
    /// </summary>
    public class CampaignMetricSnapshotConfiguration : IEntityTypeConfiguration<CampaignMetricSnapshot>
    {
        /// <summary>
        /// Configures the entity.
        /// </summary>
        /// <param name="builder">The entity type builder.</param>
        public void Configure(EntityTypeBuilder<CampaignMetricSnapshot> builder)
        {
            builder.ToTable("CampaignMetricSnapshots");

            builder.HasKey(x => x.Id);

            builder.HasIndex(x => x.CampaignId);
            builder.HasIndex(x => x.SnapshotDateUtc);
            builder.HasIndex(x => x.Platform);

            builder.Property(x => x.Platform)
                .HasConversion<int?>();

            builder.Property(x => x.Spend)
                .HasPrecision(18, 2);

            builder.Property(x => x.SnapshotDateUtc)
                .IsRequired();

            builder.Property(x => x.CreatedAtUtc)
                .IsRequired();

            builder.HasOne(x => x.Campaign)
                .WithMany(x => x.MetricSnapshots)
                .HasForeignKey(x => x.CampaignId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
