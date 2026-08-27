using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ZansiHustle.Domain.Payments.External;

namespace ZansiHustle.Infrastructure.Data.Configurations.Payments
{
    public class ExternalPaymentSessionConfiguration : IEntityTypeConfiguration<ExternalPaymentSession>
    {
        public void Configure(EntityTypeBuilder<ExternalPaymentSession> builder)
        {
            builder.ToTable("ExternalPaymentSessions");

            builder.HasKey(x => x.Id);

            // Idempotency barrier: at most one non-terminal-failed session per
            // (ShopCode, ExternalOrderId). Status 5/6/7 = Failed/Cancelled/Expired —
            // excluded so a genuinely failed attempt never blocks a fresh retry.
            builder.HasIndex(x => new { x.ShopCode, x.ExternalOrderId })
                .IsUnique()
                .HasFilter("[Status] NOT IN (5,6,7)")
                .HasDatabaseName("IX_ExternalPaymentSessions_ActiveShopOrder");

            builder.HasIndex(x => x.ProviderReference).IsUnique().HasFilter("[ProviderReference] IS NOT NULL");
            builder.HasIndex(x => x.ShopCode);
            builder.HasIndex(x => x.Status);
            builder.HasIndex(x => x.CreatedAtUtc);
            builder.HasIndex(x => x.IsTest);

            builder.Property(x => x.ShopCode).IsRequired().HasMaxLength(64);
            builder.Property(x => x.ShopName).IsRequired().HasMaxLength(120);
            builder.Property(x => x.ExternalOrderId).IsRequired().HasMaxLength(120);
            builder.Property(x => x.ExternalOrderNumber).IsRequired().HasMaxLength(120);

            builder.Property(x => x.Amount).HasPrecision(18, 2).IsRequired();
            builder.Property(x => x.Currency).IsRequired().HasMaxLength(8);
            builder.Property(x => x.CustomerEmail).HasMaxLength(256);

            builder.Property(x => x.ReturnUrl).IsRequired().HasMaxLength(1000);
            builder.Property(x => x.CallbackUrl).IsRequired().HasMaxLength(1000);

            builder.Property(x => x.Provider).IsRequired().HasMaxLength(40);
            builder.Property(x => x.ProviderReference).HasMaxLength(120);
            builder.Property(x => x.ProviderAuthorizationUrl).HasMaxLength(1000);
            builder.Property(x => x.ProviderAccessCode).HasMaxLength(200);

            builder.Property(x => x.Status).HasConversion<int>().IsRequired();
            builder.Property(x => x.FailureReason).HasMaxLength(1000);

            builder.Property(x => x.IsTest).HasDefaultValue(false).IsRequired();

            builder.Property(x => x.CreatedAtUtc).IsRequired();

            builder.Property(x => x.CallbackAttemptCount).HasDefaultValue(0).IsRequired();
            builder.Property(x => x.LastCallbackError).HasMaxLength(1000);
        }
    }
}
