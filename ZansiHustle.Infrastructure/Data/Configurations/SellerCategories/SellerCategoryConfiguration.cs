using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ZansiHustle.Domain.SellerCategories;

namespace ZansiHustle.Infrastructure.Data.Configurations.SellerCategories
{
    public class SellerCategoryConfiguration : IEntityTypeConfiguration<SellerCategory>
    {
        public void Configure(EntityTypeBuilder<SellerCategory> builder)
        {
            builder.ToTable("SellerCategories");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Name)
                .IsRequired()
                .HasMaxLength(100);

            builder.HasIndex(x => x.Name)
                .IsUnique();

            builder.Property(x => x.Slug)
                .IsRequired()
                .HasMaxLength(120);

            builder.HasIndex(x => x.Slug)
                .IsUnique();

            builder.Property(x => x.Description)
                .HasMaxLength(500);

            builder.Property(x => x.SortOrder)
                .HasDefaultValue(0);

            builder.Property(x => x.IsActive)
                .HasDefaultValue(true);

            builder.Property(x => x.CreatedAtUtc)
                .IsRequired();

            builder.Property(x => x.UpdatedAtUtc)
                .IsRequired(false);

            builder.HasMany(x => x.Subcategories)
                .WithOne(x => x.SellerCategory)
                .HasForeignKey(x => x.SellerCategoryId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}