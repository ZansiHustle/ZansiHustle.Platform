using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ZansiHustle.Domain.Reports;

namespace ZansiHustle.Infrastructure.Persistence.Reports
{
    /// <summary>
    /// EF mapping for <see cref="ContentReport"/>. Auto-discovered by
    /// <c>ApplyConfigurationsFromAssembly</c> in AppDbContext.
    /// </summary>
    public class ContentReportConfiguration : IEntityTypeConfiguration<ContentReport>
    {
        public void Configure(EntityTypeBuilder<ContentReport> entity)
        {
            entity.ToTable("ContentReports");
            entity.HasKey(x => x.Id);

            entity.Property(x => x.ReporterUserId).IsRequired();
            entity.Property(x => x.TargetType).IsRequired();
            entity.Property(x => x.TargetId).IsRequired();
            entity.Property(x => x.Reason).IsRequired();
            entity.Property(x => x.Status).IsRequired();
            entity.Property(x => x.Description).HasMaxLength(1000);
            entity.Property(x => x.ResolutionNote).HasMaxLength(1000);

            // Moderation queue reads: newest pending first.
            entity.HasIndex(x => new { x.Status, x.CreatedAtUtc })
                .HasDatabaseName("IX_ContentReports_Status_Created");
            // Target lookups (has this object been reported? by whom?).
            entity.HasIndex(x => new { x.TargetType, x.TargetId })
                .HasDatabaseName("IX_ContentReports_Target");
            // A reporter's own history + duplicate-report guard.
            entity.HasIndex(x => new { x.ReporterUserId, x.TargetType, x.TargetId })
                .HasDatabaseName("IX_ContentReports_Reporter_Target");
        }
    }
}
