using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ZansiHustle.Domain.ExternalCatalog;

namespace ZansiHustle.Infrastructure.Data.Configurations.ExternalCatalog
{
    public class ExternalCategoryMappingConfiguration : IEntityTypeConfiguration<ExternalCategoryMapping>
    {
        public void Configure(EntityTypeBuilder<ExternalCategoryMapping> builder)
        {
            builder.ToTable("ExternalCategoryMappings");

            builder.HasKey(x => x.Id);

            builder.HasIndex(x => new { x.SourceCode, x.ExternalCategoryCode }).IsUnique();

            builder.Property(x => x.SourceCode).IsRequired().HasMaxLength(64);
            builder.Property(x => x.ExternalCategoryCode).IsRequired().HasMaxLength(120);
            builder.Property(x => x.CreatedAtUtc).IsRequired();
        }
    }
}
