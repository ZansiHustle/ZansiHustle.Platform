using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ZansiHustle.Domain.AppConfigs;

namespace ZansiHustle.Infrastructure.Data.Configurations.AppConfigs
{
    /// <summary>
    /// Configures the database mapping for the <see cref="AppRuntimeConfig"/> entity.
    /// </summary>
    public class AppRuntimeConfigConfiguration : IEntityTypeConfiguration<AppRuntimeConfig>
    {
        public void Configure(EntityTypeBuilder<AppRuntimeConfig> builder)
        {
            builder.ToTable("AppRuntimeConfigs");

            builder.HasKey(x => x.Id);

            builder.HasIndex(x => x.Key).IsUnique();
            builder.HasIndex(x => x.Category);
            builder.HasIndex(x => x.IsPublic);

            builder.Property(x => x.Key)
                .IsRequired()
                .HasMaxLength(120);

            builder.Property(x => x.DisplayName)
                .IsRequired()
                .HasMaxLength(160);

            builder.Property(x => x.Description)
                .HasMaxLength(1000);

            builder.Property(x => x.Category)
                .IsRequired()
                .HasMaxLength(60);

            builder.Property(x => x.ValueType)
                .IsRequired()
                .HasMaxLength(20);

            builder.Property(x => x.StringValue)
                .HasMaxLength(2000);

            builder.Property(x => x.DisabledTitle)
                .HasMaxLength(160);

            builder.Property(x => x.DisabledMessage)
                .HasMaxLength(600);

            builder.Property(x => x.CreatedAtUtc)
                .IsRequired();
        }
    }
}
