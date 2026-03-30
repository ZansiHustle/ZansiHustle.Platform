using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ZansiHustle.Domain.ContentTasks;

namespace ZansiHustle.Infrastructure.Data.Configurations.ContentTasks
{
    /// <summary>
    /// Configures the database mapping for the <see cref="ContentTask"/> entity.
    /// </summary>
    public class ContentTaskConfiguration : IEntityTypeConfiguration<ContentTask>
    {
        /// <summary>
        /// Configures the entity.
        /// </summary>
        /// <param name="builder">The entity type builder.</param>
        public void Configure(EntityTypeBuilder<ContentTask> builder)
        {
            builder.ToTable("ContentTasks");

            builder.HasKey(x => x.Id);

            builder.HasIndex(x => x.Status);
            builder.HasIndex(x => x.Priority);
            builder.HasIndex(x => x.DueDateUtc);
            builder.HasIndex(x => x.CampaignId);

            builder.Property(x => x.Title)
                .IsRequired()
                .HasMaxLength(250);

            builder.Property(x => x.Status)
                .HasConversion<int>()
                .IsRequired();

            builder.Property(x => x.Priority)
                .HasConversion<int>()
                .IsRequired();

            builder.Property(x => x.Notes)
                .HasMaxLength(2000);

            builder.Property(x => x.CreatedAtUtc)
                .IsRequired();

            builder.HasOne(x => x.Campaign)
                .WithMany(x => x.ContentTasks)
                .HasForeignKey(x => x.CampaignId)
                .OnDelete(DeleteBehavior.SetNull);
        }
    }
}
