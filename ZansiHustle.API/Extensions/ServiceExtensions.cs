using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System;
using System.Text;
using ZansiHustle.API.Middleware;
using ZansiHustle.API.Services;
using ZansiHustle.Application.Admin.Affiliates;
using ZansiHustle.Application.Admin.Customers;
using ZansiHustle.Application.Admin.Orders;
using ZansiHustle.Application.Admin.Payments;
using ZansiHustle.Application.Admin.Seeding;
using ZansiHustle.Application.Admin.Support;
using ZansiHustle.Application.Agents.AgentApplications;
using ZansiHustle.Application.Agents.AgentMappings;
using ZansiHustle.Application.Analytics;
using ZansiHustle.Application.Auth;
using ZansiHustle.Application.BudgetTransactions;
using ZansiHustle.Application.Campaigns;
using ZansiHustle.Application.Common.Interfaces;
using ZansiHustle.Application.Common.Interfaces.Shared;
using ZansiHustle.Application.Communication.Email.Interfaces;
using ZansiHustle.Application.Communication.Email.Services;
using ZansiHustle.Application.Communications.Email.Interfaces;
using ZansiHustle.Application.Communications.Email.Mappers;
using ZansiHustle.Application.Communications.Email.Services;
using ZansiHustle.Application.ContentTasks;
using ZansiHustle.Application.Dashboard;
using ZansiHustle.Application.Events;
using ZansiHustle.Application.Fundraising;
using ZansiHustle.Application.Influencers;
using ZansiHustle.Application.Listings;
using ZansiHustle.Application.Merchants;
using ZansiHustle.Application.Orders;
using ZansiHustle.Application.Payments;
using ZansiHustle.Application.Payments.Providers;
using ZansiHustle.Application.Persistence.Admin.Affiliates;
using ZansiHustle.Application.Persistence.Admin.Customers;
using ZansiHustle.Application.Persistence.Admin.Orders;
using ZansiHustle.Application.Persistence.Admin.Payments;
using ZansiHustle.Application.Persistence.Admin.Support;
using ZansiHustle.Application.Persistence.AgentApplications;
using ZansiHustle.Application.Persistence.Analytics;
using ZansiHustle.Application.Persistence.BudgetTransactions;
using ZansiHustle.Application.Persistence.Campaigns;
using ZansiHustle.Application.Persistence.ContentTasks;
using ZansiHustle.Application.Persistence.Dashboard;
using ZansiHustle.Application.Persistence.Events;
using ZansiHustle.Application.Persistence.Fundraising;
using ZansiHustle.Application.Persistence.Identity;
using ZansiHustle.Application.Persistence.Influencers;
using ZansiHustle.Application.Persistence.Listings;
using ZansiHustle.Application.Persistence.Merchants;
using ZansiHustle.Application.Persistence.Orders;
using ZansiHustle.Application.Persistence.Payments;
using ZansiHustle.Application.Persistence.Podcasts;
using ZansiHustle.Application.Persistence.SellerCategories;
using ZansiHustle.Application.Persistence.SellerLeads;
using ZansiHustle.Application.Persistence.Users;
using ZansiHustle.Application.Podcasts;
using ZansiHustle.Application.SellerCategories;
using ZansiHustle.Application.SellerLeads;
using ZansiHustle.Application.Support;
using ZansiHustle.Application.TeamMembers;
using ZansiHustle.Application.Users;
using ZansiHustle.Domain.Identity;
using ZansiHustle.Infrastructure.Communications.Email.Mappers;
using ZansiHustle.Infrastructure.Communications.Email.Providers.Smtp;
using ZansiHustle.Infrastructure.Communications.Sms.Providers.Twilio;
using ZansiHustle.Infrastructure.Communications.WhatsApp.Providers.Twilio;
using ZansiHustle.Infrastructure.Communications.Twilio;
using ZansiHustle.Application.Communications.Sms;
using ZansiHustle.Application.Communications.Sms.Interfaces;
using ZansiHustle.Application.Communications.WhatsApp;
using ZansiHustle.Application.Communications.WhatsApp.Interfaces;
using ZansiHustle.Application.Communications.Otp;
using ZansiHustle.Application.Communications.Otp.Interfaces;
using ZansiHustle.Application.Communications.PhoneVerification;
using ZansiHustle.Infrastructure.Communications.Otp;
using ZansiHustle.Infrastructure.Configuration;
using ZansiHustle.Infrastructure.Data;
using ZansiHustle.Infrastructure.Data.Seed;
using ZansiHustle.Infrastructure.Identity;
using ZansiHustle.Infrastructure.Persistence.Admin.Affiliates;
using ZansiHustle.Infrastructure.Persistence.Admin.Customers;
using ZansiHustle.Infrastructure.Persistence.Admin.Orders;
using ZansiHustle.Infrastructure.Persistence.Admin.Payments;
using ZansiHustle.Infrastructure.Persistence.Admin.Support;
using ZansiHustle.Infrastructure.Persistence.AgentApplications;
using ZansiHustle.Infrastructure.Persistence.Analytics;
using ZansiHustle.Infrastructure.Persistence.BudgetTransactions;
using ZansiHustle.Infrastructure.Persistence.Campaigns;
using ZansiHustle.Infrastructure.Persistence.ContentTasks;
using ZansiHustle.Infrastructure.Persistence.Dashboard;
using ZansiHustle.Infrastructure.Persistence.Events;
using ZansiHustle.Infrastructure.Persistence.Fundraising;
using ZansiHustle.Infrastructure.Persistence.Influencers;
using ZansiHustle.Infrastructure.Persistence.Listings;
using ZansiHustle.Infrastructure.Persistence.Merchants;
using ZansiHustle.Infrastructure.Persistence.Orders;
using ZansiHustle.Infrastructure.Persistence.Payments;
using ZansiHustle.Infrastructure.Payments.Ozow;
using ZansiHustle.Infrastructure.Payments.Paystack;
using ZansiHustle.Infrastructure.Payments.Yoco;
using ZansiHustle.Infrastructure.Persistence.Podcasts;
using ZansiHustle.Infrastructure.Persistence.SellerCategories;
using ZansiHustle.Infrastructure.Persistence.SellerLeads;
using ZansiHustle.Infrastructure.Persistence.Users;

