using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SentinelOps.Application.Common.Interfaces;
using SentinelOps.Infrastructure.Caching;
using SentinelOps.Infrastructure.Identity;
using SentinelOps.Infrastructure.Persistence;
using SentinelOps.Infrastructure.Services;
using StackExchange.Redis;

namespace SentinelOps.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var postgresConnection = configuration.GetConnectionString("DefaultConnection")
            ?? "Host=localhost;Port=5432;Database=sentinelops;Username=postgres;Password=postgres";

        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseNpgsql(postgresConnection, b =>
            {
                b.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName);
                b.EnableRetryOnFailure(maxRetryCount: 3, maxRetryDelay: TimeSpan.FromSeconds(5), errorCodesToAdd: null);
            }));

        services.AddScoped<IApplicationDbContext>(sp =>
            sp.GetRequiredService<ApplicationDbContext>());

        services.AddIdentityCore<ApplicationUser>(options =>
        {
            options.Password.RequireDigit = true;
            options.Password.RequireLowercase = true;
            options.Password.RequireUppercase = true;
            options.Password.RequireNonAlphanumeric = true;
            options.Password.RequiredLength = 8;
            options.Lockout.AllowedForNewUsers = true;
            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
            options.Lockout.MaxFailedAccessAttempts = 5;
        })
        .AddRoles<IdentityRole>()
        .AddEntityFrameworkStores<ApplicationDbContext>();

        var redisConnection = configuration.GetConnectionString("Redis")
            ?? "localhost:6379,abortConnect=false";

        services.AddSingleton<IConnectionMultiplexer>(_ =>
        {
            var options = ConfigurationOptions.Parse(redisConnection);
            options.AbortOnConnectFail = false;
            options.ConnectTimeout = 3000;
            return ConnectionMultiplexer.Connect(options);
        });

        services.AddSingleton<ICacheService, RedisCacheService>();
        services.AddSingleton<ITelemetryBuffer, RedisTelemetryBuffer>();

        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<ICurrentTenantService, CurrentTenantService>();
        services.AddScoped<IOrganizationService, OrganizationService>();
        services.AddScoped<IOnboardingService, OnboardingService>();
        services.AddScoped<ITaxCalculationService, DefaultTaxCalculationService>();
        services.AddScoped<ISubscriptionService, SubscriptionService>();
        services.AddScoped<IBillingService, BillingService>();
        services.AddScoped<IDashboardService, DashboardService>();
        services.AddScoped<IApmService, ApmService>();
        services.AddScoped<IIncidentIntelligenceProvider, Services.Intelligence.DeterministicIncidentIntelligenceProvider>();
        services.AddScoped<IStatusPageService, Services.StatusPages.StatusPageService>();
        services.AddScoped<IIdentityService, IdentityService>();
        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<IApiKeyService, ApiKeyService>();
        services.AddScoped<IAuditService, AuditService>();
        services.AddScoped<IReportingService, ReportingService>();
        services.AddScoped<IRetentionService, RetentionService>();
        services.AddScoped<IAlertEvaluationService, Services.Alerting.AlertEvaluationService>();
        services.AddScoped<IRealtimeNotifier, Services.NullRealtimeNotifier>();

        // Notifications with SSRF DNS Rebinding & Redirect Hijacking Hardening
        services.AddHttpClient<Services.Notifications.WebhookNotificationProvider>()
            .ConfigurePrimaryHttpMessageHandler(() =>
            {
                return new SocketsHttpHandler
                {
                    AllowAutoRedirect = false,
                    ConnectCallback = async (context, cancellationToken) =>
                    {
                        var entry = await System.Net.Dns.GetHostEntryAsync(context.DnsEndPoint.Host, cancellationToken);
                        var targetIp = entry.AddressList.FirstOrDefault()
                            ?? throw new HttpRequestException($"DNS resolution yielded no address for {context.DnsEndPoint.Host}");

                        if (Security.SsrfValidator.IsPrivateOrRestrictedIp(targetIp))
                        {
                            throw new HttpRequestException($"SSRF Violation: Socket connection to restricted IP {targetIp} blocked.");
                        }

                        var socket = new System.Net.Sockets.Socket(System.Net.Sockets.SocketType.Stream, System.Net.Sockets.ProtocolType.Tcp);
                        await socket.ConnectAsync(new System.Net.IPEndPoint(targetIp, context.DnsEndPoint.Port), cancellationToken);
                        return new System.Net.Sockets.NetworkStream(socket, ownsSocket: true);
                    }
                };
            });
        services.AddScoped<Services.Notifications.INotificationProvider, Services.Notifications.EmailNotificationProvider>();
        services.AddScoped<Services.Notifications.INotificationProvider, Services.Notifications.WebhookNotificationProvider>();
        services.AddScoped<Services.Notifications.INotificationDispatcherService, Services.Notifications.NotificationDispatcherService>();

        // SLO Monitoring
        services.AddScoped<ISloEvaluationService, Services.Slo.SloEvaluationService>();

        // AI Observability & Intelligence
        services.Configure<SentinelOps.Contracts.DTOs.AiOptions>(configuration.GetSection("AiSettings"));
        services.AddScoped<IAiUsageMeter, Services.Ai.AiUsageMeter>();
        services.AddScoped<Services.Ai.DisabledAiProvider>();
        services.AddScoped<Services.Ai.DeterministicAiProvider>();
        services.AddHttpClient<Services.Ai.ExternalLlmProvider>();
        services.AddScoped<IAiProviderFactory, Services.Ai.AiProviderFactory>();
        services.AddScoped<IAiProvider>(sp => sp.GetRequiredService<IAiProviderFactory>().GetProvider());
        services.AddScoped<IAnomalyDetectionService, Services.Ai.AnomalyDetectionService>();
        services.AddScoped<ILogPatternAnalysisService, Services.Ai.LogPatternAnalysisService>();
        services.AddScoped<IRootCauseAnalysisService, Services.Ai.RootCauseAnalysisService>();
        services.AddScoped<IIncidentAnalysisService, Services.Ai.IncidentAnalysisService>();
        services.AddScoped<IPredictiveAlertingService, Services.Ai.PredictiveAlertingService>();

        services.AddHostedService<TelemetryConsumerService>();

        return services;
    }
}
