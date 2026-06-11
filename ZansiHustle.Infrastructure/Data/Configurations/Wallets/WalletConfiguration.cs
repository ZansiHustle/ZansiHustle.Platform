using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ZansiHustle.Domain.Wallets;

namespace ZansiHustle.Infrastructure.Data.Configurations.Wallets
{
    /// <summary>EF mapping for <see cref="Wallet"/>. One wallet per user
    /// (unique UserId). Additive table.</summary>
    public class WalletConfiguration : IEntityTypeConfiguration<Wallet>
    {
        public void Configure(EntityTypeBuilder<Wallet> builder)
        {
            builder.ToTable("Wallets");
            builder.HasKey(x => x.Id);

            builder.HasIndex(x => x.UserId).IsUnique();

            builder.Property(x => x.Currency).HasMaxLength(3).IsRequired();
            builder.Property(x => x.AvailableBalance).HasPrecision(18, 2);
            builder.Property(x => x.CreatedAtUtc).IsRequired();
            builder.Property(x => x.UpdatedAtUtc).IsRequired();
        }
    }
}
