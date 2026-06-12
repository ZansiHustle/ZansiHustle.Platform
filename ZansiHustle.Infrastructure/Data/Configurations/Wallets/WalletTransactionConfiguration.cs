using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ZansiHustle.Domain.Wallets;

namespace ZansiHustle.Infrastructure.Data.Configurations.Wallets
{
    /// <summary>
    /// EF mapping for <see cref="WalletTransaction"/>. Additive ledger table.
    /// The filtered unique index on (Type, ReferenceType, ReferenceId) is the
    /// HARD idempotency guard for reference-based credits (e.g. a booking
    /// rejection can only ever credit once), applied only when ReferenceId is set
    /// so reference-less entries (admin adjustments) aren't constrained.
    ///
    /// Wallet-PAYMENT debit/reversal (types 100/101) are EXCLUDED from the unique
    /// guard: an order can legitimately cycle apply → reverse → re-apply (cancel a
    /// split payment, then retry with wallet again), so multiple rows per order are
    /// expected. Their idempotency is enforced in WalletService via the net-held
    /// computation (reuse when net &gt; 0; reversal is a no-op when net ≤ 0).
    /// </summary>
    public class WalletTransactionConfiguration : IEntityTypeConfiguration<WalletTransaction>
    {
        // WalletTransactionType.WalletPaymentDebit / WalletPaymentReversal.
        private const int WalletPaymentDebit = 100;
        private const int WalletPaymentReversal = 101;

        public void Configure(EntityTypeBuilder<WalletTransaction> builder)
        {
            builder.ToTable("WalletTransactions");
            builder.HasKey(x => x.Id);

            builder.HasIndex(x => new { x.UserId, x.CreatedAtUtc });
            builder.HasIndex(x => new { x.Type, x.ReferenceType, x.ReferenceId })
                .IsUnique()
                .HasFilter(
                    $"[ReferenceId] IS NOT NULL AND [Type] NOT IN ({WalletPaymentDebit}, {WalletPaymentReversal})");

            builder.Property(x => x.Type).HasConversion<int>().IsRequired();
            builder.Property(x => x.Direction).HasConversion<int>().IsRequired();
            builder.Property(x => x.Status).HasConversion<int>().IsRequired();

            builder.Property(x => x.Amount).HasPrecision(18, 2);
            builder.Property(x => x.BalanceAfter).HasPrecision(18, 2);
            builder.Property(x => x.Currency).HasMaxLength(3).IsRequired();
            builder.Property(x => x.ReferenceType).HasMaxLength(50);
            builder.Property(x => x.Description).HasMaxLength(300).IsRequired();
            builder.Property(x => x.CreatedAtUtc).IsRequired();
        }
    }
}
