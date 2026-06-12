using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ZansiHustle.Domain.Wallets;

namespace ZansiHustle.Infrastructure.Data.Configurations.Wallets
{
    /// <summary>EF mapping for <see cref="WalletWithdrawalRequest"/>. Additive table.
    /// Only the last 4 account digits are stored — never the full number.</summary>
    public class WalletWithdrawalRequestConfiguration : IEntityTypeConfiguration<WalletWithdrawalRequest>
    {
        public void Configure(EntityTypeBuilder<WalletWithdrawalRequest> builder)
        {
            builder.ToTable("WalletWithdrawalRequests");
            builder.HasKey(x => x.Id);

            builder.HasIndex(x => x.UserId);
            builder.HasIndex(x => new { x.UserId, x.Status });

            builder.Property(x => x.Amount).HasPrecision(18, 2);
            builder.Property(x => x.Currency).HasMaxLength(3).IsRequired();

            builder.Property(x => x.BankName).HasMaxLength(120).IsRequired();
            builder.Property(x => x.AccountHolderName).HasMaxLength(120).IsRequired();
            builder.Property(x => x.AccountNumberLast4).HasMaxLength(4).IsRequired();
            builder.Property(x => x.BranchCode).HasMaxLength(20);
            builder.Property(x => x.AccountType).HasConversion<int>().IsRequired();
            builder.Property(x => x.Status).HasConversion<int>().IsRequired();

            builder.Property(x => x.Note).HasMaxLength(500);
            builder.Property(x => x.AdminNote).HasMaxLength(500);

            builder.Property(x => x.RequestedAtUtc).IsRequired();
        }
    }
}
