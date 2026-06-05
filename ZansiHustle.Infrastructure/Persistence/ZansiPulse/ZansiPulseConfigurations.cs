using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ZansiHustle.Domain.ZansiPulse;

namespace ZansiHustle.Infrastructure.Persistence.ZansiPulse
{
    // ─────────────────────────────────────────────────────────────────────────
    // EF configurations for the ZansiPulse intelligence layer.
    //
    // Design notes that apply across the board:
    //  • Reference ids (UserId / ListingId / SellerId / ShopId / CategoryId …)
    //    are plain indexed Guid columns with NO foreign keys. These are
    //    analytics tables: they must outlive the rows they point at, and hard
    //    FKs into Listings / Users / Merchants would create SQL Server
    //    multiple-cascade-path conflicts and block operational deletes. Joins
    //    happen at query time in ZansiPulseService.
    //  • Decimal scores use precision (18,4); money uses (18,2).
    //  • Period/aggregate tables carry a UNIQUE composite over their bucket
    //    key so the service can find-or-create idempotently. SQL Server treats
    //    NULL as a single value in a unique index, which gives us exactly one
    //    "no subcategory" / "no city" bucket — the behaviour we want.
    //
    // Auto-discovered by AppDbContext via ApplyConfigurationsFromAssembly.
    // ─────────────────────────────────────────────────────────────────────────

    public sealed class ZansiPulseEventConfiguration : IEntityTypeConfiguration<ZansiPulseEvent>
    {
        public void Configure(EntityTypeBuilder<ZansiPulseEvent> builder)
        {
            builder.ToTable("ZansiPulseEvents");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.SearchTerm).HasMaxLength(256);
            builder.Property(x => x.Province).HasMaxLength(150);
            builder.Property(x => x.City).HasMaxLength(150);
            builder.Property(x => x.Price).HasPrecision(18, 2);
            builder.Property(x => x.Weight).HasPrecision(18, 4);

