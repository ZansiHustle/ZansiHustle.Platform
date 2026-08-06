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
using ZansiHustle.Domain.Notifications;
using ZansiHustle.Domain.Orders;
using ZansiHustle.Domain.Payments;
using ZansiHustle.Domain.Podcasts;
using ZansiHustle.Domain.Referrals;
using ZansiHustle.Domain.Reviews;
using ZansiHustle.Domain.SellerCategories;
using ZansiHustle.Domain.ServiceBookings;
using ZansiHustle.Domain.Trust;
using ZansiHustle.Domain.Wallets;
using ZansiHustle.Domain.Shops;
using ZansiHustle.Domain.SellerLeads;
using ZansiHustle.Domain.ZansiPulse;
using ZansiHustle.Domain.ZansiDispatch;
using ZansiHustle.Domain.Finance;
using ZansiHustle.Domain.AppVersion;

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

    /// <summary>Seller fulfilment incidents (accountability foundation). Config auto-applied.</summary>
    public DbSet<SellerIncident> SellerIncidents => Set<SellerIncident>();

    public DbSet<SellerCategory> SellerCategories => Set<SellerCategory>();
    public DbSet<SellerSubcategory> SellerSubcategories => Set<SellerSubcategory>();

    public DbSet<Listing> Listings => Set<Listing>();
    public DbSet<ListingVariant> ListingVariants => Set<ListingVariant>();

    // ── Casual peer-to-peer Marketplace (separate from merchant Listings) ──
    public DbSet<MarketplaceListing> MarketplaceListings => Set<MarketplaceListing>();
    public DbSet<MarketplaceListingImage> MarketplaceListingImages => Set<MarketplaceListingImage>();

    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();

    // ── Service bookings (scheduled service slots tied to an Order) ────────
    // Queryable source of truth for booking availability / double-book
    // prevention. Config:
    // ZansiHustle.Infrastructure.Persistence...ServiceBookingConfiguration.
    public DbSet<ServiceBooking> ServiceBookings => Set<ServiceBooking>();

    // ── Two-way service-booking reviews (customer↔provider) ────────────────
    // Dedicated table (not the polymorphic Reviews table) so direction +
    // reviewee are modelled explicitly. Config:
    // ZansiHustle.Infrastructure.Data.Configurations.ServiceBookings.ServiceBookingReviewConfiguration.
    public DbSet<ServiceBookingReview> ServiceBookingReviews => Set<ServiceBookingReview>();

    // ── Notifications (in-app bell + page) and push device registry ────────
    // Notification is the source of truth behind the bell/page; SignalR +
    // OneSignal are best-effort delivery layers on top. Configs:
    // ZansiHustle.Infrastructure.Data.Configurations.Notifications.*.
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<NotificationDevice> NotificationDevices => Set<NotificationDevice>();

    // ── Wallet ledger (customer refunds/credits) + trust signals ───────────
    // Wallet.AvailableBalance is kept in lockstep with the WalletTransactions
    // ledger (source of truth). TrustEvents are append-only behaviour history
    // for a future scoring layer. Configs auto-applied below.
    public DbSet<Wallet> Wallets => Set<Wallet>();
    public DbSet<WalletTransaction> WalletTransactions => Set<WalletTransaction>();
    public DbSet<WalletWithdrawalRequest> WalletWithdrawalRequests => Set<WalletWithdrawalRequest>();
    public DbSet<TrustEvent> TrustEvents => Set<TrustEvent>();

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

    // ── Trust & safety (App Store 1.2): content reports + user blocks ──────
    public DbSet<ZansiHustle.Domain.Reports.ContentReport> ContentReports => Set<ZansiHustle.Domain.Reports.ContentReport>();
    public DbSet<ZansiHustle.Domain.Blocks.UserBlock> UserBlocks => Set<ZansiHustle.Domain.Blocks.UserBlock>();

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

    // ── App runtime configs — admin-controlled remote feature flags ─────────
    // One flexible row type the Portal Super Admin toggles and the mobile app
    // reads (public rows only) to gate features at runtime.
    public DbSet<ZansiHustle.Domain.AppConfigs.AppRuntimeConfig> AppRuntimeConfigs =>
        Set<ZansiHustle.Domain.AppConfigs.AppRuntimeConfig>();

    // ── ZansiDispatch — logistics control layer ────────────────────────────
    // Checkout delivery quotes/options, shipment lifecycle + reconciliation,
    // audit ledger, provider request logs, and tunable settings. Reference ids
    // are loose (un-FK'd) operational columns; configs live in
    // ZansiHustle.Infrastructure.Persistence.ZansiDispatch.ZansiDispatchConfigurations
    // (auto-applied above).
    public DbSet<ZansiDispatchQuote> ZansiDispatchQuotes => Set<ZansiDispatchQuote>();
    public DbSet<ZansiDispatchQuoteOption> ZansiDispatchQuoteOptions => Set<ZansiDispatchQuoteOption>();
    public DbSet<ZansiDispatchShipment> ZansiDispatchShipments => Set<ZansiDispatchShipment>();
    public DbSet<ZansiDispatchShipmentEvent> ZansiDispatchShipmentEvents => Set<ZansiDispatchShipmentEvent>();
    public DbSet<ZansiDispatchShipmentAction> ZansiDispatchShipmentActions => Set<ZansiDispatchShipmentAction>();
    public DbSet<ZansiDispatchLedgerEntry> ZansiDispatchLedgerEntries => Set<ZansiDispatchLedgerEntry>();
    public DbSet<ZansiDispatchProviderRequestLog> ZansiDispatchProviderRequestLogs => Set<ZansiDispatchProviderRequestLog>();
    public DbSet<ZansiDispatchSetting> ZansiDispatchSettings => Set<ZansiDispatchSetting>();

    // ── Finance ledgers (seller proceeds + platform book) ──────────────────
    // Append-only accounting tables. Seller funds (SellerLedgerEntries) and
    // platform funds (PlatformLedgerEntries) are kept STRICTLY separate. Written
    // by the idempotent reconcile job (LedgerReconcileService); the seller
    // dashboard reads the SAME fee math via SellerFeeCalculator. Configs in
    // ZansiHustle.Infrastructure.Data.Configurations.Finance.* (auto-applied).
    public DbSet<SellerLedgerEntry> SellerLedgerEntries => Set<SellerLedgerEntry>();
    public DbSet<PlatformLedgerEntry> PlatformLedgerEntries => Set<PlatformLedgerEntry>();

    // ── Mobile app version-control rules ───────────────────────────────────
    // One row per (Platform, Channel) store target driving the public
    // GET /api/app-version/mobile check and the App-Version gate middleware.
    // A missing/disabled row falls back to Platform+Generic, then to the
    // appsettings "MobileAppVersion" section — force-update is NEVER implied
    // by absence. Config:
    // ZansiHustle.Infrastructure.Persistence.AppVersion.MobileAppVersionRuleConfiguration
    // (auto-applied above).
    public DbSet<MobileAppVersionRule> MobileAppVersionRules => Set<MobileAppVersionRule>();
}
