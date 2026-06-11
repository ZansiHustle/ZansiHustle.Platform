using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using ZansiHustle.Domain.Listings;
using ZansiHustle.Shared.Enums.Listings;

namespace ZansiHustle.Infrastructure.Data.Configurations.Listings
{
    /// <summary>
    /// EF Core mapping for <see cref="Listing"/>.
    /// String arrays (Images, DeliveryOptions, Availability, BookingMethods) are
    /// persisted as JSON in nvarchar(max) columns.
    /// </summary>
    public class ListingConfiguration : IEntityTypeConfiguration<Listing>
    {
        private static readonly ValueConverter<List<string>, string> NonNullListConverter = new(
            v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
            v => string.IsNullOrEmpty(v)
                ? new List<string>()
                : JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions?)null) ?? new List<string>());

        private static readonly ValueConverter<List<string>?, string?> NullableListConverter = new(
            v => v == null ? null : JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
            v => string.IsNullOrEmpty(v)
                ? null
                : JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions?)null));

        private static readonly ValueComparer<List<string>> NonNullListComparer = new(
            (a, b) => (a == null && b == null) || (a != null && b != null && a.SequenceEqual(b)),
            v => v == null ? 0 : v.Aggregate(0, (h, s) => System.HashCode.Combine(h, s)),
            v => v == null ? new List<string>() : v.ToList());

        private static readonly ValueComparer<List<string>?> NullableListComparer = new(
            (a, b) => (a == null && b == null) || (a != null && b != null && a.SequenceEqual(b)),
            v => v == null ? 0 : v.Aggregate(0, (h, s) => System.HashCode.Combine(h, s)),
            v => v == null ? null : v.ToList());

        public void Configure(EntityTypeBuilder<Listing> builder)
        {
            builder.ToTable("Listings");

            builder.HasKey(x => x.Id);

            builder.HasIndex(x => x.Code).IsUnique();
            builder.HasIndex(x => x.Slug).IsUnique();
            builder.HasIndex(x => x.Type);
            builder.HasIndex(x => x.Status);
            // SearchAsync filters Status==Active AND AvailabilityMode!=InStoreOnly
            // on every public listing query — index supports the hot path.
            builder.HasIndex(x => x.AvailabilityMode);
            builder.HasIndex(x => x.MerchantId);
            // Indexes for the new source / shop association fields.
            // ShopProfile.ProductsTab filters by ShopProfileId; the
            // index makes that the hot path (vs scanning all listings
            // for that merchant).
            builder.HasIndex(x => x.ListingSource);
            builder.HasIndex(x => x.ShopProfileId);
            builder.HasIndex(x => x.SellerCategoryId);
            builder.HasIndex(x => x.SellerSubcategoryId);
            builder.HasIndex(x => x.Price);
            builder.HasIndex(x => x.City);
            builder.HasIndex(x => x.Province);
            builder.HasIndex(x => x.CreatedAtUtc);
            builder.HasIndex(x => x.IsFeatured);
            builder.HasIndex(x => x.IsBoosted);

            builder.Property(x => x.Code)
                .IsRequired()
                .HasMaxLength(60);

            builder.Property(x => x.Slug)
                .IsRequired()
                .HasMaxLength(220);

            builder.Property(x => x.Title)
                .IsRequired()
                .HasMaxLength(250);

            builder.Property(x => x.Description)
                .HasMaxLength(5000);

            builder.Property(x => x.Price)
                .HasPrecision(18, 2)
                .IsRequired();

            builder.Property(x => x.Currency)
                .IsRequired()
                .HasMaxLength(8);

            builder.Property(x => x.Type)
                .HasConversion<int>()
                .IsRequired();

            builder.Property(x => x.Status)
                .HasConversion<int>()
                .IsRequired();

            // NOT NULL with DB-level default of 1 (OnlineOnly) so the
            // migration backfills existing rows without needing a
            // separate UPDATE pass — every pre-existing listing was
            // created by an OnlineStore merchant and is correctly
            // tagged OnlineOnly.
            builder.Property(x => x.AvailabilityMode)
                .HasConversion<int>()
                .IsRequired()
                .HasDefaultValue(AvailabilityMode.OnlineOnly);

            // NOT NULL with DB-level default 1 (SellerAccount) so the
            // migration backfills every existing listing without
            // requiring a separate UPDATE — pre-existing rows were
            // created before ShopProfile existed and are correctly
            // categorised as seller-account listings.
            builder.Property(x => x.ListingSource)
                .HasConversion<int>()
                .IsRequired()
                .HasDefaultValue(ListingSource.SellerAccount);

            builder.Property(x => x.Condition)
                .HasConversion<int?>();

            builder.Property(x => x.PricingModel)
                .HasConversion<int?>();

            builder.Property(x => x.Province)
                .HasMaxLength(150);

            builder.Property(x => x.City)
                .HasMaxLength(150);

            builder.Property(x => x.ServiceArea)
                .HasMaxLength(250);

            builder.Property(x => x.Turnaround)
                .HasMaxLength(250);

            // ── Service fulfilment (all nullable, additive) ──────────────────
            builder.Property(x => x.FulfilmentMode)
                .HasConversion<int?>();
            builder.Property(x => x.TravelFeeType)
                .HasConversion<int?>();

            builder.Property(x => x.ProviderLocationName).HasMaxLength(150);
            builder.Property(x => x.ProviderAddressLine1).HasMaxLength(250);
            builder.Property(x => x.ProviderAddressLine2).HasMaxLength(250);
            builder.Property(x => x.ProviderCity).HasMaxLength(150);
            builder.Property(x => x.ProviderProvince).HasMaxLength(150);
            builder.Property(x => x.ProviderPostalCode).HasMaxLength(20);

            // Lat/lng — generous precision for geo coordinates.
            builder.Property(x => x.ProviderLatitude).HasPrecision(9, 6);
            builder.Property(x => x.ProviderLongitude).HasPrecision(9, 6);

            // Money + distance amounts.
            builder.Property(x => x.TravelFeePerKm).HasPrecision(18, 2);
            builder.Property(x => x.TravelFeeFlatAmount).HasPrecision(18, 2);
            builder.Property(x => x.TravelFeeMinimum).HasPrecision(18, 2);
            builder.Property(x => x.TravelFeeMaximum).HasPrecision(18, 2);
            builder.Property(x => x.HouseCallSurchargeAmount).HasPrecision(18, 2);
            builder.Property(x => x.FreeTravelRadiusKm).HasPrecision(9, 2);
            builder.Property(x => x.MaxTravelDistanceKm).HasPrecision(9, 2);

            builder.Property(x => x.Rating)
                .HasPrecision(5, 2);

            var imagesProperty = builder.Property(x => x.Images)
                .HasColumnType("nvarchar(max)")
                .IsRequired()
                .HasConversion(NonNullListConverter);
            imagesProperty.Metadata.SetValueComparer(NonNullListComparer);

            var deliveryProperty = builder.Property(x => x.DeliveryOptions)
                .HasColumnType("nvarchar(max)")
                .HasConversion(NullableListConverter);
            deliveryProperty.Metadata.SetValueComparer(NullableListComparer);

            var availabilityProperty = builder.Property(x => x.Availability)
                .HasColumnType("nvarchar(max)")
                .HasConversion(NullableListConverter);
            availabilityProperty.Metadata.SetValueComparer(NullableListComparer);

            var bookingMethodsProperty = builder.Property(x => x.BookingMethods)
                .HasColumnType("nvarchar(max)")
                .HasConversion(NullableListConverter);
            bookingMethodsProperty.Metadata.SetValueComparer(NullableListComparer);

            builder.Property(x => x.CreatedAtUtc)
                .IsRequired();

            builder.HasOne(x => x.Merchant)
                .WithMany()
                .HasForeignKey(x => x.MerchantId)
                .OnDelete(DeleteBehavior.Cascade);

            // Optional FK to ShopProfile — populated only when
            // ListingSource == ShopProfile. Restrict on delete so a
            // shop can't be deleted while listings still reference
            // it; product flow has its own "Suspend" lifecycle for
            // taking a shop offline without orphaning listings.
            builder.HasOne(x => x.ShopProfile)
                .WithMany()
                .HasForeignKey(x => x.ShopProfileId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.SellerCategory)
                .WithMany()
                .HasForeignKey(x => x.SellerCategoryId)
                .OnDelete(DeleteBehavior.SetNull);

            // NoAction mirrors the Merchants config — avoids SQL Server's
            // multiple-cascade-paths error on SellerCategory → SellerSubcategory.
            builder.HasOne(x => x.SellerSubcategory)
                .WithMany()
                .HasForeignKey(x => x.SellerSubcategoryId)
                .OnDelete(DeleteBehavior.NoAction);
        }
    }
}