            // Hot read/aggregate paths.
            builder.HasIndex(x => x.UserId);
            builder.HasIndex(x => x.ListingId);
            builder.HasIndex(x => x.SellerId);
            builder.HasIndex(x => x.ShopId);
            builder.HasIndex(x => x.CategoryId);
            builder.HasIndex(x => x.EventType);
            builder.HasIndex(x => x.CreatedAt);
            builder.HasIndex(x => new { x.Province, x.City });
            // Common dashboard slice: "events of type T since date D".
            builder.HasIndex(x => new { x.EventType, x.CreatedAt });
        }
    }

    public sealed class ZansiPulseUserInterestScoreConfiguration : IEntityTypeConfiguration<ZansiPulseUserInterestScore>
    {
        public void Configure(EntityTypeBuilder<ZansiPulseUserInterestScore> builder)
        {
            builder.ToTable("ZansiPulseUserInterestScores");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Score).HasPrecision(18, 4);

            builder.HasIndex(x => new { x.UserId, x.CategoryId, x.SubCategoryId }).IsUnique();
            builder.HasIndex(x => x.UserId);
        }
    }

    public sealed class ZansiPulseListingMetricConfiguration : IEntityTypeConfiguration<ZansiPulseListingMetric>
    {
        public void Configure(EntityTypeBuilder<ZansiPulseListingMetric> builder)
        {
            builder.ToTable("ZansiPulseListingMetrics");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.TrendingScore).HasPrecision(18, 4);

            builder.HasIndex(x => x.ListingId).IsUnique();
            builder.HasIndex(x => x.TrendingScore);
        }
    }

    public sealed class ZansiPulseSellerMetricConfiguration : IEntityTypeConfiguration<ZansiPulseSellerMetric>
    {
        public void Configure(EntityTypeBuilder<ZansiPulseSellerMetric> builder)
        {
            builder.ToTable("ZansiPulseSellerMetrics");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.ResponsivenessScore).HasPrecision(18, 4);
            builder.Property(x => x.TrustScore).HasPrecision(18, 4);
            builder.Property(x => x.PopularityScore).HasPrecision(18, 4);
            builder.Property(x => x.QualityScore).HasPrecision(18, 4);

            builder.HasIndex(x => x.SellerId).IsUnique();
            builder.HasIndex(x => x.PopularityScore);
        }
    }

    public sealed class ZansiPulseShopMetricConfiguration : IEntityTypeConfiguration<ZansiPulseShopMetric>
    {
        public void Configure(EntityTypeBuilder<ZansiPulseShopMetric> builder)
        {
            builder.ToTable("ZansiPulseShopMetrics");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.PopularityScore).HasPrecision(18, 4);
            builder.Property(x => x.QualityScore).HasPrecision(18, 4);

            builder.HasIndex(x => x.ShopId).IsUnique();
            builder.HasIndex(x => x.PopularityScore);
        }
    }

    public sealed class ZansiPulseCategoryMetricConfiguration : IEntityTypeConfiguration<ZansiPulseCategoryMetric>
    {
        public void Configure(EntityTypeBuilder<ZansiPulseCategoryMetric> builder)
        {
            builder.ToTable("ZansiPulseCategoryMetrics");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.TrendingScore).HasPrecision(18, 4);

            builder.HasIndex(x => new { x.CategoryId, x.SubCategoryId, x.PeriodType, x.PeriodStart }).IsUnique();
            builder.HasIndex(x => new { x.PeriodType, x.PeriodStart });
        }
    }

    public sealed class ZansiPulseRegionMetricConfiguration : IEntityTypeConfiguration<ZansiPulseRegionMetric>
    {
        public void Configure(EntityTypeBuilder<ZansiPulseRegionMetric> builder)
        {
            builder.ToTable("ZansiPulseRegionMetrics");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Province).HasMaxLength(150).IsRequired();
            builder.Property(x => x.City).HasMaxLength(150);
            builder.Property(x => x.TrendingScore).HasPrecision(18, 4);

            builder.HasIndex(x => new { x.Province, x.City, x.PeriodType, x.PeriodStart }).IsUnique();
            builder.HasIndex(x => new { x.PeriodType, x.PeriodStart });
        }
    }

    public sealed class ZansiPulseSearchTermMetricConfiguration : IEntityTypeConfiguration<ZansiPulseSearchTermMetric>
    {
        public void Configure(EntityTypeBuilder<ZansiPulseSearchTermMetric> builder)
        {
            builder.ToTable("ZansiPulseSearchTermMetrics");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.SearchTerm).HasMaxLength(256).IsRequired();
            builder.Property(x => x.Province).HasMaxLength(150);
            builder.Property(x => x.City).HasMaxLength(150);

            builder.HasIndex(x => new { x.SearchTerm, x.Province, x.City, x.CategoryId, x.PeriodType, x.PeriodStart }).IsUnique();
            builder.HasIndex(x => new { x.PeriodType, x.PeriodStart });
            builder.HasIndex(x => x.SearchTerm);
        }
    }

    public sealed class ZansiPulseSupplyDemandGapConfiguration : IEntityTypeConfiguration<ZansiPulseSupplyDemandGap>
    {
        public void Configure(EntityTypeBuilder<ZansiPulseSupplyDemandGap> builder)
        {
            builder.ToTable("ZansiPulseSupplyDemandGaps");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Province).HasMaxLength(150);
            builder.Property(x => x.City).HasMaxLength(150);
            builder.Property(x => x.SearchTerm).HasMaxLength(256);
            builder.Property(x => x.RecommendedAction).HasMaxLength(500);
            builder.Property(x => x.DemandScore).HasPrecision(18, 4);
            builder.Property(x => x.GapScore).HasPrecision(18, 4);

            builder.HasIndex(x => x.GapScore);
            builder.HasIndex(x => new { x.PeriodStart, x.PeriodEnd });
            builder.HasIndex(x => new { x.CategoryId, x.Province, x.City });
        }
    }

    public sealed class ZansiPulseRecommendationLogConfiguration : IEntityTypeConfiguration<ZansiPulseRecommendationLog>
    {
        public void Configure(EntityTypeBuilder<ZansiPulseRecommendationLog> builder)
        {
            builder.ToTable("ZansiPulseRecommendationLogs");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.RecommendationType).HasMaxLength(100).IsRequired();
            builder.Property(x => x.Source).HasMaxLength(100).IsRequired();

            builder.HasIndex(x => x.UserId);
            builder.HasIndex(x => x.CreatedAt);
            builder.HasIndex(x => x.RecommendationType);
        }
    }

    public sealed class ZansiPulseSnapshotConfiguration : IEntityTypeConfiguration<ZansiPulseSnapshot>
    {
        public void Configure(EntityTypeBuilder<ZansiPulseSnapshot> builder)
        {
            builder.ToTable("ZansiPulseSnapshots");
            builder.HasKey(x => x.Id);

            builder.HasIndex(x => new { x.SnapshotType, x.PeriodStart, x.PeriodEnd }).IsUnique();
            builder.HasIndex(x => x.CreatedAt);
        }
    }

    public sealed class ZansiPulseSettingConfiguration : IEntityTypeConfiguration<ZansiPulseSetting>
    {
        public void Configure(EntityTypeBuilder<ZansiPulseSetting> builder)
        {
            builder.ToTable("ZansiPulseSettings");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Key).HasMaxLength(200).IsRequired();
            builder.Property(x => x.Description).HasMaxLength(500);

            builder.HasIndex(x => x.Key).IsUnique();
        }
    }
}
