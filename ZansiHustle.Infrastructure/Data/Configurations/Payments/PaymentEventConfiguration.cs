using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ZansiHustle.Domain.Payments;

namespace ZansiHustle.Infrastructure.Data.Configurations.Payments
{
    public class PaymentEventConfiguration : IEntityTypeConfiguration<PaymentEvent>
    {
        public void Configure(EntityTypeBuilder<PaymentEvent> builder)
        {
            builder.ToTable("PaymentEvents");

            builder.HasKey(x => x.Id);

            // The primary idempotency barrier: two events with the same dedup key
            // can never both persist.
            builder.HasIndex(x => x.ProviderEventKey).IsUnique();
            builder.HasIndex(x => x.PaymentId);
            builder.HasIndex(x => x.Provider);
            builder.HasIndex(x => x.EventType);
            builder.HasIndex(x => x.ReceivedAtUtc);

            builder.Property(x => x.Provider)
                .IsRequired()
                .HasMaxLength(40);

            builder.Property(x => x.ProviderEventKey)
                .IsRequired()
                .HasMaxLength(300);

            builder.Property(x => x.EventType)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(x => x.RawPayload)
                .IsRequired()
                .HasColumnType("nvarchar(max)");

            builder.Property(x => x.SignatureHeader)
                .HasMaxLength(300);

            builder.Property(x => x.ProcessingError)
                .HasColumnType("nvarchar(max)");

            builder.Property(x => x.ReceivedAtUtc)
                .IsRequired();
        }
    }
}
