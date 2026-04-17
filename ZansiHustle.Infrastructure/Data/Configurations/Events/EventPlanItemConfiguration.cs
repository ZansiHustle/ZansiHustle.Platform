using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ZansiHustle.Domain.Events;

namespace ZansiHustle.Infrastructure.Data.Configurations.Events
{
    public class EventPlanItemConfiguration : IEntityTypeConfiguration<EventPlanItem>
    {
        public void Configure(EntityTypeBuilder<EventPlanItem> builder)
        {
            builder.ToTable("EventPlanItems");

            builder.HasKey(x => x.Id);

            builder.HasIndex(x => x.EventPlanId);
            builder.HasIndex(x => x.SellerSubcategoryId);
            builder.HasIndex(x => x.ListingId);
            builder.HasIndex(x => new { x.EventPlanId, x.CategorySlug }).IsUnique();

            builder.Property(x => x.CategorySlug)
                .IsRequired()
                .HasMaxLength(80);

            builder.Property(x => x.CategoryLabel)
                .IsRequired()
                .HasMaxLength(150);

            builder.Property(x => x.Priority)
                .HasConversion<int>()
                .IsRequired();

            builder.Property(x => x.EstimatedCost)
                .HasPrecision(18, 2);

            builder.Property(x => x.Notes)
                .HasMaxLength(1000);

            builder.Property(x => x.CreatedAtUtc).IsRequired();

            // Keep the item row when the underlying taxonomy changes — the slug +
            // label snapshots let the plan keep displaying the checklist.
            builder.HasOne(x => x.SellerSubcategory)
                .WithMany()
                .HasForeignKey(x => x.SellerSubcategoryId)
                .OnDelete(DeleteBehavior.SetNull);

            // Same principle for the chosen listing — if it's deleted later the
            // slot stays, just without a link.
            builder.HasOne(x => x.Listing)
                .WithMany()
                .HasForeignKey(x => x.ListingId)
                .OnDelete(DeleteBehavior.SetNull);
        }
    }
}
