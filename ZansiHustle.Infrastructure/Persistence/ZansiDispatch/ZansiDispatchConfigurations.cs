using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ZansiHustle.Domain.ZansiDispatch;

namespace ZansiHustle.Infrastructure.Persistence.ZansiDispatch
{
    // ─────────────────────────────────────────────────────────────────────────
    // EF configurations for ZansiDispatch — the logistics control layer.
    //
    //  • Reference ids (OrderId / ListingId / ShopId / MerchantId / UserId) are
    //    plain indexed Guid columns with NO foreign keys: logistics records are
    //    operational/audit data that must outlive the rows they reference, and
    //    hard FKs into Orders / Listings / Merchants would create cascade-path
    //    conflicts. The ONE relationship modelled is Quote → Options (owned set,
    //    cascade) because options have no meaning without their quote.
    //  • Money uses precision (18,2); weight/distance (18,3).
    //
    // Auto-discovered by AppDbContext via ApplyConfigurationsFromAssembly.
    // ─────────────────────────────────────────────────────────────────────────

    public sealed class ZansiDispatchQuoteConfiguration : IEntityTypeConfiguration<ZansiDispatchQuote>
    {
        public void Configure(EntityTypeBuilder<ZansiDispatchQuote> builder)
        {
            builder.ToTable("ZansiDispatchQuotes");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.BuyerProvince).HasMaxLength(150);
            builder.Property(x => x.BuyerCity).HasMaxLength(150);
            builder.Property(x => x.BuyerAddressSummary).HasMaxLength(500);
            builder.Property(x => x.BuyerStreetAddress).HasMaxLength(300);
            builder.Property(x => x.BuyerLocalArea).HasMaxLength(150);
            builder.Property(x => x.BuyerPostalCode).HasMaxLength(20);
            builder.Property(x => x.BuyerCountry).HasMaxLength(4);
            builder.Property(x => x.BuyerLat).HasPrecision(18, 7);
            builder.Property(x => x.BuyerLng).HasPrecision(18, 7);
            builder.Property(x => x.SellerProvince).HasMaxLength(150);
            builder.Property(x => x.SellerCity).HasMaxLength(150);
            builder.Property(x => x.SellerAddressSummary).HasMaxLength(500);
            builder.Property(x => x.SellerStreetAddress).HasMaxLength(300);
            builder.Property(x => x.SellerLocalArea).HasMaxLength(150);
            builder.Property(x => x.SellerPostalCode).HasMaxLength(20);
            builder.Property(x => x.SellerCountry).HasMaxLength(4);
            builder.Property(x => x.SellerLat).HasPrecision(18, 7);
            builder.Property(x => x.SellerLng).HasPrecision(18, 7);
            builder.Property(x => x.ParcelDescription).HasMaxLength(300);
            builder.Property(x => x.EstimatedWeightKg).HasPrecision(18, 3);
            builder.Property(x => x.SubmittedLengthCm).HasPrecision(18, 2);
            builder.Property(x => x.SubmittedWidthCm).HasPrecision(18, 2);
            builder.Property(x => x.SubmittedHeightCm).HasPrecision(18, 2);
            builder.Property(x => x.DistanceKm).HasPrecision(18, 3);
            builder.Property(x => x.DeclaredValue).HasPrecision(18, 2);

            builder.HasIndex(x => x.UserId);
            builder.HasIndex(x => x.OrderId);
            builder.HasIndex(x => x.MerchantId);
            builder.HasIndex(x => x.Status);
            builder.HasIndex(x => x.CreatedAt);

            builder.HasMany(x => x.Options)
                .WithOne(o => o.Quote)
                .HasForeignKey(o => o.QuoteId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }

    public sealed class ZansiDispatchQuoteOptionConfiguration : IEntityTypeConfiguration<ZansiDispatchQuoteOption>
    {
        public void Configure(EntityTypeBuilder<ZansiDispatchQuoteOption> builder)
        {
            builder.ToTable("ZansiDispatchQuoteOptions");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.ProviderQuoteReference).HasMaxLength(200);
            builder.Property(x => x.ProviderServiceLevelId).HasMaxLength(120);
            builder.Property(x => x.ServiceLevelCode).HasMaxLength(60);
            builder.Property(x => x.ServiceLevelName).HasMaxLength(120);
            builder.Property(x => x.Label).HasMaxLength(120).IsRequired();
            builder.Property(x => x.Description).HasMaxLength(500);
            builder.Property(x => x.QuotedAmount).HasPrecision(18, 2);
            builder.Property(x => x.VatAmount).HasPrecision(18, 2);
            builder.Property(x => x.TotalAmount).HasPrecision(18, 2);
            builder.Property(x => x.Currency).HasMaxLength(8).IsRequired();

            builder.HasIndex(x => x.QuoteId);
            builder.HasIndex(x => new { x.QuoteId, x.IsSelected });
        }
    }

    public sealed class ZansiDispatchShipmentConfiguration : IEntityTypeConfiguration<ZansiDispatchShipment>
    {
        public void Configure(EntityTypeBuilder<ZansiDispatchShipment> builder)
        {
            builder.ToTable("ZansiDispatchShipments");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.QuotedDeliveryFee).HasPrecision(18, 2);
            builder.Property(x => x.ActualCourierCost).HasPrecision(18, 2);
            builder.Property(x => x.SurplusAmount).HasPrecision(18, 2);
            builder.Property(x => x.DeficitAmount).HasPrecision(18, 2);
            builder.Property(x => x.NetAmount).HasPrecision(18, 2);

