using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ZansiHustle.Domain.Events;

namespace ZansiHustle.Infrastructure.Data.Configurations.Events
{
    public class EventTypeTemplateConfiguration : IEntityTypeConfiguration<EventTypeTemplate>
    {
        public void Configure(EntityTypeBuilder<EventTypeTemplate> builder)
        {
            builder.ToTable("EventTypeTemplates");

            builder.HasKey(x => x.Id);

            builder.HasIndex(x => new { x.EventType, x.CategorySlug }).IsUnique();
            builder.HasIndex(x => x.EventType);

            builder.Property(x => x.EventType)
                .HasConversion<int>()
                .IsRequired();

            builder.Property(x => x.CategorySlug)
                .IsRequired()
                .HasMaxLength(80);

            builder.Property(x => x.DisplayLabel)
                .IsRequired()
                .HasMaxLength(150);

            builder.Property(x => x.Priority)
                .HasConversion<int>()
                .IsRequired();

            builder.Property(x => x.CreatedAtUtc).IsRequired();
        }
    }
}
