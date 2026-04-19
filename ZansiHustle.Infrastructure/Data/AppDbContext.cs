using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System.Reflection;
using ZansiHustle.Domain.Agents.AgentApplications;
using ZansiHustle.Domain.BudgetTransactions;
using ZansiHustle.Domain.Campaigns;
using ZansiHustle.Domain.ContentTasks;
using ZansiHustle.Domain.Events;
using ZansiHustle.Domain.Fundraising;
using ZansiHustle.Domain.Identity;
using ZansiHustle.Domain.Influencers;
using ZansiHustle.Domain.Listings;
using ZansiHustle.Domain.Media;
using ZansiHustle.Domain.Merchants;
using ZansiHustle.Domain.Orders;
using ZansiHustle.Domain.Payments;
using ZansiHustle.Domain.Podcasts;
using ZansiHustle.Domain.Referrals;
using ZansiHustle.Domain.SellerCategories;
using ZansiHustle.Domain.SellerLeads;

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
}
