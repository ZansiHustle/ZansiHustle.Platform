using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ZansiHustle.Domain.Finance;

namespace ZansiHustle.Infrastructure.Data.Configurations.Finance
{
    /// <summary>
    /// EF mapping for <see cref="PlatformLedgerEntry"/>. Append-only platform book.
    /// The filtered UNIQUE index on (OrderId, EntryType) WHERE OrderId IS NOT NULL
    /// guards against duplicate backfill rows per order/entry-type. Money column is
    /// decimal(18,2). No FKs (operational/audit data outlives refs).
    /// </summary>
    public sealed class PlatformLedgerEntryConfiguration : IEntityTypeConfiguration<PlatformLedgerEntry>
    {
        public void Configure(EntityTypeBuilder<PlatformLedgerEntry> builder)
        {
            builder.ToTable("PlatformLedgerEntries");
            builder.HasKey(x => x.Id);

            builder.HasIndex(x => new { x.OrderId, x.EntryType })
                .IsUnique()
                .HasFilter("[OrderId] IS NOT NULL");

            builder.Property(x => x.SourceType).HasConversion<int>().IsRequired();
            builder.Property(x => x.EntryType).HasConversion<int>().IsRequired();

            builder.Property(x => x.Amount).HasPrecision(18, 2);

            builder.Property(x => x.Currency).HasMaxLength(8).IsRequired();
            builder.Property(x => x.Notes).HasMaxLength(500);

            builder.Property(x => x.OccurredAtUtc).IsRequired();
            builder.Property(x => x.CreatedAtUtc).IsRequired();
        }
    }
}
