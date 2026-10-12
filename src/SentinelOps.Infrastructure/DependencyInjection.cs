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
                b.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName)));

        services.AddScoped<IApplicationDbContext>(sp =>
            sp.GetRequiredService<ApplicationDbContext>());

        services.AddIdentityCore<ApplicationUser>(options =>
        {
            options.Password.RequireDigit = true;
            options.Password.RequireLowercase = true;
            options.Password.RequireUppercase = true;
            options.Password.RequireNonAlphanumeric = true;
            options.Password.RequiredLength = 8;
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
        services.AddScoped<IIdentityService, IdentityService>();
        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<IApiKeyService, ApiKeyService>();

        services.AddHostedService<TelemetryConsumerService>();

        return services;
    }
}
