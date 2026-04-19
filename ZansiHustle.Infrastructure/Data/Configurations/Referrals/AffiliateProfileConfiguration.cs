using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ZansiHustle.Domain.Referrals;

namespace ZansiHustle.Infrastructure.Data.Configurations.Referrals
{
    public class AffiliateProfileConfiguration : IEntityTypeConfiguration<AffiliateProfile>
    {
        public void Configure(EntityTypeBuilder<AffiliateProfile> builder)
        {
            builder.ToTable("AffiliateProfiles");

            builder.HasKey(x => x.Id);

            // One profile per user; one slug globally.
            builder.HasIndex(x => x.UserId).IsUnique();
            builder.HasIndex(x => x.ReferralCode).IsUnique();
            builder.HasIndex(x => x.IsActive);

            builder.Property(x => x.ReferralCode)
                .IsRequired()
                .HasMaxLength(60);

            builder.Property(x => x.Tier)
                .HasMaxLength(40);

            builder.Property(x => x.Notes)
                .HasMaxLength(1000);

            builder.Property(x => x.CommissionRate)
                .HasPrecision(6, 4);

            builder.Property(x => x.CreatedAtUtc).IsRequired();

            builder.HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
