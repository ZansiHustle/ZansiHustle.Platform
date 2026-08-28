using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ZansiHustle.Domain.ExternalCatalog;

namespace ZansiHustle.Infrastructure.Data.Configurations.ExternalCatalog
{
    public class ExternalCatalogSyncRunConfiguration : IEntityTypeConfiguration<ExternalCatalogSyncRun>
    {
        public void Configure(EntityTypeBuilder<ExternalCatalogSyncRun> builder)
        {
            builder.ToTable("ExternalCatalogSyncRuns");

            builder.HasKey(x => x.Id);

            builder.HasIndex(x => x.SourceCode);
            builder.HasIndex(x => x.Status);
            builder.HasIndex(x => x.StartedAtUtc);

            builder.Property(x => x.SourceCode).IsRequired().HasMaxLength(64);
            builder.Property(x => x.Status).HasConversion<int>().IsRequired();
            builder.Property(x => x.ErrorMessage).HasMaxLength(2000);
            builder.Property(x => x.StartedAtUtc).IsRequired();
        }
    }
}
