using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SentinelOps.Application.Common.Interfaces;
using SentinelOps.Contracts.DTOs;
using SentinelOps.Domain.Entities;
using SentinelOps.Domain.Enums;
using SentinelOps.Infrastructure.Identity;
using ApplicationEntity = SentinelOps.Domain.Entities.Application;
using Environment = SentinelOps.Domain.Entities.Environment;

namespace SentinelOps.Infrastructure.Services;

public class OnboardingService : IOnboardingService
{
    private readonly IApplicationDbContext _context;
    private readonly IApiKeyService _apiKeyService;
    private readonly IAuditService _auditService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<OnboardingService> _logger;

    public OnboardingService(
        IApplicationDbContext context,
        IApiKeyService apiKeyService,
        IAuditService auditService,
        UserManager<ApplicationUser> userManager,
        ILogger<OnboardingService> logger)
    {
        _context = context;
        _apiKeyService = apiKeyService;
        _auditService = auditService;
        _userManager = userManager;
        _logger = logger;
    }

    public async Task<OnboardingStatusDto> GetStatusAsync(string userId, CancellationToken ct = default)
    {
        var membership = await _context.OrganizationMembers
            .AsNoTracking()
            .Include(m => m.Organization)
            .ThenInclude(o => o.Applications)
            .ThenInclude(a => a.ApiKeys)
            .Include(m => m.Organization.Subscription)
            .ThenInclude(s => s!.Plan)
            .OrderByDescending(m => m.JoinedAtUtc)
            .FirstOrDefaultAsync(m => m.UserId == userId && m.Organization.IsActive, ct);

        if (membership == null)
        {
            return new OnboardingStatusDto
            {
                IsCompleted = false,
                CurrentStep = "Organization"
            };
        }

        var org = membership.Organization;
        var app = org.Applications.FirstOrDefault(a => a.IsActive);

        if (app == null)
        {
            return new OnboardingStatusDto
            {
                IsCompleted = false,
                CurrentStep = "Application",
                OrganizationId = org.Id,
                OrganizationName = org.Name,
                OrganizationSlug = org.Slug,
                PlanTier = org.Subscription?.Plan?.Tier ?? org.Settings?.PlanTier ?? "Starter"
            };
        }

        var apiKey = app.ApiKeys.FirstOrDefault(k => k.RevokedAtUtc == null);
        if (apiKey == null)
        {
            return new OnboardingStatusDto
            {
                IsCompleted = false,
                CurrentStep = "ApiKey",
                OrganizationId = org.Id,
                OrganizationName = org.Name,
                OrganizationSlug = org.Slug,
                PlanTier = org.Subscription?.Plan?.Tier ?? org.Settings?.PlanTier ?? "Starter",
                ApplicationId = app.Id,
                ApplicationName = app.Name,
                HasApiKey = false
            };
        }

        // Check if any telemetry has arrived for this application
        var hasLogs = await _context.LogEntries.AnyAsync(l => l.ApplicationId == app.Id, ct);
        var hasTraces = await _context.TraceRecords.AnyAsync(t => t.ApplicationId == app.Id, ct);
        var hasMetrics = await _context.MetricRecords.AnyAsync(m => m.ApplicationId == app.Id, ct);
        var hasReceived = hasLogs || hasTraces || hasMetrics;

        DateTime? firstTelemetry = null;
        if (hasReceived)
        {
            var firstLog = await _context.LogEntries
                .Where(l => l.ApplicationId == app.Id)
                .OrderBy(l => l.Timestamp)
                .Select(l => (DateTime?)l.Timestamp)
                .FirstOrDefaultAsync(ct);

            firstTelemetry = firstLog ?? DateTime.UtcNow;
        }

        return new OnboardingStatusDto
        {
            IsCompleted = hasReceived,
            CurrentStep = hasReceived ? "Completed" : "TelemetryVerification",
            OrganizationId = org.Id,
            OrganizationName = org.Name,
            OrganizationSlug = org.Slug,
            PlanTier = org.Subscription?.Plan?.Tier ?? org.Settings?.PlanTier ?? "Starter",
            ApplicationId = app.Id,
            ApplicationName = app.Name,
            HasApiKey = true,
            ApiKeyPrefix = apiKey.KeyPrefix,
            HasReceivedTelemetry = hasReceived,
            FirstTelemetryReceivedAtUtc = firstTelemetry
        };
    }

