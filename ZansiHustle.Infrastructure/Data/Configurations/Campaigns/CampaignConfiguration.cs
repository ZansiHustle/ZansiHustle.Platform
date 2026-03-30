using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ZansiHustle.Domain.Campaigns;

namespace ZansiHustle.Infrastructure.Data.Configurations.Campaigns
{
    /// <summary>
    /// Configures the database mapping for the <see cref="Campaign"/> entity.
    /// </summary>
    public class CampaignConfiguration : IEntityTypeConfiguration<Campaign>
    {
        /// <summary>
        /// Configures the entity.
        /// </summary>
        /// <param name="builder">The entity type builder.</param>
        public void Configure(EntityTypeBuilder<Campaign> builder)
        {
            builder.ToTable("Campaigns");

            builder.HasKey(x => x.Id);

            builder.HasIndex(x => x.Code).IsUnique();
            builder.HasIndex(x => x.Name);
            builder.HasIndex(x => x.Status);
            builder.HasIndex(x => x.PrimaryPlatform);

            builder.Property(x => x.Code)
                .IsRequired()
                .HasMaxLength(50);

            builder.Property(x => x.Name)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(x => x.Description)
                .HasMaxLength(2000);

            builder.Property(x => x.CampaignType)
                .HasMaxLength(120);

            builder.Property(x => x.Status)
                .HasConversion<int>()
                .IsRequired();

            builder.Property(x => x.PrimaryPlatform)
                .HasConversion<int?>();

            builder.Property(x => x.Budget)
                .HasPrecision(18, 2);

            builder.Property(x => x.StartDateUtc)
                .IsRequired();

            builder.Property(x => x.EndDateUtc)
                .IsRequired();

            builder.Property(x => x.CreatedAtUtc)
                .IsRequired();

            builder.HasMany(x => x.MetricSnapshots)
                .WithOne(x => x.Campaign)
                .HasForeignKey(x => x.CampaignId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(x => x.ContentTasks)
                .WithOne(x => x.Campaign)
                .HasForeignKey(x => x.CampaignId)
                .OnDelete(DeleteBehavior.SetNull);
        }
    }
}
