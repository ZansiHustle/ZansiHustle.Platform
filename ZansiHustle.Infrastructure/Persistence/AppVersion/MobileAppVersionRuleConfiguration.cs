using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ZansiHustle.Domain.AppVersion;

namespace ZansiHustle.Infrastructure.Persistence.AppVersion
{
    /// <summary>
    /// EF configuration for <see cref="MobileAppVersionRule"/>. One row per
    /// (Platform, Channel) — enforced by a UNIQUE composite index. Enums persist as
    /// their int values (default EF behaviour). Auto-discovered by AppDbContext via
    /// ApplyConfigurationsFromAssembly.
    /// </summary>
    public sealed class MobileAppVersionRuleConfiguration : IEntityTypeConfiguration<MobileAppVersionRule>
    {
        public void Configure(EntityTypeBuilder<MobileAppVersionRule> builder)
        {
            builder.ToTable("MobileAppVersionRules");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.LatestVersion).HasMaxLength(40).IsRequired();
            builder.Property(x => x.MinimumSupportedVersion).HasMaxLength(40).IsRequired();
            builder.Property(x => x.Title).HasMaxLength(200);
            builder.Property(x => x.Message).HasMaxLength(1000);
            builder.Property(x => x.PrimaryButtonText).HasMaxLength(80);
            builder.Property(x => x.SecondaryButtonText).HasMaxLength(80);
            builder.Property(x => x.StoreUrl).HasMaxLength(1000);
            builder.Property(x => x.ReleaseNotes).HasMaxLength(4000);

            // Optimistic-concurrency token for admin edits.
            builder.Property(x => x.RowVersion).IsRowVersion();

            // One rule per store target.
            builder.HasIndex(x => new { x.Platform, x.Channel }).IsUnique();
            builder.HasIndex(x => x.IsEnabled);
        }
    }
}
