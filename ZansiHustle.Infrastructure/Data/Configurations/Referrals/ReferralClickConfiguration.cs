using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ZansiHustle.Domain.Referrals;

namespace ZansiHustle.Infrastructure.Data.Configurations.Referrals
{
    public class ReferralClickConfiguration : IEntityTypeConfiguration<ReferralClick>
    {
        public void Configure(EntityTypeBuilder<ReferralClick> builder)
        {
            builder.ToTable("ReferralClicks");

            builder.HasKey(x => x.Id);

            builder.HasIndex(x => x.AffiliateProfileId);
            builder.HasIndex(x => x.ReferralCode);
            builder.HasIndex(x => x.ClickedAtUtc);

            builder.Property(x => x.ReferralCode)
                .IsRequired()
                .HasMaxLength(60);

            builder.Property(x => x.LandingPath)
                .HasMaxLength(500);

            builder.Property(x => x.UserAgent)
                .HasMaxLength(500);

            // Hashed (SHA-256 hex) — fixed 64-char width.
            builder.Property(x => x.IpHash)
                .HasMaxLength(64);

            builder.Property(x => x.ClickedAtUtc).IsRequired();

            builder.HasOne(x => x.AffiliateProfile)
                .WithMany()
                .HasForeignKey(x => x.AffiliateProfileId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
