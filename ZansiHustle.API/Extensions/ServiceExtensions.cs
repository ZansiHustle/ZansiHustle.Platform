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
using ZansiHustle.Application.Admin.Seeding;
using ZansiHustle.Application.Agents.AgentApplications;
using ZansiHustle.Application.Agents.AgentMappings;
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
using ZansiHustle.Application.Influencers;
using ZansiHustle.Application.Listings;
using ZansiHustle.Application.Merchants;
using ZansiHustle.Application.Orders;
using ZansiHustle.Application.Payments;
using ZansiHustle.Application.Payments.Providers;
using ZansiHustle.Application.Persistence.AgentApplications;
using ZansiHustle.Application.Persistence.BudgetTransactions;
using ZansiHustle.Application.Persistence.Campaigns;
using ZansiHustle.Application.Persistence.ContentTasks;
using ZansiHustle.Application.Persistence.Dashboard;
using ZansiHustle.Application.Persistence.Events;
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
using ZansiHustle.Infrastructure.Communications.Otp;
using ZansiHustle.Infrastructure.Configuration;
using ZansiHustle.Infrastructure.Data;
using ZansiHustle.Infrastructure.Data.Seed;
using ZansiHustle.Infrastructure.Identity;
using ZansiHustle.Infrastructure.Persistence.AgentApplications;
using ZansiHustle.Infrastructure.Persistence.BudgetTransactions;
using ZansiHustle.Infrastructure.Persistence.Campaigns;
using ZansiHustle.Infrastructure.Persistence.ContentTasks;
using ZansiHustle.Infrastructure.Persistence.Dashboard;
using ZansiHustle.Infrastructure.Persistence.Events;
using ZansiHustle.Infrastructure.Persistence.Influencers;
using ZansiHustle.Infrastructure.Persistence.Listings;
using ZansiHustle.Infrastructure.Persistence.Merchants;
using ZansiHustle.Infrastructure.Persistence.Orders;
using ZansiHustle.Infrastructure.Persistence.Payments;
using ZansiHustle.Infrastructure.Payments.Paystack;
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
        services.AddControllers();
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
    /// Registers Entity Framework Core database services.
    /// </summary>
    public static IServiceCollection AddDatabaseServices(this IServiceCollection services, IConfiguration configuration)
    {
        const bool IS_LIVE = false;
        const string UAT_DB = "UATConnection";
        const string LIVE_DB = "LiveConnection";

        const string SELECTED_ENV = IS_LIVE ? LIVE_DB : UAT_DB; 

        var connectionString = configuration.GetConnectionString(SELECTED_ENV)
                               ?? throw new InvalidOperationException($"{SELECTED_ENV} is not configured.");

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
    public static IServiceCollection AddAuthServices(this IServiceCollection services)
    {
        services.AddScoped<IAuthService, AuthService>();
        return services;
    }

    /// <summary>
    /// Registers API-specific services.
    /// </summary>
    public static IServiceCollection AddApiServices(this IServiceCollection services)
    {
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<IUatSeederService, UatSeederService>();
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
    public static IServiceCollection AddMarketingAndOperationsServices(this IServiceCollection services)
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
        services.AddScoped<IMerchantRepository, MerchantRepository>();
        services.AddScoped<ISellerCategoryRepository, SellerCategoryRepository>();
        services.AddScoped<IListingRepository, ListingRepository>();
        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddScoped<IEventPlanRepository, EventPlanRepository>();

        // Services
        services.AddScoped<IAgentApplicationService, AgentApplicationService>();
        services.AddScoped<ISellerLeadService, SellerLeadService>();
        services.AddScoped<IInfluencerService, InfluencerService>();
        services.AddScoped<IPodcastService, PodcastService>();
        services.AddScoped<ICampaignService, CampaignService>();
        services.AddScoped<IContentTaskService, ContentTaskService>();
        services.AddScoped<IBudgetTransactionService, BudgetTransactionService>();
        services.AddScoped<ILaunchOpsDashboardService, LaunchOpsDashboardService>();
        services.AddScoped<IMarketingDashboardService, MarketingDashboardService>();
        services.AddScoped<IMerchantService, MerchantService>();
        services.AddScoped<ISellerCategoryService, SellerCategoryService>();
        services.AddScoped<IAgentMappingService, AgentMappingService>();
        services.AddScoped<IListingService, ListingService>();
        services.AddScoped<IOrderService, OrderService>();
        services.AddScoped<IPaymentRepository, PaymentRepository>();
        services.AddScoped<IPaymentService, PaymentService>();
        services.AddScoped<IEventPlanService, EventPlanService>();

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
                            "https://portal.zansihustle.com",
                            "https://www.zansihustle.com",
                            "https://www.zansihustle.co.za"
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

        return app;
    }

    /// <summary>
    /// Applies migrations and seeds startup data.
    /// </summary>
    public static async Task SeedApplicationAsync(this WebApplication app)
    {
        await using var scope = app.Services.CreateAsyncScope();
        var services = scope.ServiceProvider;

        try
        {
            var dbContext = services.GetRequiredService<AppDbContext>();
            await dbContext.Database.MigrateAsync();

            var roleManager = services.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
            await IdentitySeeder.SeedRolesAsync(roleManager);

            await EventTypeTemplateSeeder.SeedAsync(dbContext);
        }
        catch (Exception ex)
        {
            var logger = services.GetRequiredService<ILogger<Program>>();
            logger.LogError(ex, "An error occurred during application startup seeding.");
            throw;
        }
    }
}


