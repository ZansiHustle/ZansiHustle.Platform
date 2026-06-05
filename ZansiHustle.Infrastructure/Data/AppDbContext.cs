using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System.Reflection;
using ZansiHustle.Domain.Agents.AgentApplications;
using ZansiHustle.Domain.Agents.AgentPayouts;
using ZansiHustle.Domain.Chat;
using ZansiHustle.Domain.BudgetTransactions;
using ZansiHustle.Domain.Campaigns;
using ZansiHustle.Domain.ContentTasks;
using ZansiHustle.Domain.Engagement;
using ZansiHustle.Domain.Events;
using ZansiHustle.Domain.Fundraising;
using ZansiHustle.Domain.Identity;
using ZansiHustle.Domain.Influencers;
using ZansiHustle.Domain.Listings;
using ZansiHustle.Domain.Marketplace;
using ZansiHustle.Domain.Media;
using ZansiHustle.Domain.Merchants;
using ZansiHustle.Domain.Orders;
using ZansiHustle.Domain.Payments;
using ZansiHustle.Domain.Podcasts;
using ZansiHustle.Domain.Referrals;
using ZansiHustle.Domain.Reviews;
using ZansiHustle.Domain.SellerCategories;
using ZansiHustle.Domain.Shops;
using ZansiHustle.Domain.SellerLeads;
using ZansiHustle.Domain.ZansiPulse;

namespace ZansiHustle.Infrastructure.Data;

