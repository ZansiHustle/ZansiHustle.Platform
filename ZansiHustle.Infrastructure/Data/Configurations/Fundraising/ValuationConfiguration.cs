using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ZansiHustle.Domain.Fundraising;

namespace ZansiHustle.Infrastructure.Data.Configurations.Fundraising
{
    public class ValuationConfiguration : IEntityTypeConfiguration<Valuation>
    {
        public void Configure(EntityTypeBuilder<Valuation> builder)
        {
            builder.ToTable("FundraisingValuations");

            builder.HasKey(x => x.Id);

            // At most one active valuation at a time.
            builder.HasIndex(x => x.IsActive)
                .IsUnique()
                .HasFilter("[IsActive] = 1");

            builder.HasIndex(x => x.CreatedAtUtc);

            builder.Property(x => x.Label)
                .IsRequired()
                .HasMaxLength(150);

            builder.Property(x => x.InternalBaseline).HasPrecision(18, 2).IsRequired();
            builder.Property(x => x.FundraisingValuation).HasPrecision(18, 2).IsRequired();
            builder.Property(x => x.ScenarioConservative).HasPrecision(18, 2).IsRequired();
            builder.Property(x => x.ScenarioModerate).HasPrecision(18, 2).IsRequired();
            builder.Property(x => x.ScenarioAggressive).HasPrecision(18, 2).IsRequired();

            builder.Property(x => x.ScenarioHorizonLabel).HasMaxLength(80);

            builder.Property(x => x.Currency)
                .IsRequired()
                .HasMaxLength(8);

            builder.Property(x => x.Notes).HasMaxLength(2000);

            builder.Property(x => x.EffectiveFromUtc).IsRequired();
            builder.Property(x => x.CreatedAtUtc).IsRequired();
        }
    }
}
