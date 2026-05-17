using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ZansiHustle.Domain.Engagement;
using ZansiHustle.Domain.Identity;
using ZansiHustle.Domain.Listings;
using ZansiHustle.Domain.Marketplace;
using ZansiHustle.Domain.Merchants;
using ZansiHustle.Domain.Shops;

namespace ZansiHustle.Infrastructure.Persistence.Engagement
{
    // ─────────────────────────────────────────────────────────────────────────
    // EF configurations for the four engagement join tables. All share the
    // same shape: (Id, UserId, <TargetId>, CreatedAtUtc) plus a unique index
    // on (UserId, TargetId) so duplicate likes/follows/saves can never be
    // inserted even under concurrency. FK behaviour notes documented per
    // configuration — the engagement rows are tied to the parent's lifetime
    // (delete listing → delete its likes) and to the user's lifetime (delete
    // user → delete their engagements).
    // ─────────────────────────────────────────────────────────────────────────

    public sealed class ListingLikeConfiguration : IEntityTypeConfiguration<ListingLike>
    {
        public void Configure(EntityTypeBuilder<ListingLike> builder)
        {
            builder.ToTable("ListingLikes");
            builder.HasKey(x => x.Id);

            // Unique composite — one heart per (user, listing). The
            // service still does a duplicate-check first (so we can
            // return a clean idempotent success), but the index is the
            // hard guarantee against any race between two concurrent
            // POSTs from the same client.
            builder.HasIndex(x => new { x.UserId, x.ListingId }).IsUnique();

            // Hot read paths: "my liked listings" (filter by UserId) and
            // "who liked this?" (filter by ListingId — currently not
            // exposed, but the index is cheap and keeps the door open).
            builder.HasIndex(x => x.UserId);
            builder.HasIndex(x => x.ListingId);

            builder.HasOne(x => x.Listing)
                .WithMany()
                .HasForeignKey(x => x.ListingId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                // NoAction — deleting an Identity user is rare; we don't
                // want cascade-delete pulling rows from join tables for
                // every user soft-delete operation. If/when GDPR-style
                // hard-delete arrives, the cleanup will be explicit.
                .OnDelete(DeleteBehavior.NoAction);
        }
    }

    public sealed class MarketplaceListingLikeConfiguration : IEntityTypeConfiguration<MarketplaceListingLike>
    {
        public void Configure(EntityTypeBuilder<MarketplaceListingLike> builder)
        {
            builder.ToTable("MarketplaceListingLikes");
            builder.HasKey(x => x.Id);

            builder.HasIndex(x => new { x.UserId, x.MarketplaceListingId }).IsUnique();
            builder.HasIndex(x => x.UserId);
            builder.HasIndex(x => x.MarketplaceListingId);

            builder.HasOne(x => x.MarketplaceListing)
                .WithMany()
                .HasForeignKey(x => x.MarketplaceListingId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.NoAction);
        }
    }

    public sealed class ShopFollowConfiguration : IEntityTypeConfiguration<ShopFollow>
    {
        public void Configure(EntityTypeBuilder<ShopFollow> builder)
        {
            builder.ToTable("ShopFollows");
            builder.HasKey(x => x.Id);

            builder.HasIndex(x => new { x.UserId, x.ShopProfileId }).IsUnique();
            builder.HasIndex(x => x.UserId);
            builder.HasIndex(x => x.ShopProfileId);

            builder.HasOne(x => x.ShopProfile)
                .WithMany()
                .HasForeignKey(x => x.ShopProfileId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.NoAction);
        }
    }

    public sealed class StoreSaveConfiguration : IEntityTypeConfiguration<StoreSave>
    {
        public void Configure(EntityTypeBuilder<StoreSave> builder)
        {
            builder.ToTable("StoreSaves");
            builder.HasKey(x => x.Id);

            builder.HasIndex(x => new { x.UserId, x.MerchantId }).IsUnique();
            builder.HasIndex(x => x.UserId);
            builder.HasIndex(x => x.MerchantId);

            // FK to Merchant. Type==PhysicalStore guard lives in the
            // service layer (a CHECK on Type would require a trigger
            // and still couldn't constrain the soft enum). The unique
            // index above plus the service-side validation is enough.
            builder.HasOne(x => x.Merchant)
                .WithMany()
                .HasForeignKey(x => x.MerchantId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.NoAction);
        }
    }
}
