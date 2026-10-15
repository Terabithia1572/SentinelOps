using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;
using SentinelOps.Api.Middleware;
using SentinelOps.Application;
using SentinelOps.Domain.Constants;
using SentinelOps.Infrastructure;
using Microsoft.AspNetCore.HttpOverrides;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true);
builder.Configuration.AddEnvironmentVariables();

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateLogger();

builder.Host.UseSerilog();

// Layer Dependency Injection
builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration);

// Add Controllers
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowReadingFromString;
    });
builder.Services.Configure<Microsoft.AspNetCore.Mvc.ApiBehaviorOptions>(options =>
{
    var defaultFactory = options.InvalidModelStateResponseFactory;
    options.InvalidModelStateResponseFactory = context =>
    {
        var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
        var errors = string.Join("; ", context.ModelState
            .Where(e => e.Value?.Errors.Count > 0)
            .Select(e => $"{e.Key}: {string.Join(", ", e.Value!.Errors.Select(x => string.IsNullOrWhiteSpace(x.ErrorMessage) ? x.Exception?.Message : x.ErrorMessage))}"));
        logger.LogWarning("Model validation failed on {Path}: {Errors}", context.HttpContext.Request.Path, errors);
        return defaultFactory(context);
    };
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddScoped<SentinelOps.Api.Authentication.ApiKeyAuthFilter>();
builder.Services.AddRequestDecompression();

// Swagger/OpenAPI configuration
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "SentinelOps Observability API",
        Version = "v1",
        Description = "Enterprise Observability & Telemetry Platform API"
    });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityDefinition("ApiKey", new OpenApiSecurityScheme
    {
        Description = "API Key header for telemetry ingestion. Example: \"X-API-Key: sent_live_...\"",
        Name = "X-API-Key",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        },
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "ApiKey"
                }
            },
            Array.Empty<string>()
        }
    });
});

// JWT Authentication
var jwtKey = builder.Configuration["JwtSettings:Key"] ?? "PLACEHOLDER_DEV_SECRET_KEY_AT_LEAST_32_CHARS_LONG";
if (builder.Environment.IsProduction())
{
    if (string.IsNullOrWhiteSpace(jwtKey) || jwtKey.Contains("PLACEHOLDER") || jwtKey.Length < 32)
    {
        throw new InvalidOperationException("CRITICAL SECURITY VIOLATION: Production requires a secure, non-placeholder JwtSettings:Key with at least 32 characters.");
    }
}
var jwtIssuer = builder.Configuration["JwtSettings:Issuer"] ?? "SentinelOps";
var jwtAudience = builder.Configuration["JwtSettings:Audience"] ?? "SentinelOpsUsers";

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtIssuer,
        ValidAudience = jwtAudience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
        ClockSkew = TimeSpan.Zero
    };
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var accessToken = context.Request.Query["access_token"];
            var path = context.HttpContext.Request.Path;
            if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
            {
                context.Token = accessToken;
            }
            return Task.CompletedTask;
        },
        OnTokenValidated = async context =>
        {
            var userManager = context.HttpContext.RequestServices.GetRequiredService<Microsoft.AspNetCore.Identity.UserManager<SentinelOps.Infrastructure.Identity.ApplicationUser>>();
            var userId = context.Principal?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                ?? context.Principal?.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)?.Value;
            if (!string.IsNullOrEmpty(userId))
            {
                var user = await userManager.FindByIdAsync(userId);
                if (user == null || !user.IsActive)
                {
                    context.Fail("User is inactive or deleted.");
                    return;
                }
                if (await userManager.IsLockedOutAsync(user))
                {
                    context.Fail("User account is locked.");
                    return;
                }
            }
        }
    };
});

