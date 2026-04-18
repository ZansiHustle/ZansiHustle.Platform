using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ZansiHustle.Domain.Fundraising;

namespace ZansiHustle.Infrastructure.Data.Configurations.Fundraising
{
    public class StakeholderConfiguration : IEntityTypeConfiguration<Stakeholder>
    {
        public void Configure(EntityTypeBuilder<Stakeholder> builder)
        {
            builder.ToTable("FundraisingStakeholders");

            builder.HasKey(x => x.Id);

            builder.HasIndex(x => x.Type);
            builder.HasIndex(x => x.IsActive);
            builder.HasIndex(x => x.CreatedAtUtc);

            builder.Property(x => x.FullName)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(x => x.Type)
                .HasConversion<int>()
                .IsRequired();

            builder.Property(x => x.Email).HasMaxLength(256);
            builder.Property(x => x.Phone).HasMaxLength(30);

            builder.Property(x => x.PercentageOwned).HasPrecision(9, 4).IsRequired();
            builder.Property(x => x.AmountInvested).HasPrecision(18, 2);
            builder.Property(x => x.PricingBasisValuation).HasPrecision(18, 2).IsRequired();

            builder.Property(x => x.AgreementReference).HasMaxLength(200);
            builder.Property(x => x.Notes).HasMaxLength(2000);

            builder.Property(x => x.EntryDateUtc).IsRequired();
            builder.Property(x => x.CreatedAtUtc).IsRequired();
        }
    }
}