            builder.Property(x => x.ServiceLevelCode).HasMaxLength(60);
            builder.Property(x => x.ServiceLevelName).HasMaxLength(120);
            builder.Property(x => x.ProviderShipmentId).HasMaxLength(120);
            builder.Property(x => x.TrackingNumber).HasMaxLength(120);
            builder.Property(x => x.ShortTrackingReference).HasMaxLength(120);
            builder.Property(x => x.ProviderShipmentReference).HasMaxLength(200);
            builder.Property(x => x.CourierReference).HasMaxLength(200);
            builder.Property(x => x.PickupAddressSummary).HasMaxLength(500);
            builder.Property(x => x.DropoffAddressSummary).HasMaxLength(500);
            builder.Property(x => x.LabelUrl).HasMaxLength(1000);
            builder.Property(x => x.Notes).HasMaxLength(2000);
            builder.Property(x => x.FailureReason).HasMaxLength(1000);

            // One shipment per order in v1 (single-merchant orders).
            builder.HasIndex(x => x.OrderId).IsUnique();
            builder.HasIndex(x => x.UserId);
            builder.HasIndex(x => x.MerchantId);
            builder.HasIndex(x => x.Status);
            builder.HasIndex(x => x.ReconciliationStatus);
            builder.HasIndex(x => x.CreatedAt);
            builder.HasIndex(x => x.TrackingNumber);
            builder.HasIndex(x => x.ShortTrackingReference);
            builder.HasIndex(x => x.ProviderShipmentId);
        }
    }

    public sealed class ZansiDispatchShipmentEventConfiguration : IEntityTypeConfiguration<ZansiDispatchShipmentEvent>
    {
        public void Configure(EntityTypeBuilder<ZansiDispatchShipmentEvent> builder)
        {
            builder.ToTable("ZansiDispatchShipmentEvents");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.ProviderEventId).HasMaxLength(120);
            builder.Property(x => x.ProviderStatus).HasMaxLength(80).IsRequired();
            builder.Property(x => x.Message).HasMaxLength(500);
            builder.Property(x => x.Location).HasMaxLength(200);

            builder.HasIndex(x => x.ShipmentId);
            builder.HasIndex(x => new { x.ShipmentId, x.EventTime });
        }
    }

    public sealed class ZansiDispatchShipmentActionConfiguration : IEntityTypeConfiguration<ZansiDispatchShipmentAction>
    {
        public void Configure(EntityTypeBuilder<ZansiDispatchShipmentAction> builder)
        {
            builder.ToTable("ZansiDispatchShipmentActions");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.ProviderStatusBefore).HasMaxLength(80);
            builder.Property(x => x.ProviderStatusAfter).HasMaxLength(80);
            builder.Property(x => x.Reason).HasMaxLength(1000);
            builder.Property(x => x.Notes).HasMaxLength(1000);
            builder.Property(x => x.CorrelationId).HasMaxLength(100);

            builder.HasIndex(x => x.ShipmentId);
            builder.HasIndex(x => new { x.ShipmentId, x.CreatedAtUtc });
            builder.HasIndex(x => x.OrderId);
        }
    }

    public sealed class ZansiDispatchLedgerEntryConfiguration : IEntityTypeConfiguration<ZansiDispatchLedgerEntry>
    {
        public void Configure(EntityTypeBuilder<ZansiDispatchLedgerEntry> builder)
        {
            builder.ToTable("ZansiDispatchLedgerEntries");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Amount).HasPrecision(18, 2);
            builder.Property(x => x.BalanceImpact).HasPrecision(18, 2);
            builder.Property(x => x.Description).HasMaxLength(500);
            builder.Property(x => x.Reference).HasMaxLength(200);

            builder.HasIndex(x => x.ShipmentId);
            builder.HasIndex(x => x.OrderId);
            builder.HasIndex(x => x.EntryType);
            builder.HasIndex(x => x.CreatedAt);
        }
    }

    public sealed class ZansiDispatchProviderRequestLogConfiguration : IEntityTypeConfiguration<ZansiDispatchProviderRequestLog>
    {
        public void Configure(EntityTypeBuilder<ZansiDispatchProviderRequestLog> builder)
        {
            builder.ToTable("ZansiDispatchProviderRequestLogs");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.ErrorMessage).HasMaxLength(1000);

            builder.HasIndex(x => x.ProviderType);
            builder.HasIndex(x => x.Operation);
            builder.HasIndex(x => x.CreatedAt);
        }
    }

    public sealed class ZansiDispatchSettingConfiguration : IEntityTypeConfiguration<ZansiDispatchSetting>
    {
        public void Configure(EntityTypeBuilder<ZansiDispatchSetting> builder)
        {
            builder.ToTable("ZansiDispatchSettings");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Key).HasMaxLength(200).IsRequired();
            builder.Property(x => x.Description).HasMaxLength(500);

            builder.HasIndex(x => x.Key).IsUnique();
        }
    }
}
