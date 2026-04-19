using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ZansiHustle.Domain.Referrals;

namespace ZansiHustle.Infrastructure.Data.Configurations.Referrals
{
    public class UserReferralConfiguration : IEntityTypeConfiguration<UserReferral>
    {
        public void Configure(EntityTypeBuilder<UserReferral> builder)
        {
            builder.ToTable("UserReferrals");

            builder.HasKey(x => x.Id);

            // A given user can be referred at most once per referral type
            // (e.g. one Merchant referral, but they could later show up as a
            // Buyer referral too if we ever support multi-context joins).
            builder.HasIndex(x => new { x.ReferredUserId, x.ReferralType }).IsUnique();
            builder.HasIndex(x => x.AffiliateProfileId);
            builder.HasIndex(x => x.ReferrerUserId);
            builder.HasIndex(x => x.ReferralCodeUsed);
            builder.HasIndex(x => x.Status);

            builder.Property(x => x.ReferralCodeUsed)
                .IsRequired()
                .HasMaxLength(60);

            builder.Property(x => x.ReferralType)
                .HasConversion<int>()
                .IsRequired();

            builder.Property(x => x.Status)
                .HasConversion<int>()
                .IsRequired();

            builder.Property(x => x.SourcePath)
                .HasMaxLength(500);

            builder.Property(x => x.JoinedAtUtc).IsRequired();
            builder.Property(x => x.CreatedAtUtc).IsRequired();

            // NoAction: an affiliate profile delete shouldn't cascade-delete
            // historical referral rows — they're audit data.
            builder.HasOne(x => x.AffiliateProfile)
                .WithMany()
                .HasForeignKey(x => x.AffiliateProfileId)
                .OnDelete(DeleteBehavior.NoAction);
        }
    }
}
