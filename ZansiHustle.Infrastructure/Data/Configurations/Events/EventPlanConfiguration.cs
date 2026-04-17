using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ZansiHustle.Domain.Events;

namespace ZansiHustle.Infrastructure.Data.Configurations.Events
{
    public class EventPlanConfiguration : IEntityTypeConfiguration<EventPlan>
    {
        public void Configure(EntityTypeBuilder<EventPlan> builder)
        {
            builder.ToTable("EventPlans");

            builder.HasKey(x => x.Id);

            builder.HasIndex(x => x.Code).IsUnique();
            builder.HasIndex(x => x.UserId);
            builder.HasIndex(x => x.EventType);
            builder.HasIndex(x => x.Status);
            builder.HasIndex(x => x.CreatedAtUtc);

            builder.Property(x => x.Code)
                .IsRequired()
                .HasMaxLength(60);

            builder.Property(x => x.EventType)
                .HasConversion<int>()
                .IsRequired();

            builder.Property(x => x.Status)
                .HasConversion<int>()
                .IsRequired();

            builder.Property(x => x.Title)
                .IsRequired()
                .HasMaxLength(150);

            builder.Property(x => x.LocationArea)
                .HasMaxLength(200);

            builder.Property(x => x.BudgetTotal)
                .HasPrecision(18, 2);

            builder.Property(x => x.Currency)
                .IsRequired()
                .HasMaxLength(8);

            builder.Property(x => x.Notes)
                .HasMaxLength(2000);

            builder.Property(x => x.CreatedAtUtc).IsRequired();

            builder.HasMany(x => x.Items)
                .WithOne(i => i.EventPlan)
                .HasForeignKey(i => i.EventPlanId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