/// <summary>
/// Primary EF Core database context for ZansiHustle.
/// </summary>
public class AppDbContext : IdentityDbContext<User, IdentityRole<Guid>, Guid>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<User>().ToTable("Users");
        builder.Entity<IdentityRole<Guid>>().ToTable("Roles");
        builder.Entity<IdentityUserRole<Guid>>().ToTable("UserRoles");
        builder.Entity<IdentityUserClaim<Guid>>().ToTable("UserClaims");
        builder.Entity<IdentityUserLogin<Guid>>().ToTable("UserLogins");
        builder.Entity<IdentityRoleClaim<Guid>>().ToTable("RoleClaims");
        builder.Entity<IdentityUserToken<Guid>>().ToTable("UserTokens");

        builder.Entity<UserProfile>(entity =>
        {
            entity.ToTable("UserProfiles");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.UserId).IsUnique();

            entity.Property(x => x.ProfileImageUrl).HasMaxLength(500);
            entity.Property(x => x.Bio).HasMaxLength(1000);
            entity.Property(x => x.City).HasMaxLength(150);
            entity.Property(x => x.Province).HasMaxLength(150);

            entity.HasOne<User>()
                .WithOne()
                .HasForeignKey<UserProfile>(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<UserSettings>(entity =>
        {
            entity.ToTable("UserSettings");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.UserId).IsUnique();

            entity.HasOne<User>()
                .WithOne()
                .HasForeignKey<UserSettings>(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<RefreshToken>(entity =>
        {
            entity.ToTable("RefreshTokens");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.Token).IsUnique();

            entity.Property(x => x.Token).HasMaxLength(500);

            entity.HasOne<User>()
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
    }

    public DbSet<UserProfile> UserProfiles => Set<UserProfile>();
    public DbSet<UserSettings> UserSettings => Set<UserSettings>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<AgentApplication> AgentApplications { get; set; }
    public DbSet<SellerLead> SellerLeads { get; set; }
    public DbSet<Influencer> Influencers { get; set; }
    public DbSet<InfluencerPlatformAccount> InfluencerPlatformAccounts { get; set; }
    public DbSet<Podcast> Podcasts { get; set; }
    public DbSet<PodcastAdFormat> PodcastAdFormats { get; set; }
    public DbSet<Campaign> Campaigns { get; set; }
    public DbSet<CampaignMetricSnapshot> CampaignMetricSnapshots { get; set; }
    public DbSet<ContentTask> ContentTasks { get; set; }
    public DbSet<BudgetTransaction> BudgetTransactions { get; set; }

    public DbSet<Merchant> Merchants => Set<Merchant>();

    public DbSet<SellerCategory> SellerCategories => Set<SellerCategory>();
    public DbSet<SellerSubcategory> SellerSubcategories => Set<SellerSubcategory>();

    public DbSet<Listing> Listings => Set<Listing>();
    public DbSet<ListingVariant> ListingVariants => Set<ListingVariant>();

    // ── Casual peer-to-peer Marketplace (separate from merchant Listings) ──
    public DbSet<MarketplaceListing> MarketplaceListings => Set<MarketplaceListing>();
    public DbSet<MarketplaceListingImage> MarketplaceListingImages => Set<MarketplaceListingImage>();

    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();

    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<PaymentEvent> PaymentEvents => Set<PaymentEvent>();

    public DbSet<EventPlan> EventPlans => Set<EventPlan>();
    public DbSet<EventPlanItem> EventPlanItems => Set<EventPlanItem>();
    public DbSet<EventTypeTemplate> EventTypeTemplates => Set<EventTypeTemplate>();

    public DbSet<Valuation> FundraisingValuations => Set<Valuation>();
    public DbSet<Stakeholder> FundraisingStakeholders => Set<Stakeholder>();

    // ── Standalone referral / affiliate system ────────────────────────────
    public DbSet<AffiliateProfile> AffiliateProfiles => Set<AffiliateProfile>();
    public DbSet<UserReferral> UserReferrals => Set<UserReferral>();
    public DbSet<ReferralClick> ReferralClicks => Set<ReferralClick>();

    // ── Shared media / blob-metadata system ───────────────────────────────
    public DbSet<MediaAsset> MediaAssets => Set<MediaAsset>();

    // ── Polymorphic reviews (Store / Shop / Product / Service) ────────────
    public DbSet<Review> Reviews => Set<Review>();

    // ── Shop storefronts (decoupled from Merchant; see ShopProfile) ──
    public DbSet<ShopProfile> ShopProfiles => Set<ShopProfile>();

    // ── Chat / Messaging ──────────────────────────────────────────────────
    // Buyer↔seller and buyer↔merchant text conversations. See
    // ZansiHustle.Application.Chat.ChatService for the read/write flows
    // and ZansiHustle.Infrastructure.Persistence.Chat.ChatConfigurations
    // for table / index / FK shapes.
    public DbSet<Conversation> Conversations => Set<Conversation>();
    public DbSet<ConversationParticipant> ConversationParticipants => Set<ConversationParticipant>();
    public DbSet<Message> ChatMessages => Set<Message>();

    // ── Agent payout ledger ───────────────────────────────────────────────
    // Auditable history of money paid to agents (lead-affiliate commissions).
    // Source of truth for `Outstanding = TotalEarned − TotalPaidOut`. See
    // ZansiHustle.Application.Agents.AgentPayouts.AgentPayoutService for the
    // transactional record path; configuration lives in
    // ZansiHustle.Infrastructure.Persistence.Agents.AgentPayoutConfiguration.
    public DbSet<AgentPayout> AgentPayouts => Set<AgentPayout>();

    // ── Buyer engagement (likes / follows / saves) ─────────────────────────
    // Four join tables, one per target domain. Counts are denormalised onto
    // the parent entities (Listing.LikeCount, MarketplaceListing.LikeCount,
    // ShopProfile.FollowersCount, Merchant.SavesCount) and updated
    // transactionally with each row insert/delete. See
    // ZansiHustle.Application.Engagement.EngagementService for the
    // mutation flow.
    public DbSet<ListingLike> ListingLikes => Set<ListingLike>();
    public DbSet<MarketplaceListingLike> MarketplaceListingLikes => Set<MarketplaceListingLike>();
    public DbSet<ShopFollow> ShopFollows => Set<ShopFollow>();
    public DbSet<StoreSave> StoreSaves => Set<StoreSave>();

    // ── ZansiPulse — the ZansiHustle intelligence layer ────────────────────
    // Event stream + derived metrics, scores, recommendations, trending,
    // supply/demand and dashboard snapshots. Reference ids are loose
    // (un-FK'd) analytics columns; joins happen in ZansiPulseService. EF
    // configurations live in
    // ZansiHustle.Infrastructure.Persistence.ZansiPulse.ZansiPulseConfigurations
    // and are auto-applied via ApplyConfigurationsFromAssembly above.
    public DbSet<ZansiPulseEvent> ZansiPulseEvents => Set<ZansiPulseEvent>();
    public DbSet<ZansiPulseUserInterestScore> ZansiPulseUserInterestScores => Set<ZansiPulseUserInterestScore>();
    public DbSet<ZansiPulseListingMetric> ZansiPulseListingMetrics => Set<ZansiPulseListingMetric>();
    public DbSet<ZansiPulseSellerMetric> ZansiPulseSellerMetrics => Set<ZansiPulseSellerMetric>();
    public DbSet<ZansiPulseShopMetric> ZansiPulseShopMetrics => Set<ZansiPulseShopMetric>();
    public DbSet<ZansiPulseCategoryMetric> ZansiPulseCategoryMetrics => Set<ZansiPulseCategoryMetric>();
    public DbSet<ZansiPulseRegionMetric> ZansiPulseRegionMetrics => Set<ZansiPulseRegionMetric>();
    public DbSet<ZansiPulseSearchTermMetric> ZansiPulseSearchTermMetrics => Set<ZansiPulseSearchTermMetric>();
    public DbSet<ZansiPulseSupplyDemandGap> ZansiPulseSupplyDemandGaps => Set<ZansiPulseSupplyDemandGap>();
    public DbSet<ZansiPulseRecommendationLog> ZansiPulseRecommendationLogs => Set<ZansiPulseRecommendationLog>();
    public DbSet<ZansiPulseSnapshot> ZansiPulseSnapshots => Set<ZansiPulseSnapshot>();
    public DbSet<ZansiPulseSetting> ZansiPulseSettings => Set<ZansiPulseSetting>();
}
