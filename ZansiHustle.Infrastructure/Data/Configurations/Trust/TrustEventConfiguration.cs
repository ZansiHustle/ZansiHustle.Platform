using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ZansiHustle.Domain.Trust;

namespace ZansiHustle.Infrastructure.Data.Configurations.Trust
{
    /// <summary>EF mapping for <see cref="TrustEvent"/>. Append-only history.</summary>
    public class TrustEventConfiguration : IEntityTypeConfiguration<TrustEvent>
    {
        public void Configure(EntityTypeBuilder<TrustEvent> builder)
        {
            builder.ToTable("TrustEvents");
            builder.HasKey(x => x.Id);

            builder.HasIndex(x => new { x.UserId, x.CreatedAtUtc });
            builder.HasIndex(x => new { x.ReferenceType, x.ReferenceId });

            builder.Property(x => x.ActorRole).HasConversion<int>().IsRequired();
            builder.Property(x => x.Type).HasConversion<int>().IsRequired();
            builder.Property(x => x.ReferenceType).HasMaxLength(50);
            builder.Property(x => x.ScoreImpact).HasPrecision(9, 4);
            builder.Property(x => x.MetadataJson).HasMaxLength(2000);
            builder.Property(x => x.CreatedAtUtc).IsRequired();
        }
    }
}