    public async Task<(bool Success, OnboardingProvisionedResponse? Response, string? Error)> ProvisionTenantAndAppAsync(
        OnboardingProvisionRequest request,
        string userId,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.OrganizationName))
            return (false, null, "Organization name is required.");

        if (string.IsNullOrWhiteSpace(request.ApplicationName))
            return (false, null, "Application name is required.");

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            return (false, null, "User account not found.");

        // Compute unique slug
        var baseSlug = (string.IsNullOrWhiteSpace(request.OrganizationSlug) ? request.OrganizationName : request.OrganizationSlug)
            .Trim().ToLowerInvariant().Replace(' ', '-');

        var slug = baseSlug;
        var slugExists = await _context.Organizations.AnyAsync(o => o.Slug == slug, ct);
        if (slugExists)
        {
            slug = $"{baseSlug}-{Guid.NewGuid().ToString("N")[..6]}";
        }

        // Retrieve requested subscription plan (default to Starter if not found)
        var targetTier = string.IsNullOrWhiteSpace(request.PlanTier) ? "Starter" : request.PlanTier.Trim();
        var plan = await _context.SubscriptionPlans
            .FirstOrDefaultAsync(p => p.Tier.ToLower() == targetTier.ToLower() && p.IsActive, ct)
            ?? await _context.SubscriptionPlans.FirstOrDefaultAsync(p => p.Tier == "Starter", ct);

        if (plan == null)
        {
            // Fallback plan if DB seed not yet applied in test context
            plan = new SubscriptionPlan
            {
                Tier = targetTier,
                Name = $"{targetTier} Plan",
                Description = $"Default {targetTier} Plan",
                MaxApplications = targetTier.Equals("Enterprise", StringComparison.OrdinalIgnoreCase) ? 100 : (targetTier.Equals("Pro", StringComparison.OrdinalIgnoreCase) ? 20 : 3),
                MaxMembers = targetTier.Equals("Enterprise", StringComparison.OrdinalIgnoreCase) ? 100 : (targetTier.Equals("Pro", StringComparison.OrdinalIgnoreCase) ? 25 : 3),
                MaxCustomDashboards = targetTier.Equals("Enterprise", StringComparison.OrdinalIgnoreCase) ? 50 : (targetTier.Equals("Pro", StringComparison.OrdinalIgnoreCase) ? 15 : 3),
                MaxMonthlyLogs = targetTier.Equals("Enterprise", StringComparison.OrdinalIgnoreCase) ? 10_000_000 : (targetTier.Equals("Pro", StringComparison.OrdinalIgnoreCase) ? 1_000_000 : 100_000),
                MaxMonthlyTraces = targetTier.Equals("Enterprise", StringComparison.OrdinalIgnoreCase) ? 5_000_000 : (targetTier.Equals("Pro", StringComparison.OrdinalIgnoreCase) ? 500_000 : 50_000),
                MaxMonthlyMetrics = targetTier.Equals("Enterprise", StringComparison.OrdinalIgnoreCase) ? 10_000_000 : (targetTier.Equals("Pro", StringComparison.OrdinalIgnoreCase) ? 1_000_000 : 100_000),
                RetentionDays = targetTier.Equals("Enterprise", StringComparison.OrdinalIgnoreCase) ? 90 : (targetTier.Equals("Pro", StringComparison.OrdinalIgnoreCase) ? 30 : 7),
                MaxMonthlyAiTokens = targetTier.Equals("Enterprise", StringComparison.OrdinalIgnoreCase) ? 1_000_000 : (targetTier.Equals("Pro", StringComparison.OrdinalIgnoreCase) ? 100_000 : 10_000),
                AllowCustomReports = !targetTier.Equals("Starter", StringComparison.OrdinalIgnoreCase),
                AllowSloAlerts = !targetTier.Equals("Starter", StringComparison.OrdinalIgnoreCase),
                AllowApiAccess = true
            };
            _context.SubscriptionPlans.Add(plan);
            await _context.SaveChangesAsync(ct);
        }

        // 1. Create Organization
        var org = new Organization
        {
            Name = request.OrganizationName.Trim(),
            Slug = slug,
            Description = $"Provisioned for {user.Email}",
            IsActive = true,
            Settings = new OrganizationSettings
            {
                PlanTier = plan.Tier,
                MaxApplications = plan.MaxApplications,
                MaxMembers = plan.MaxMembers,
                MaxCustomDashboards = plan.MaxCustomDashboards,
                MaxMonthlyLogs = plan.MaxMonthlyLogs,
                MaxMonthlyTraces = plan.MaxMonthlyTraces,
                DefaultRetentionDays = plan.RetentionDays,
                MaxMonthlyAiTokens = plan.MaxMonthlyAiTokens,
                IsAiEnabled = true,
                PreferredAiProvider = "Deterministic"
            },
            Usage = new OrganizationUsage
            {
                LastResetAtUtc = DateTime.UtcNow
            }
        };

        // 2. Add Organization Member (Owner)
        var member = new OrganizationMember
        {
            OrganizationId = org.Id,
            UserId = userId,
            Role = OrganizationRole.Owner,
            JoinedAtUtc = DateTime.UtcNow
        };

        // 3. Create Billing Account
        var billingAccount = new BillingAccount
        {
            OrganizationId = org.Id,
            BillingEmail = user.Email ?? "billing@example.com",
            CompanyName = request.OrganizationName.Trim(),
            Currency = "USD"
        };

        // 4. Create Organization Subscription
        var subscription = new OrganizationSubscription
        {
            OrganizationId = org.Id,
            PlanId = plan.Id,
            Status = SubscriptionStatus.Trialing,
            BillingInterval = BillingInterval.Monthly,
            CurrentPeriodStartUtc = DateTime.UtcNow,
            CurrentPeriodEndUtc = DateTime.UtcNow.AddMonths(1),
            TrialEndsAtUtc = DateTime.UtcNow.AddDays(14)
        };

        // 5. Create Initial Application & Environment
        var appName = request.ApplicationName.Trim().ToLowerInvariant().Replace(' ', '-');
        var application = new ApplicationEntity
        {
            OrganizationId = org.Id,
            Name = appName,
            DisplayName = string.IsNullOrWhiteSpace(request.ApplicationDisplayName) ? request.ApplicationName.Trim() : request.ApplicationDisplayName.Trim(),
            Description = "Default application created during self-service onboarding.",
            IsActive = true,
            ApiKeyHash = Guid.NewGuid().ToString("N")
        };

        var envName = string.IsNullOrWhiteSpace(request.EnvironmentName) ? "Production" : request.EnvironmentName.Trim();
        var environment = new Environment
        {
            ApplicationId = application.Id,
            Name = envName,
            EnvironmentType = EnvironmentType.Production,
            IsActive = true
        };

        // 6. Generate Telemetry API Key
        var (rawApiKey, keyPrefix, keyHash) = _apiKeyService.GenerateApiKey("live");
        var apiKey = new ApiKey
        {
            ApplicationId = application.Id,
            Name = "Initial Ingestion Key",
            KeyPrefix = keyPrefix,
            KeyHash = keyHash,
            CreatedBy = user.UserName ?? user.Email ?? userId
        };

        _context.Organizations.Add(org);
        _context.OrganizationMembers.Add(member);
        _context.BillingAccounts.Add(billingAccount);
        _context.OrganizationSubscriptions.Add(subscription);
        _context.Applications.Add(application);
        _context.Environments.Add(environment);
        _context.ApiKeys.Add(apiKey);

        await _context.SaveChangesAsync(ct);

        await _auditService.RecordAsync(
            action: "Onboarding.Provisioned",
            targetEntityType: "Organization",
            targetEntityId: org.Id.ToString(),
            outcome: "Success",
            details: $"User {user.Email} onboarded organization '{org.Name}' ({org.Slug}) with initial app '{application.Name}'.",
            cancellationToken: ct
        );

        // Generate SDK code snippets
        var snippets = GenerateSnippets(application.Name, rawApiKey);

        var response = new OnboardingProvisionedResponse
        {
            OrganizationId = org.Id,
            OrganizationName = org.Name,
            OrganizationSlug = org.Slug,
            ApplicationId = application.Id,
            ApplicationName = application.Name,
            ApiKeyId = apiKey.Id,
            RawApiKey = rawApiKey, // Provided once to operator
            KeyPrefix = keyPrefix,
            PlanTier = plan.Tier,
            SubscriptionStatus = subscription.Status.ToString(),
            TrialEndsAtUtc = subscription.TrialEndsAtUtc,
            SdkSnippetCSharp = snippets.CSharp,
            SdkSnippetNodeJs = snippets.NodeJs,
            SdkSnippetPython = snippets.Python,
            OtelCollectorYaml = snippets.CollectorYaml
        };

        return (true, response, null);
    }

    public async Task<FirstTelemetryVerificationResponse> VerifyFirstTelemetryAsync(
        Guid applicationId,
        string userId,
        CancellationToken ct = default)
    {
        var app = await _context.Applications
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == applicationId, ct);

        if (app == null)
        {
            return new FirstTelemetryVerificationResponse
            {
                ApplicationId = applicationId,
                HasReceivedTelemetry = false
            };
        }

        // Validate user belongs to the application's organization
        if (app.OrganizationId.HasValue)
        {
            var isMember = await _context.OrganizationMembers
                .AnyAsync(m => m.OrganizationId == app.OrganizationId.Value && m.UserId == userId, ct);

            if (!isMember)
            {
                return new FirstTelemetryVerificationResponse
                {
                    ApplicationId = applicationId,
                    HasReceivedTelemetry = false
                };
            }
        }

        var logsCount = await _context.LogEntries.CountAsync(l => l.ApplicationId == applicationId, ct);
        var tracesCount = await _context.TraceRecords.CountAsync(t => t.ApplicationId == applicationId, ct);
        var metricsCount = await _context.MetricRecords.CountAsync(m => m.ApplicationId == applicationId, ct);

        var hasReceived = (logsCount + tracesCount + metricsCount) > 0;

        DateTime? firstReceived = null;
        DateTime? lastReceived = null;

        if (hasReceived)
        {
            firstReceived = await _context.LogEntries
                .Where(l => l.ApplicationId == applicationId)
                .OrderBy(l => l.Timestamp)
                .Select(l => (DateTime?)l.Timestamp)
                .FirstOrDefaultAsync(ct);

            lastReceived = await _context.LogEntries
                .Where(l => l.ApplicationId == applicationId)
                .OrderByDescending(l => l.Timestamp)
                .Select(l => (DateTime?)l.Timestamp)
                .FirstOrDefaultAsync(ct) ?? DateTime.UtcNow;
        }

        return new FirstTelemetryVerificationResponse
        {
            ApplicationId = applicationId,
            HasReceivedTelemetry = hasReceived,
            LogsCount = logsCount,
            TracesCount = tracesCount,
            MetricsCount = metricsCount,
            FirstReceivedAtUtc = firstReceived,
            LastReceivedAtUtc = lastReceived
        };
    }

    public async Task<(bool Success, string? Error)> CompleteOnboardingAsync(string userId, CancellationToken ct = default)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            return (false, "User not found.");

        await _auditService.RecordAsync(
            action: "Onboarding.Completed",
            targetEntityType: "User",
            targetEntityId: userId,
            outcome: "Success",
            details: $"User {user.Email} successfully completed the self-service onboarding workflow.",
            cancellationToken: ct
        );

        return (true, null);
    }

    private static (string CSharp, string NodeJs, string Python, string CollectorYaml) GenerateSnippets(string serviceName, string apiKey)
    {
        var csharp = $$"""
// In Program.cs (ASP.NET Core / .NET 10)
builder.Services.AddOpenTelemetry()
    .WithTracing(tracerProviderBuilder => {
        tracerProviderBuilder
            .AddSource("{{serviceName}}")
            .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("{{serviceName}}"))
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddOtlpExporter(opt => {
                opt.Endpoint = new Uri("http://localhost:5100/v1/traces");
                opt.Headers = "X-SentinelOps-Key={{apiKey}}";
                opt.Protocol = OtlpExportProtocol.HttpProtobuf;
            });
    })
    .WithMetrics(meterProviderBuilder => {
        meterProviderBuilder
            .AddMeter("{{serviceName}}")
            .AddAspNetCoreInstrumentation()
            .AddOtlpExporter(opt => {
                opt.Endpoint = new Uri("http://localhost:5100/v1/metrics");
                opt.Headers = "X-SentinelOps-Key={{apiKey}}";
                opt.Protocol = OtlpExportProtocol.HttpProtobuf;
            });
    });
""";

        var nodejs = $$"""
// telemetry.js (Node.js)
const { NodeSDK } = require('@opentelemetry/sdk-node');
const { OTLPTraceExporter } = require('@opentelemetry/exporter-trace-otlp-proto');
const { Resource } = require('@opentelemetry/resources');
const { SemanticResourceAttributes } = require('@opentelemetry/semantic-conventions');

const sdk = new NodeSDK({
  resource: new Resource({
    [SemanticResourceAttributes.SERVICE_NAME]: '{{serviceName}}',
  }),
  traceExporter: new OTLPTraceExporter({
    url: 'http://localhost:5100/v1/traces',
    headers: { 'X-SentinelOps-Key': '{{apiKey}}' },
  }),
});

sdk.start();
""";

        var python = $$"""
# telemetry.py (Python)
from opentelemetry import trace
from opentelemetry.sdk.trace import TracerProvider
from opentelemetry.sdk.trace.export import BatchSpanProcessor
from opentelemetry.exporter.otlp.proto.http.trace_exporter import OTLPSpanExporter
from opentelemetry.sdk.resources import Resource

resource = Resource.create({"service.name": "{{serviceName}}"})
provider = TracerProvider(resource=resource)
processor = BatchSpanProcessor(
    OTLPSpanExporter(
        endpoint="http://localhost:5100/v1/traces",
        headers={"X-SentinelOps-Key": "{{apiKey}}"}
    )
)
provider.add_span_processor(processor)
trace.set_tracer_provider(provider)
""";

        var collectorYaml = $$"""
receivers:
  otlp:
    protocols:
      grpc:
        endpoint: 0.0.0.0:4317
      http:
        endpoint: 0.0.0.0:4318

processors:
  batch:
    timeout: 1s

exporters:
  otlphttp/sentinelops:
    endpoint: "http://sentinelops-api:5100"
    headers:
      "X-SentinelOps-Key": "{{apiKey}}"

service:
  pipelines:
    traces:
      receivers: [otlp]
      processors: [batch]
      exporters: [otlphttp/sentinelops]
    logs:
      receivers: [otlp]
      processors: [batch]
      exporters: [otlphttp/sentinelops]
    metrics:
      receivers: [otlp]
      processors: [batch]
      exporters: [otlphttp/sentinelops]
""";

        return (csharp, nodejs, python, collectorYaml);
    }
}
