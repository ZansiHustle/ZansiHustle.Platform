using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ZansiHustle.Domain.Payments;

namespace ZansiHustle.Infrastructure.Data.Configurations.Payments
{
    public class PaymentConfiguration : IEntityTypeConfiguration<Payment>
    {
        public void Configure(EntityTypeBuilder<Payment> builder)
        {
            builder.ToTable("Payments");

            builder.HasKey(x => x.Id);

            builder.HasIndex(x => x.Code).IsUnique();
            builder.HasIndex(x => x.OrderId);
            builder.HasIndex(x => x.UserId);
            builder.HasIndex(x => x.Provider);
            builder.HasIndex(x => x.ProviderReference).IsUnique().HasFilter("[ProviderReference] IS NOT NULL");
            builder.HasIndex(x => x.Status);
            builder.HasIndex(x => x.CreatedAtUtc);

            builder.Property(x => x.Code)
                .IsRequired()
                .HasMaxLength(80);

            builder.Property(x => x.Provider)
                .IsRequired()
                .HasMaxLength(40);

            builder.Property(x => x.ProviderReference)
                .HasMaxLength(120);

            builder.Property(x => x.ProviderAuthorizationUrl)
                .HasMaxLength(1000);

            builder.Property(x => x.ProviderAccessCode)
                .HasMaxLength(200);

            builder.Property(x => x.Amount)
                .HasPrecision(18, 2)
                .IsRequired();

            builder.Property(x => x.Currency)
                .IsRequired()
                .HasMaxLength(8);

            builder.Property(x => x.Status)
                .HasConversion<int>()
                .IsRequired();

            builder.Property(x => x.FailureReason)
                .HasMaxLength(1000);

            builder.Property(x => x.ChannelUsed)
                .HasMaxLength(40);

            // RawProviderMetadata is nvarchar(max) for unbounded JSON storage.
            builder.Property(x => x.RawProviderMetadata)
                .HasColumnType("nvarchar(max)");

            builder.Property(x => x.CreatedAtUtc)
                .IsRequired();

            builder.HasOne(x => x.Order)
                .WithMany()
                .HasForeignKey(x => x.OrderId)
                .OnDelete(DeleteBehavior.Restrict); // never cascade-delete payment history

            builder.HasMany(x => x.Events)
                .WithOne(e => e.Payment)
                .HasForeignKey(e => e.PaymentId)
                .OnDelete(DeleteBehavior.SetNull); // keep event audit if payment row is ever purged
        }
    }
}