var signalRBuilder = builder.Services.AddSignalR();
var useRedisBackplane = builder.Configuration.GetValue<bool>("SignalR:UseRedisBackplane", false);
var redisConnectionString = builder.Configuration.GetConnectionString("Redis");
if (useRedisBackplane && !string.IsNullOrWhiteSpace(redisConnectionString))
{
    signalRBuilder.AddStackExchangeRedis(redisConnectionString, options =>
    {
        options.Configuration.ChannelPrefix = StackExchange.Redis.RedisChannel.Literal("sentinelops:signalr");
        options.Configuration.AbortOnConnectFail = false;
    });
}
builder.Services.AddScoped<SentinelOps.Application.Common.Interfaces.IRealtimeNotifier, SentinelOps.Api.Services.RealtimeNotifier>();

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("RequireAdmin", policy => policy.RequireRole(Roles.Admin));
    options.AddPolicy("RequireOperator", policy => policy.RequireRole(Roles.Admin, Roles.Operator));
    options.AddPolicy("RequireViewer", policy => policy.RequireRole(Roles.Admin, Roles.Operator, Roles.Viewer));
});

// CORS configuration
var allowedOrigins = builder.Configuration.GetSection("CorsSettings:AllowedOrigins").Get<string[]>()
    ?? ["http://localhost:3000", "http://localhost:5173"];

builder.Services.AddCors(options =>
{
    options.AddPolicy("DefaultCorsPolicy", policy =>
    {
        policy.WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

// Rate Limiting
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddFixedWindowLimiter("auth-rate-limit", opt =>
    {
        opt.PermitLimit = 60;
        opt.Window = TimeSpan.FromMinutes(1);
        opt.QueueLimit = 0;
    });
});

// Health Checks
var npgsqlConn = builder.Configuration.GetConnectionString("DefaultConnection");
var redisConn = builder.Configuration.GetConnectionString("Redis");

var healthCheckBuilder = builder.Services.AddHealthChecks();
if (!string.IsNullOrEmpty(npgsqlConn))
{
    healthCheckBuilder.AddNpgSql(npgsqlConn, name: "postgresql", tags: ["ready"]);
}
if (!string.IsNullOrEmpty(redisConn))
{
    healthCheckBuilder.AddRedis(redisConn, name: "redis", tags: ["ready"]);
}
healthCheckBuilder.AddCheck<SentinelOps.Infrastructure.Services.TelemetryBufferHealthCheck>("telemetry_buffer", tags: ["ready"]);

// Forwarded headers for reverse proxy & Kubernetes Ingress
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});

var app = builder.Build();

// Reverse proxy forwarded headers (must be first in pipeline)
app.UseForwardedHeaders();

// Middleware pipeline
app.UseMiddleware<SecurityHeadersMiddleware>();
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseMiddleware<RequestLoggingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "SentinelOps API v1"));
}

app.UseHttpsRedirection();
app.UseCors("DefaultCorsPolicy");
app.UseRequestDecompression();
app.UseRateLimiter();

app.UseAuthentication();
app.UseMiddleware<SentinelOps.Api.Middleware.TenantResolutionMiddleware>();
app.UseAuthorization();

// Health check endpoints
app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = _ => false,
    ResultStatusCodes =
    {
        [HealthStatus.Healthy] = StatusCodes.Status200OK,
        [HealthStatus.Degraded] = StatusCodes.Status200OK,
        [HealthStatus.Unhealthy] = StatusCodes.Status503ServiceUnavailable
    }
});

app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready"),
    ResultStatusCodes =
    {
        [HealthStatus.Healthy] = StatusCodes.Status200OK,
        [HealthStatus.Degraded] = StatusCodes.Status200OK,
        [HealthStatus.Unhealthy] = StatusCodes.Status503ServiceUnavailable
    }
});

app.MapControllers();
app.MapHub<SentinelOps.Api.Hubs.AlertsHub>("/hubs/alerts");

