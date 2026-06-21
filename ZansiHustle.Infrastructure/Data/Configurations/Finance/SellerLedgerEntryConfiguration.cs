using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ZansiHustle.Domain.Finance;

namespace ZansiHustle.Infrastructure.Data.Configurations.Finance
{
    /// <summary>
    /// EF mapping for <see cref="SellerLedgerEntry"/>. Append-only seller book.
    /// The filtered UNIQUE index on (OrderId, EntryType) WHERE OrderId IS NOT NULL
    /// is the HARD idempotency guard for backfill/reconcile — an order can only
    /// ever produce one SellerNetCredit row, even under concurrent runs. All money
    /// columns are decimal(18,2). No FKs (operational/audit data outlives refs).
    /// </summary>
    public sealed class SellerLedgerEntryConfiguration : IEntityTypeConfiguration<SellerLedgerEntry>
    {
        public void Configure(EntityTypeBuilder<SellerLedgerEntry> builder)
        {
            builder.ToTable("SellerLedgerEntries");
            builder.HasKey(x => x.Id);

            builder.HasIndex(x => new { x.OrderId, x.EntryType })
                .IsUnique()
                .HasFilter("[OrderId] IS NOT NULL");
            builder.HasIndex(x => new { x.SellerUserId, x.Status });

            builder.Property(x => x.SourceType).HasConversion<int>().IsRequired();
            builder.Property(x => x.EntryType).HasConversion<int>().IsRequired();
            builder.Property(x => x.Status).HasConversion<int>().IsRequired();

            builder.Property(x => x.GrossAmount).HasPrecision(18, 2);
            builder.Property(x => x.EligibleBaseAmount).HasPrecision(18, 2);
            builder.Property(x => x.GatewayFeeAmount).HasPrecision(18, 2);
            builder.Property(x => x.PlatformFeeAmount).HasPrecision(18, 2);
            builder.Property(x => x.SellerNetAmount).HasPrecision(18, 2);
            builder.Property(x => x.DeliveryFeeAmount).HasPrecision(18, 2);

            builder.Property(x => x.Currency).HasMaxLength(8).IsRequired();
            builder.Property(x => x.Notes).HasMaxLength(500);

            builder.Property(x => x.OccurredAtUtc).IsRequired();
            builder.Property(x => x.CreatedAtUtc).IsRequired();
        }
    }
}