namespace ZansiHustle.API.Extensions;

/// <summary>
/// Centralized registration for API, infrastructure, auth, and middleware services.
/// </summary>
public static class ServiceExtensions
{
    /// <summary>
    /// Registers core ASP.NET services.
    /// </summary>
    public static IServiceCollection AddCoreServices(this IServiceCollection services)
    {
        services
            .AddControllers()
            .AddJsonOptions(options =>
            {
                // Serialize all DateTime as UTC with a trailing 'Z' so clients never
                // parse a UTC instant as local time (the "2 hours behind" bug). EF
                // reads datetime2 back as Kind=Unspecified, which would otherwise drop
                // the 'Z'. Central, additive — only changes the serialized format.
                options.JsonSerializerOptions.Converters.Add(new ZansiHustle.API.Json.UtcDateTimeConverter());
                options.JsonSerializerOptions.Converters.Add(new ZansiHustle.API.Json.NullableUtcDateTimeConverter());
            });
        services.AddEndpointsApiExplorer();
        services.AddHttpContextAccessor();

        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "ZansiHustle API",
                Version = "v1",
                Description = "Backend API for ZansiHustle mobile and web clients."
            });

            var securityScheme = new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Description = "Enter: Bearer {your JWT}",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = JwtBearerDefaults.AuthenticationScheme
                }
            };

            options.AddSecurityDefinition(JwtBearerDefaults.AuthenticationScheme, securityScheme);
            options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    securityScheme,
                    Array.Empty<string>()
                }
            });
        });

        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IUserProfileService, UserProfileService>();
        services.AddScoped<IUserSettingsService, UserSettingsService>();

        
        services.AddScoped<ITeamMemberService, TeamMemberService>();return services;
    }

    /// <summary>
    /// Registers Entity Framework Core database services. The target
    /// connection string is selected by the <c>Database:UseLive</c> flag
    /// (default <c>false</c> → UAT). Override per environment with
    /// <c>Database__UseLive=true</c> for the LIVE host. This avoids the
    /// previous pattern of toggling a <c>const bool IS_LIVE</c> in code
    /// before each deploy.
    /// </summary>
    public static IServiceCollection AddDatabaseServices(this IServiceCollection services, IConfiguration configuration)
    {
        const string UAT_DB = "UATConnection";
        const string LIVE_DB = "LiveConnection";

        var useLive = configuration.GetValue<bool>("Database:UseLive");
        var selectedEnv = useLive ? LIVE_DB : UAT_DB;

        var connectionString = configuration.GetConnectionString(selectedEnv);
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException($"ConnectionStrings:{selectedEnv} is not configured.");

        services.AddDbContext<AppDbContext>(options =>
        {
            options.UseSqlServer(
                connectionString,
                sql =>
                {
                    sql.MigrationsAssembly("ZansiHustle.Infrastructure");
                    sql.MigrationsHistoryTable("__EFMigrationsHistory", "dbo");
                });
        });

        return services;
    }

    /// <summary>
    /// Registers ASP.NET Identity and JWT bearer authentication.
    /// </summary>
    public static IServiceCollection AddIdentityServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<JwtSettings>(configuration.GetSection(JwtSettings.SectionName));

        var jwtSettings = configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>()
                         ?? throw new InvalidOperationException("JwtSettings configuration is missing.");

        if (string.IsNullOrWhiteSpace(jwtSettings.Key))
            throw new InvalidOperationException("JWT signing key is missing.");

        var key = Encoding.UTF8.GetBytes(jwtSettings.Key);

        services.AddIdentity<User, IdentityRole<Guid>>(options =>
        {
            options.Password.RequireDigit = true;
            options.Password.RequiredLength = 8;
            options.Password.RequireNonAlphanumeric = true;
            options.Password.RequireUppercase = true;
            options.Password.RequireLowercase = true;
            options.User.RequireUniqueEmail = true;
            // v1 policy: verification is soft (buyers can log in unverified; sensitive
            // seller/KYC/payment flows are gated on EmailConfirmed in feature code).
            options.SignIn.RequireConfirmedEmail = false;
            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
            options.Lockout.MaxFailedAccessAttempts = 5;
        })
        .AddEntityFrameworkStores<AppDbContext>()
        .AddDefaultTokenProviders();

        services
            .AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.RequireHttpsMetadata = true;
                options.SaveToken = true;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = true,
                    ValidIssuer = jwtSettings.Issuer,
                    ValidateAudience = true,
                    ValidAudience = jwtSettings.Audience,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                };

                // SignalR (WebSockets) can't send an Authorization header, so the
                // client passes the JWT as the `access_token` query-string on the
                // hub connection. Lift it into the auth pipeline ONLY for hub paths.
                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        var accessToken = context.Request.Query["access_token"];
                        var path = context.HttpContext.Request.Path;
                        if (!string.IsNullOrEmpty(accessToken) &&
                            path.StartsWithSegments("/hubs"))
                        {
                            context.Token = accessToken;
                        }
                        return Task.CompletedTask;
                    }
                };
            });

        services.AddAuthorization();

        return services;
    }

    /// <summary>
    /// Registers infrastructure implementations.
    /// </summary>
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services)
    {
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IUserProfileRepository, UserProfileRepository>();
        services.AddScoped<IUserSettingsRepository, UserSettingsRepository>();
        services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();

        return services;
    }

    /// <summary>
    /// Registers application-layer auth services.
    /// </summary>
    public static IServiceCollection AddAuthServices(this IServiceCollection services, IConfiguration configuration)
    {
        // QA / staging-only OTP bypass. Default Enabled=false. When on,
        // both phone-OTP (Twilio) and email-OTP (OtpService) accept
        // the configured BypassCode (default "111111") in addition to
        // their real check. Real OTP validation stays in place.
        services.Configure<AuthTestModeSettings>(
            configuration.GetSection(AuthTestModeSettings.SectionName));

        services.AddScoped<IAuthService, AuthService>();
        return services;
    }

    /// <summary>
    /// Registers API-specific services.
    /// </summary>
    public static IServiceCollection AddApiServices(this IServiceCollection services)
    {
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<IUserLookupService, UserLookupService>();
        services.AddScoped<IUatSeederService, UatSeederService>();

        // Realtime in-app events (notifications + booking status). The hub is
        // authenticated; tokens arrive on the WebSocket via the access_token
        // query-string (wired in AddIdentityServices' JwtBearer events).
        services.AddSignalR();

        return services;
    }

    public static IServiceCollection AddEmailServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<EmailSenderSettings>(configuration.GetSection(EmailSenderSettings.SectionName));

        services.AddScoped<IEmailProvider, SmtpEmailProvider>();
        services.AddScoped<IEmailService, EmailService>();
        services.AddScoped<IEmailSenderMapper, EmailSenderMapper>();
        services.AddScoped<ISupportEmailService, SupportEmailService>();
        services.AddScoped<IMerchantEmailService, MerchantEmailService>();

        // Boot-time diagnostic — logs per-sender readiness once at
        // startup so a misconfigured UAT/live environment is obvious
        // in the startup log instead of surfacing at first password
        // reset. Only logs field names; never secret values.
        services.AddHostedService<EmailSenderConfigReporter>();
        return services;
    }

    /// <summary>
    /// Registers Twilio-backed SMS and WhatsApp providers plus application services.
    /// Credentials are read from the "Twilio" configuration section. Providers
    /// fail with <c>PROVIDER_NOT_CONFIGURED</c> at call time when credentials
    /// are missing, so the app still starts in environments without Twilio.
    /// </summary>
    public static IServiceCollection AddTwilioCommunications(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<TwilioSettings>(configuration.GetSection(TwilioSettings.SectionName));

        services.AddSingleton<ITwilioClientProvider, TwilioClientProvider>();

        // SMS
        services.AddScoped<ISmsProvider, TwilioSmsProvider>();
        services.AddScoped<ISmsService, SmsService>();

        // WhatsApp
        services.AddScoped<IWhatsAppProvider, TwilioWhatsAppProvider>();
        services.AddScoped<IWhatsAppTemplateCatalog, TwilioWhatsAppTemplateCatalog>();
        services.AddScoped<IWhatsAppService, WhatsAppService>();

        // Phone verification (Twilio Verify V2). Backed by IMemoryCache for
        // the per-destination resend cooldown — added unconditionally so the
        // service can rely on it being present.
        services.AddMemoryCache();
        services.AddScoped<IPhoneVerificationService, TwilioVerifyService>();

        return services;
    }

    /// <summary>
    /// Registers the Paystack-backed payment provider + application service.
    /// The HTTP client is typed so base URL + bearer auth are injected once and
    /// never leak into callers. Fails gracefully at call time with
    /// <c>PROVIDER_NOT_CONFIGURED</c> when credentials are missing.
    /// </summary>
    public static IServiceCollection AddPaystackPayments(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<PaystackSettings>(configuration.GetSection(PaystackSettings.SectionName));

        services.AddHttpClient<IPaystackClient, PaystackClient>((sp, client) =>
        {
            var settings = sp.GetRequiredService<IOptions<PaystackSettings>>().Value;
            PaystackClient.ConfigureHttpClient(client, settings);
        });

        return services;
    }

    /// <summary>
    /// Registers the Ozow-backed payment provider, the SHA512 hash service,
    /// and a typed HTTP client. Mirrors the Paystack registration pattern;
    /// Ozow credentials are read from the "Ozow" configuration section.
    /// Calls fail at runtime with <c>PROVIDER_NOT_CONFIGURED</c> when
    /// credentials or the hash service are missing — the app still starts.
    /// </summary>
    public static IServiceCollection AddOzowPayments(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<OzowSettings>(configuration.GetSection(OzowSettings.SectionName));

        // Dev/UAT-only mock checkout switch (Payments:MockCheckoutEnabled).
        // Lets POST /api/payments/mock/order-success settle an order WITHOUT
        // Ozow, through the same paid-transition path. Default false; the
        // controller also hard-blocks it in Production.
        services.Configure<ZansiHustle.Application.Payments.MockCheckoutSettings>(
            configuration.GetSection(ZansiHustle.Application.Payments.MockCheckoutSettings.SectionName));

        services.AddScoped<IOzowHashService, OzowHashService>();

        services.AddHttpClient<IOzowClient, OzowClient>((sp, client) =>
        {
            var settings = sp.GetRequiredService<IOptions<OzowSettings>>().Value;
            OzowClient.ConfigureHttpClient(client, settings);
        });

        // Boot-time diagnostic — logs Ozow readiness, missing env vars, and
        // localhost URL warnings ONCE at startup so a misconfigured UAT
        // environment is obvious in the deploy log instead of surfacing at
        // first checkout. See OzowConfigReporter for safety notes (no
        // secret values are ever logged).
        services.AddHostedService<OzowConfigReporter>();

        return services;
    }

    /// <summary>
    /// Registers the Yoco-backed payment provider, the Standard Webhooks
    /// signature service, and a typed HTTP client. Mirrors the Ozow/Paystack
    /// registration pattern; Yoco credentials are read from the "Yoco"
    /// configuration section. Calls fail at runtime with
    /// <c>PROVIDER_NOT_CONFIGURED</c> when SecretKey or WebhookSigningSecret
    /// is missing — the app still starts so other providers (Ozow) keep
    /// working independently.
    /// </summary>
    public static IServiceCollection AddYocoPayments(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<YocoSettings>(configuration.GetSection(YocoSettings.SectionName));

        services.AddScoped<IYocoSignatureService, YocoSignatureService>();

        services.AddHttpClient<IYocoClient, YocoClient>((sp, client) =>
        {
            var settings = sp.GetRequiredService<IOptions<YocoSettings>>().Value;
            YocoClient.ConfigureHttpClient(client, settings);
        });

        services.AddHostedService<YocoConfigReporter>();

        return services;
    }

    /// <summary>
    /// Registers the channel-agnostic OTP service and the in-memory session store.
    /// Replace <see cref="InMemoryOtpStore"/> with a Redis/SQL implementation
    /// when scaling horizontally.
    /// </summary>
    public static IServiceCollection AddOtpServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<OtpSettings>(configuration.GetSection(OtpSettings.SectionName));
        services.AddSingleton<IOtpStore, InMemoryOtpStore>();
        services.AddScoped<IOtpService, OtpService>();
        return services;
    }

    /// <summary>
    /// Registers marketing and operations repositories and services.
    /// </summary>
    public static IServiceCollection AddMarketingAndOperationsServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Repositories
        services.AddScoped<IAgentApplicationRepository, AgentApplicationRepository>();
        services.AddScoped<ISellerLeadRepository, SellerLeadRepository>();
        services.AddScoped<IInfluencerRepository, InfluencerRepository>();
        services.AddScoped<IPodcastRepository, PodcastRepository>();
        services.AddScoped<ICampaignRepository, CampaignRepository>();
        services.AddScoped<IContentTaskRepository, ContentTaskRepository>();
        services.AddScoped<IBudgetTransactionRepository, BudgetTransactionRepository>();
        services.AddScoped<ILaunchOpsDashboardRepository, LaunchOpsDashboardRepository>();
        services.AddScoped<IMarketingDashboardRepository, MarketingDashboardRepository>();
        services.AddScoped<IAnalyticsRepository, AnalyticsRepository>();
        services.AddScoped<IAdminOrderRepository, AdminOrderRepository>();
        services.AddScoped<IAdminCustomerRepository, AdminCustomerRepository>();
        services.AddScoped<ZansiHustle.Application.Persistence.Admin.Users.IAdminUserRepository,
                           ZansiHustle.Infrastructure.Persistence.Admin.Users.AdminUserRepository>();
        services.AddScoped<IAdminPaymentRepository, AdminPaymentRepository>();
        services.AddScoped<IAdminAffiliateRepository, AdminAffiliateRepository>();
        services.AddScoped<IAdminSupportRepository, AdminSupportRepository>();
        services.AddScoped<IMerchantRepository, MerchantRepository>();
        services.AddScoped<ISellerCategoryRepository, SellerCategoryRepository>();
        services.AddScoped<IListingRepository, ListingRepository>();
        services.AddScoped<
            ZansiHustle.Application.Persistence.Reviews.IReviewRepository,
            ZansiHustle.Infrastructure.Persistence.Reviews.ReviewRepository>();
        services.AddScoped<
            ZansiHustle.Application.Persistence.Shops.IShopProfileRepository,
            ZansiHustle.Infrastructure.Persistence.Shops.ShopProfileRepository>();
        services.AddScoped<IOrderRepository, OrderRepository>();
        // Service bookings — scheduled-slot persistence + availability overlap
        // queries. Used by OrderService (create-time slot guard), PaymentService
        // (status sync) and ServiceBookingService (availability endpoint).
        services.AddScoped<
            ZansiHustle.Application.Persistence.ServiceBookings.IServiceBookingRepository,
            ZansiHustle.Infrastructure.Persistence.ServiceBookings.ServiceBookingRepository>();

        // ── Notifications + realtime + push (booking workflow, bell, page) ──
        // Notification is the source of truth (REST); SignalR (in-app) + OneSignal
        // (device push) are best-effort delivery layers on top. The realtime
        // notifier implementation lives in the API layer (it needs IHubContext).
        services.AddScoped<
            ZansiHustle.Application.Persistence.Notifications.INotificationRepository,
            ZansiHustle.Infrastructure.Persistence.Notifications.NotificationRepository>();
        services.AddScoped<
            ZansiHustle.Application.Persistence.Notifications.INotificationDeviceRepository,
            ZansiHustle.Infrastructure.Persistence.Notifications.NotificationDeviceRepository>();
        services.AddScoped<
            ZansiHustle.Application.Notifications.INotificationService,
            ZansiHustle.Application.Notifications.NotificationService>();
        services.AddScoped<
            ZansiHustle.Application.Realtime.IRealtimeNotifier,
            ZansiHustle.API.Realtime.SignalRRealtimeNotifier>();

        // Wallet ledger (customer refund credits) + trust-signal history.
        services.AddScoped<
            ZansiHustle.Application.Persistence.Wallets.IWalletRepository,
            ZansiHustle.Infrastructure.Persistence.Wallets.WalletRepository>();
        services.AddScoped<
            ZansiHustle.Application.Wallets.IWalletService,
            ZansiHustle.Application.Wallets.WalletService>();
        services.AddScoped<
            ZansiHustle.Application.Seller.Earnings.ISellerEarningsService,
            ZansiHustle.Application.Seller.Earnings.SellerEarningsService>();
        services.AddScoped<
            ZansiHustle.Application.Persistence.Trust.ITrustEventRepository,
            ZansiHustle.Infrastructure.Persistence.Trust.TrustEventRepository>();
        services.AddScoped<
            ZansiHustle.Application.Trust.ITrustEventService,
            ZansiHustle.Application.Trust.TrustEventService>();

        // OneSignal push — enabled by config. When OneSignal:Enabled is false or
        // the keys are blank we register the safe Null transport, so missing keys
        // can NEVER break booking/payment/notification flows (push is just skipped).
        services.Configure<ZansiHustle.Infrastructure.Notifications.Push.OneSignalOptions>(
            configuration.GetSection(
                ZansiHustle.Infrastructure.Notifications.Push.OneSignalOptions.SectionName));
        if (configuration.GetValue<bool>("OneSignal:Enabled"))
        {
            services.AddHttpClient<
                ZansiHustle.Application.Notifications.IPushNotificationService,
                ZansiHustle.Infrastructure.Notifications.Push.OneSignalPushNotificationService>();
        }
        else
        {
            services.AddScoped<
                ZansiHustle.Application.Notifications.IPushNotificationService,
                ZansiHustle.Infrastructure.Notifications.Push.NullPushNotificationService>();
        }

        services.AddScoped<IEventPlanRepository, EventPlanRepository>();
        services.AddScoped<IValuationRepository, ValuationRepository>();
        services.AddScoped<IStakeholderRepository, StakeholderRepository>();

        // Services
        services.AddScoped<IAgentApplicationService, AgentApplicationService>();
        services.AddScoped<ZansiHustle.Application.Agents.AgentProvisioning.IAgentProvisioningService,
                           ZansiHustle.Application.Agents.AgentProvisioning.AgentProvisioningService>();
        // Agent payout ledger. Settings bound from the `Agents` config
        // section (CommissionPerApprovedLead, default R10). Service
        // implementation lives in Infrastructure because it talks to
        // AppDbContext directly — no repository indirection needed for
        // this single feature.
        services.Configure<ZansiHustle.Application.Agents.AgentPayouts.AgentEarningsSettings>(
            configuration.GetSection(
                ZansiHustle.Application.Agents.AgentPayouts.AgentEarningsSettings.SectionName));
        services.AddScoped<ZansiHustle.Application.Agents.AgentPayouts.IAgentPayoutService,
                           ZansiHustle.Infrastructure.Agents.AgentPayoutService>();
        // Buyer ↔ seller / buyer ↔ merchant chat. Service lives in
        // Infrastructure because it queries AppDbContext directly
        // (same pattern as AgentPayoutService). See
        // ZansiHustle.Application.Chat.IChatService for the contract.
        services.AddScoped<ZansiHustle.Application.Chat.IChatService,
                           ZansiHustle.Infrastructure.Chat.ChatService>();
        services.AddScoped<ISellerLeadService, SellerLeadService>();
        services.AddScoped<IInfluencerService, InfluencerService>();
        services.AddScoped<IPodcastService, PodcastService>();
        services.AddScoped<ICampaignService, CampaignService>();
        services.AddScoped<IContentTaskService, ContentTaskService>();
        services.AddScoped<IBudgetTransactionService, BudgetTransactionService>();
        services.AddScoped<ILaunchOpsDashboardService, LaunchOpsDashboardService>();
        services.AddScoped<IMarketingDashboardService, MarketingDashboardService>();
        services.AddScoped<IAnalyticsService, AnalyticsService>();
        services.AddScoped<IAdminOrderService, AdminOrderService>();
        services.AddScoped<IAdminCustomerService, AdminCustomerService>();
        services.AddScoped<ZansiHustle.Application.Admin.Users.IAdminUserService,
                           ZansiHustle.Application.Admin.Users.AdminUserService>();
        services.AddScoped<IAdminPaymentService, AdminPaymentService>();
        services.AddScoped<IAdminAffiliateService, AdminAffiliateService>();
        services.AddScoped<IAdminSupportService, AdminSupportService>();
        services.AddScoped<IMerchantService, MerchantService>();
        services.AddScoped<ISellerCategoryService, SellerCategoryService>();
        services.AddScoped<IAgentMappingService, AgentMappingService>();
        services.AddScoped<IListingService, ListingService>();
        services.AddScoped<
            ZansiHustle.Application.ServiceBookings.IServiceBookingService,
            ZansiHustle.Application.ServiceBookings.ServiceBookingService>();
        services.AddScoped<
            ZansiHustle.Application.Reviews.IReviewService,
            ZansiHustle.Application.Reviews.ReviewService>();
        services.AddScoped<
            ZansiHustle.Application.Shops.IShopProfileService,
            ZansiHustle.Application.Shops.ShopProfileService>();
        services.AddScoped<IOrderService, OrderService>();
        // Customer financial overview (spending + combined transactions).
        services.AddScoped<
            ZansiHustle.Application.CustomerFinance.ICustomerFinanceService,
            ZansiHustle.Application.CustomerFinance.CustomerFinanceService>();
        // Seller accountability foundation (records fulfilment incidents; admin applies).
        services.AddScoped<
            ZansiHustle.Application.Sellers.Incidents.ISellerIncidentService,
            ZansiHustle.Infrastructure.Sellers.SellerIncidentService>();
        // Seller actionable-request count (Sell-tab badge).
        services.AddScoped<
            ZansiHustle.Application.Sellers.Requests.ISellerRequestsService,
            ZansiHustle.Application.Sellers.Requests.SellerRequestsService>();
        services.AddScoped<IPaymentRepository, PaymentRepository>();
        services.AddScoped<IPaymentService, PaymentService>();
        services.AddScoped<IEventPlanService, EventPlanService>();
        services.AddScoped<IFundraisingService, FundraisingService>();

        // Casual peer-to-peer Marketplace — separate domain from
        // merchant Listings; owned by a User, not a Merchant.
        services.AddScoped<
            ZansiHustle.Application.Persistence.Marketplace.IMarketplaceListingRepository,
            ZansiHustle.Infrastructure.Persistence.Marketplace.MarketplaceListingRepository>();
        services.AddScoped<
            ZansiHustle.Application.Marketplace.IMarketplaceListingService,
            ZansiHustle.Application.Marketplace.MarketplaceListingService>();

        // Standalone referral / affiliate system.
        services.AddScoped<ZansiHustle.Application.Persistence.Referrals.IAffiliateProfileRepository,
                           ZansiHustle.Infrastructure.Persistence.Referrals.AffiliateProfileRepository>();
        services.AddScoped<ZansiHustle.Application.Persistence.Referrals.IUserReferralRepository,
                           ZansiHustle.Infrastructure.Persistence.Referrals.UserReferralRepository>();
        services.AddScoped<ZansiHustle.Application.Persistence.Referrals.IReferralClickRepository,
                           ZansiHustle.Infrastructure.Persistence.Referrals.ReferralClickRepository>();
        services.AddScoped<ZansiHustle.Application.Referrals.IReferralService,
                           ZansiHustle.Application.Referrals.ReferralService>();

        // Shared media / blob-metadata system. Storage backend is chosen by
        // config: if Storage:R2:AccountId is set we use Cloudflare R2, else
        // we fall back to the local filesystem adapter so local dev still
        // works out-of-the-box with no credentials.
        services.AddHttpContextAccessor();
        services.AddScoped<ZansiHustle.Application.Persistence.Media.IMediaAssetRepository,
                           ZansiHustle.Infrastructure.Persistence.Media.MediaAssetRepository>();

        var r2AccountId = configuration["Storage:R2:AccountId"];
        if (!string.IsNullOrWhiteSpace(r2AccountId))
        {
            services.AddScoped<ZansiHustle.Application.Media.Storage.IMediaStorageService,
                               ZansiHustle.API.Storage.R2MediaStorageService>();
        }
        else
        {
            services.AddScoped<ZansiHustle.Application.Media.Storage.IMediaStorageService,
                               ZansiHustle.API.Storage.LocalFilesystemMediaStorageService>();
        }

        services.AddScoped<ZansiHustle.Application.Media.IMediaService,
                           ZansiHustle.Application.Media.MediaService>();

        // Shared read-time URL refresher. Used by MerchantService and
        // MarketplaceListingService to recover from rotted R2 signed
        // URLs persisted on entity rows (legacy uploads, or any path
        // that ran without Storage:R2:PublicBaseUrl configured).
        services.AddScoped<ZansiHustle.Application.Media.Storage.IStorageUrlResolver,
                           ZansiHustle.Application.Media.Storage.StorageUrlResolver>();

        // Buyer engagement — likes / follows / saves. One service +
        // one repository drive all four target domains (Listing /
        // MarketplaceListing / ShopProfile / Merchant) so the
        // controller, the React Query hooks, and the future Saved
        // screen all share a uniform Result envelope.
        services.AddScoped<
            ZansiHustle.Application.Persistence.Engagement.IEngagementRepository,
            ZansiHustle.Infrastructure.Persistence.Engagement.EngagementRepository>();
        services.AddScoped<
            ZansiHustle.Application.Engagement.IEngagementService,
            ZansiHustle.Application.Engagement.EngagementService>();

        // ZansiPulse — the ZansiHustle intelligence layer. Event tracking,
        // interest scoring, recommendations, trending, supply/demand and
        // dashboard rollups. Service lives in Infrastructure (talks to
        // AppDbContext directly, same as ChatService) so it can run the
        // cross-entity joins recommendations need without a wide repository.
        services.AddScoped<
            ZansiHustle.Application.ZansiPulse.IZansiPulseService,
            ZansiHustle.Infrastructure.ZansiPulse.ZansiPulseService>();

        // ZansiDispatch — the logistics control layer. Provider-agnostic
        // checkout quotes + shipment/reconciliation command centre. Bind the
        // provider config section (all-optional; missing Courier Guy/Shiplogic
        // credentials never crash startup — they're validated only when that
        // provider is enabled, which Phase 1 never does). Both Phase 1 quote
        // providers register against the same interface; the service resolves
        // the configured one and falls back to ManualFallback.
        services.Configure<ZansiHustle.Infrastructure.Configuration.ZansiDispatchOptions>(
            configuration.GetSection(ZansiHustle.Infrastructure.Configuration.ZansiDispatchOptions.SectionName));
        // UAT/dev quote observability switch (root "DispatchDebug" section;
        // toggle with DispatchDebug__Enabled=true). Off by default.
        services.Configure<ZansiHustle.Infrastructure.Configuration.DispatchDebugOptions>(
            configuration.GetSection(ZansiHustle.Infrastructure.Configuration.DispatchDebugOptions.SectionName));

        // Deterministic in-house providers (always available, no credentials).
        services.AddScoped<
            ZansiHustle.Application.ZansiDispatch.Providers.IZansiDispatchQuoteProvider,
            ZansiHustle.Infrastructure.ZansiDispatch.Providers.InternalEstimateProvider>();
        services.AddScoped<
            ZansiHustle.Application.ZansiDispatch.Providers.IZansiDispatchQuoteProvider,
            ZansiHustle.Infrastructure.ZansiDispatch.Providers.ManualFallbackProvider>();

        // Courier Guy / Shiplogic (Provider #1). Typed HttpClient so the base
        // address/timeout are managed by IHttpClientFactory; the Bearer key is
        // attached per request inside the provider (never logged). The SAME
        // provider implements both quote + shipment interfaces. It self-gates on
        // Enabled + IsConfigured, so a deployment without credentials binds fine
        // and the API starts — the provider simply reports IsEnabled == false.
        services.AddHttpClient<ZansiHustle.Infrastructure.ZansiDispatch.Providers.CourierGuy.CourierGuyProvider>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(30);
        });
        services.AddScoped<ZansiHustle.Application.ZansiDispatch.Providers.IZansiDispatchQuoteProvider>(
            sp => sp.GetRequiredService<ZansiHustle.Infrastructure.ZansiDispatch.Providers.CourierGuy.CourierGuyProvider>());
        services.AddScoped<ZansiHustle.Application.ZansiDispatch.Providers.IZansiDispatchShipmentProvider>(
            sp => sp.GetRequiredService<ZansiHustle.Infrastructure.ZansiDispatch.Providers.CourierGuy.CourierGuyProvider>());

        services.AddScoped<
            ZansiHustle.Application.ZansiDispatch.IZansiDispatchService,
            ZansiHustle.Infrastructure.ZansiDispatch.ZansiDispatchService>();

        return services;
    }

    public static IServiceCollection AddCustomCors(this IServiceCollection services)
    {
        services.AddCors(options =>
        {
            options.AddPolicy("FrontendCors", policy =>
            {
                if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development")
                {
                    policy
                        .AllowAnyOrigin()
                        .AllowAnyHeader()
                        .AllowAnyMethod();
                }
                else
                {
                    policy
                        .WithOrigins(
                            "http://localhost:5173",
                            "https://localhost:5173",
                            "http://localhost:8081",
                            "https://localhost:8081",
                            "https://portal.zansihustle.com",
                            "https://www.zansihustle.com",
                            "https://www.zansihustle.co.za",
                            "https://uat.portal.zansihustle.com",
                            "https://uat.pulse.zansihustle.com",
                            "https://uat.dispatch.zansihustle.com"
                        )
                        .AllowAnyHeader()
                        .AllowAnyMethod()
                        .AllowCredentials();
                }
            });
        });

        return services;
    }

    /// <summary>
    /// Configures the HTTP request pipeline.
    /// </summary>
    public static WebApplication ConfigureMiddleware(this WebApplication app)
    {
        if (true)//app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseMiddleware<ExceptionHandlingMiddleware>();

        app.UseRouting();
        app.UseCors("FrontendCors");

        app.UseHttpsRedirection();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapControllers();

        // Authenticated realtime hub for user-targeted in-app events
        // (NotificationCreated / NotificationUnreadCountChanged / BookingStatusChanged).
        app.MapHub<ZansiHustle.API.Realtime.RealtimeHub>("/hubs/realtime");

        return app;
    }

    /// <summary>
    /// Applies migrations and seeds startup data.
    ///
    /// Failure model — split between the two phases:
    ///   • Migration phase (<see cref="RelationalDatabaseFacadeExtensions.MigrateAsync"/>):
    ///     stays <b>fatal</b>. If the schema cannot be brought up to the
    ///     compiled model, the running code's queries WILL break at request
    ///     time. Better to refuse to boot — IIS surfaces 500.30 with the
    ///     real reason in stdout, and ops can roll forward / fix DDL
    ///     permissions / reconcile <c>__EFMigrationsHistory</c>.
    ///   • Seeder phase (Identity roles / EventType templates / ZansiPulse
    ///     setting defaults): <b>non-fatal</b>. Seeders insert tuning rows
    ///     that the runtime already has code-level fallbacks for
    ///     (<c>ZansiPulseDefaults</c>, code-defined role names, baked
    ///     event-type templates). A broken seeder no longer takes the
    ///     whole API down — the failure is logged and the host continues.
    ///     The CEO/admin dashboard surfaces the missing rows separately;
    ///     ops can retune them out-of-band.
    /// </summary>
    public static async Task SeedApplicationAsync(this WebApplication app)
    {
        await using var scope = app.Services.CreateAsyncScope();
        var services = scope.ServiceProvider;
        var logger = services.GetRequiredService<ILogger<Program>>();

        // ── Phase 1: migrations (FATAL on failure) ────────────────────────
        try
        {
            var dbContext = services.GetRequiredService<AppDbContext>();
            await dbContext.Database.MigrateAsync();
        }
        catch (Exception ex)
        {
            logger.LogCritical(ex,
                "Database migration failed during startup. Host will not start. " +
                "Check stdout (web.config: stdoutLogEnabled=true) for the underlying SQL exception, " +
                "and verify the app DB user has DDL rights (CREATE TABLE / CREATE INDEX). " +
                "Migrations can be applied out-of-band with: dotnet ef database update " +
                "--project ZansiHustle.Infrastructure --startup-project ZansiHustle.API.");
            throw;
        }

        // ── Phase 2: seeders (NON-FATAL on failure) ───────────────────────
        // Each seeder is wrapped individually so a failure in one does not
        // skip the rest. Order is preserved from the original implementation.
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
        var dbContextForSeed = services.GetRequiredService<AppDbContext>();

        await SafeSeedAsync(logger, "IdentityRoles",
            () => IdentitySeeder.SeedRolesAsync(roleManager));

        await SafeSeedAsync(logger, "EventTypeTemplates",
            () => EventTypeTemplateSeeder.SeedAsync(dbContextForSeed));

        // ZansiPulse tuning knobs (event weights / recommendation blend /
        // interest bounds). Idempotent — only inserts missing keys, and
        // ZansiPulseDefaults provides code-level fallbacks if the rows are
        // ever missing, so a transient failure here cannot affect correctness.
        await SafeSeedAsync(logger, "ZansiPulseSettings",
            () => ZansiPulseSettingsSeeder.SeedAsync(dbContextForSeed));

        // ZansiDispatch logistics settings (fees / buffers / expiry / flags).
        // Idempotent; ZansiDispatchDefaults provides code-level fallbacks, so a
        // transient failure here cannot affect quoting correctness.
        await SafeSeedAsync(logger, "ZansiDispatchSettings",
            () => ZansiDispatchSettingsSeeder.SeedAsync(dbContextForSeed));

        // ── ZansiDispatch provider-mode startup banner / launch-safety guard ──
        // Make the active delivery-pricing mode visible at boot, and SHOUT if a
        // deployment has configured Courier Guy as the default before its
        // response-shape mappings are validated against a real sandbox response.
        try
        {
            var dispatchOpts = services
                .GetRequiredService<Microsoft.Extensions.Options.IOptions<
                    ZansiHustle.Infrastructure.Configuration.ZansiDispatchOptions>>().Value;
            var configuredDefault = dispatchOpts.DefaultProvider;
            var cg = dispatchOpts.CourierGuy;
            var courierGuyIsDefault =
                string.Equals(configuredDefault, "CourierGuy", StringComparison.OrdinalIgnoreCase);

            // Real-booking kill-switch warning — independent of the default
            // provider. When booking is ON, create-from-quote can place REAL,
            // billable courier shipments. SandboxMode does NOT prevent charges.
            if (cg.Enabled && cg.AllowShipmentBooking)
            {
                logger.LogWarning(
                    "[ZansiDispatch] Courier shipment BOOKING is ENABLED (CourierGuy.Enabled=true, AllowShipmentBooking=true, Configured={Configured}, SandboxMode={Sandbox}). " +
                    "create-from-quote can place REAL, billable bookings. SandboxMode does NOT prevent billable bookings — ensure the configured Shiplogic/Courier Guy API key is a sandbox/test key. " +
                    "Set ZansiDispatch__CourierGuy__AllowShipmentBooking=false to disable booking entirely.",
                    cg.IsConfigured, cg.SandboxMode);
            }
            else if (cg.Enabled)
            {
                logger.LogInformation(
                    "[ZansiDispatch] Courier shipment booking is DISABLED (AllowShipmentBooking=false). NEW bookings (create-from-quote / auto-book / retry) are blocked. Risk-reducing ops on already-booked shipments still work — provider cancellation={Cancel}, status/tracking refresh={Status} (defaults true).",
                    cg.AllowProviderCancellation, cg.AllowProviderStatusRefresh);
            }

            if (courierGuyIsDefault && cg.Enabled)
            {
                logger.LogWarning(
                    "[ZansiDispatch] DefaultProvider=CourierGuy is ACTIVE (CourierGuy.Enabled=true, Configured={Configured}, SandboxMode={Sandbox}). " +
                    "Courier Guy response-shape mappings are NOT yet validated against a real sandbox response — buyers could be charged a courier-quoted price built from an unconfirmed mapping. " +
                    "VERIFY the /rates mapping (SandboxMode shape logs) before using this in any environment that charges real money, or set ZansiDispatch__DefaultProvider=InternalEstimate.",
                    cg.IsConfigured, cg.SandboxMode);
            }
            else
            {
                logger.LogInformation(
                    "[ZansiDispatch] Delivery pricing mode: default={Default} courierGuyEnabled={CgEnabled} courierGuyConfigured={CgConfigured} fallbackToInternalEstimate={Fallback}. " +
                    "Buyers see managed 'ZansiHustle Dispatch' pricing unless a real Courier Guy quote is returned.",
                    string.IsNullOrWhiteSpace(configuredDefault) ? "InternalEstimate (default)" : configuredDefault,
                    cg.Enabled, cg.IsConfigured, dispatchOpts.FallbackToInternalEstimate);
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "[ZansiDispatch] Could not log delivery provider-mode banner at startup.");
        }
    }

    /// <summary>
    /// Runs a single seeder and converts any exception into a logged
    /// warning. The host keeps booting — the dashboard / admin surface
    /// will show the absent tuning rows separately, and ops can retry
    /// out-of-band without redeploying.
    /// </summary>
    private static async Task SafeSeedAsync(ILogger logger, string name, Func<Task> seed)
    {
        try
        {
            await seed();
            logger.LogInformation("Seeder '{Name}' completed.", name);
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Seeder '{Name}' failed during startup. Host will continue; " +
                "the missing data can be reapplied out-of-band. See exception for the root cause.",
                name);
        }
    }
}


