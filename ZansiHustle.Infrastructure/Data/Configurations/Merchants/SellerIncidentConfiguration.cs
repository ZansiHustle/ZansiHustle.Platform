using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ZansiHustle.Domain.Merchants;

namespace ZansiHustle.Infrastructure.Data.Configurations.Merchants
{
    /// <summary>
    /// EF mapping for <see cref="SellerIncident"/>. Additive, append-only table.
    /// Indexed by merchant (+ status) for the seller-accountability views and by
    /// order for incident-per-order lookups. No cascade from Merchant deletion is
    /// forced here beyond the default; the FK is optional on order.
    /// </summary>
    public class SellerIncidentConfiguration : IEntityTypeConfiguration<SellerIncident>
    {
        public void Configure(EntityTypeBuilder<SellerIncident> builder)
        {
            builder.ToTable("SellerIncidents");
            builder.HasKey(x => x.Id);

            builder.HasIndex(x => new { x.MerchantId, x.Status });
            builder.HasIndex(x => x.OrderId);

            builder.Property(x => x.IncidentType).HasConversion<int>().IsRequired();
            builder.Property(x => x.Severity).HasConversion<int>().IsRequired();
            builder.Property(x => x.Status).HasConversion<int>().IsRequired();

            builder.Property(x => x.Amount).HasPrecision(18, 2);
            builder.Property(x => x.Currency).HasMaxLength(3).IsRequired();
            builder.Property(x => x.Reason).HasMaxLength(500).IsRequired();
            builder.Property(x => x.AdminNotes).HasMaxLength(1000);
            builder.Property(x => x.CreatedAtUtc).IsRequired();

            builder.HasOne(x => x.Merchant)
                .WithMany()
                .HasForeignKey(x => x.MerchantId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
