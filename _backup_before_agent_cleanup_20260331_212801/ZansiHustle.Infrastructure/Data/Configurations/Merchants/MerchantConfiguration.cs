using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ZansiHustle.Domain.Merchants;

namespace ZansiHustle.Infrastructure.Data.Configurations.Merchants
{
    /// <summary>
    /// Configures the database mapping for the <see cref="Merchant"/> entity.
    /// </summary>
    public class MerchantConfiguration : IEntityTypeConfiguration<Merchant>
    {
        /// <summary>
        /// Configures the entity.
        /// </summary>
        /// <param name="builder">The entity type builder.</param>
        public void Configure(EntityTypeBuilder<Merchant> builder)
        {
            builder.ToTable("Merchants");

            builder.HasKey(x => x.Id);

            builder.HasIndex(x => x.Code).IsUnique();
            builder.HasIndex(x => x.Name);
            builder.HasIndex(x => x.Type);
            builder.HasIndex(x => x.Status);
            builder.HasIndex(x => x.KycStatus);
            builder.HasIndex(x => x.OwnerUserId);
            builder.HasIndex(x => x.ContactEmail);
            builder.HasIndex(x => x.ContactPhoneNumber);
            builder.HasIndex(x => x.Province);
            builder.HasIndex(x => x.City);
            builder.HasIndex(x => x.IsPayoutEligible);

            builder.Property(x => x.Code)
                .IsRequired()
                .HasMaxLength(50);

            builder.Property(x => x.Name)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(x => x.Description)
                .HasMaxLength(2000);

            builder.Property(x => x.Type)
                .HasConversion<int>()
                .IsRequired();

            builder.Property(x => x.Status)
                .HasConversion<int>()
                .IsRequired();

            builder.Property(x => x.KycStatus)
                .HasConversion<int>()
                .IsRequired();

            builder.Property(x => x.ContactEmail)
                .HasMaxLength(256);

            builder.Property(x => x.ContactPhoneNumber)
                .HasMaxLength(30);

            builder.Property(x => x.Province)
                .HasMaxLength(150);

            builder.Property(x => x.City)
                .HasMaxLength(150);

            builder.Property(x => x.AddressLine1)
                .HasMaxLength(300);

            builder.Property(x => x.WebsiteUrl)
                .HasMaxLength(500);

            builder.Property(x => x.LogoUrl)
                .HasMaxLength(500);

            builder.Property(x => x.BannerUrl)
                .HasMaxLength(500);

            builder.Property(x => x.Rating)
                .HasPrecision(5, 2);

            builder.Property(x => x.TotalRevenue)
                .HasPrecision(18, 2);

            builder.Property(x => x.CreatedAtUtc)
                .IsRequired();
        }
    }
}