// Apply pending migrations and seed Identity Roles on startup
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetService<SentinelOps.Infrastructure.Persistence.ApplicationDbContext>();
    if (dbContext != null)
    {
        if (dbContext.Database.ProviderName != "Microsoft.EntityFrameworkCore.InMemory" && dbContext.Database.IsRelational())
        {
            var isMigrateOnly = args.Contains("--apply-migrations") || args.Contains("--migrate-only") || args.Contains("--migrate-database-only");
            var autoMigrate = app.Environment.IsDevelopment() || builder.Configuration.GetValue<bool>("MigrationSettings:AutoMigrateOnStartup", false);

            if (isMigrateOnly)
            {
                Log.Information("Applying pending EF Core migrations (--migrate-only mode)...");
                await dbContext.Database.MigrateAsync();
                Log.Information("All pending EF Core migrations applied successfully. Exiting.");
                return;
            }

            if (autoMigrate)
            {
                Log.Information("Applying EF Core migrations in Development mode...");
                await dbContext.Database.MigrateAsync();
            }
            else
            {
                var pendingMigrations = (await dbContext.Database.GetPendingMigrationsAsync()).ToList();
                if (pendingMigrations.Count > 0)
                {
                    Log.Fatal("CRITICAL DATABASE COMPATIBILITY ERROR: Detected {Count} unapplied EF Core migrations ({Migrations}) in {Environment} environment. Uncontrolled startup migrations are disabled. Migrations must be executed explicitly via CI/CD migration job or 'dotnet SentinelOps.Api.dll --apply-migrations' prior to starting API pods.",
                        pendingMigrations.Count, string.Join(", ", pendingMigrations), app.Environment.EnvironmentName);
                    throw new InvalidOperationException($"Database schema is not up to date in {app.Environment.EnvironmentName}. Please apply pending migrations ({string.Join(", ", pendingMigrations)}) before starting the application.");
                }
            }
        }
        else
        {
            await dbContext.Database.EnsureCreatedAsync();
        }
    }

    var roleManager = scope.ServiceProvider.GetService<RoleManager<IdentityRole>>();
    if (roleManager != null)
    {
        foreach (var role in Roles.All)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }
    }

    // Initialize Default Organization and reconcile legacy applications
    if (dbContext != null)
    {
        var defaultOrg = await dbContext.Organizations.FirstOrDefaultAsync(o => o.Slug == "default-org");
        if (defaultOrg == null)
        {
            defaultOrg = new SentinelOps.Domain.Entities.Organization
            {
                Name = "Default Organization",
                Slug = "default-org",
                Description = "Default tenant organization for legacy applications.",
                IsActive = true,
                CreatedAtUtc = DateTime.UtcNow,
                Settings = new SentinelOps.Domain.Entities.OrganizationSettings
                {
                    AllowMemberInvites = true,
                    RequireTwoFactor = false,
                    DefaultRetentionDays = 30
                },
                Usage = new SentinelOps.Domain.Entities.OrganizationUsage
                {
                    LastResetAtUtc = DateTime.UtcNow
                }
            };
            dbContext.Organizations.Add(defaultOrg);
            await dbContext.SaveChangesAsync();
        }

        var autoReconcile = builder.Configuration.GetValue<bool>("MigrationSettings:AutoReconcileLegacyTenants", false);
        if (autoReconcile)
        {
            var unassignedApps = await dbContext.Applications.Where(a => a.OrganizationId == null).ToListAsync();
            if (unassignedApps.Count > 0)
            {
                foreach (var appItem in unassignedApps)
                {
                    appItem.OrganizationId = defaultOrg.Id;
                }
                await dbContext.SaveChangesAsync();
            }

            var userManager = scope.ServiceProvider.GetService<UserManager<SentinelOps.Infrastructure.Identity.ApplicationUser>>();
            if (userManager != null)
            {
                var existingUsers = userManager.Users.Where(u => u.IsActive).ToList();
                foreach (var user in existingUsers)
                {
                    var isMember = await dbContext.OrganizationMembers
                        .AnyAsync(m => m.OrganizationId == defaultOrg.Id && m.UserId == user.Id);
                    if (!isMember)
                    {
                        var isUserAdmin = await userManager.IsInRoleAsync(user, Roles.Admin);
                        dbContext.OrganizationMembers.Add(new SentinelOps.Domain.Entities.OrganizationMember
                        {
                            OrganizationId = defaultOrg.Id,
                            UserId = user.Id,
                            Role = isUserAdmin ? SentinelOps.Domain.Enums.OrganizationRole.Owner : SentinelOps.Domain.Enums.OrganizationRole.Member,
                            JoinedAtUtc = DateTime.UtcNow
                        });
                    }
                }
                await dbContext.SaveChangesAsync();
            }
        }
    }
}

app.Run();

public partial class Program
{
}
