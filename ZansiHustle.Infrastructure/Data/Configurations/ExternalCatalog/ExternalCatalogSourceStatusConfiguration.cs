using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ZansiHustle.Domain.ExternalCatalog;

namespace ZansiHustle.Infrastructure.Data.Configurations.ExternalCatalog
{
    public class ExternalCatalogSourceStatusConfiguration : IEntityTypeConfiguration<ExternalCatalogSourceStatus>
    {
        public void Configure(EntityTypeBuilder<ExternalCatalogSourceStatus> builder)
        {
            builder.ToTable("ExternalCatalogSourceStatuses");

            builder.HasKey(x => x.Id);

            builder.HasIndex(x => x.SourceCode).IsUnique();

            builder.Property(x => x.SourceCode).IsRequired().HasMaxLength(64);
            builder.Property(x => x.LastActivityType).HasMaxLength(40);
            builder.Property(x => x.LastError).HasMaxLength(2000);
            builder.Property(x => x.CreatedAtUtc).IsRequired();
        }
    }
}
