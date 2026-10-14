using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using SentinelOps.Application.Common.Interfaces;
using SentinelOps.Contracts.DTOs;
using SentinelOps.Contracts.Otlp;
using SentinelOps.Contracts.Requests;
using SentinelOps.Contracts.Responses;
using SentinelOps.Domain.Entities;
using SentinelOps.Infrastructure.Persistence;
using Xunit;

using Microsoft.AspNetCore.Identity;
using SentinelOps.Domain.Constants;
using SentinelOps.Infrastructure.Identity;

namespace SentinelOps.Api.IntegrationTests;

public class MultiTenantTelemetryPipelineIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public MultiTenantTelemetryPipelineIntegrationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private async Task<(string token, string email)> CreateUserWithAdminRoleAsync(string prefix)
    {
        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var tokenService = scope.ServiceProvider.GetRequiredService<ITokenService>();

        if (!await roleManager.RoleExistsAsync(Roles.Admin))
        {
            await roleManager.CreateAsync(new IdentityRole(Roles.Admin));
        }

        var unique = Guid.NewGuid().ToString("N")[..8];
        var email = $"{prefix}_{unique}@sentinelops.local";
        var user = new ApplicationUser
        {
            UserName = $"user_{prefix}_{unique}",
            Email = email,
            FullName = $"{prefix} Admin User",
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow
        };

        var result = await userManager.CreateAsync(user, "P@ssword2026!Secure");
        result.Succeeded.Should().BeTrue();

        await userManager.AddToRoleAsync(user, Roles.Admin);

        var (token, _) = await tokenService.GenerateAccessTokenAsync(user.Id, user.UserName, user.Email, [Roles.Admin]);
        return (token, email);
    }

    [Fact]
    public async Task MultiTenantTelemetry_OrganizationAlphaMustNeverAccessOrganizationBetaTelemetry()
    {
        // 1. Create User Alpha and Organization Alpha
        var (tokenAlpha, _) = await CreateUserWithAdminRoleAsync("owner_alpha");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenAlpha);
        _client.DefaultRequestHeaders.Remove("X-Organization-Id");

        var orgAlphaRes = await _client.PostAsJsonAsync("/api/organizations", new CreateOrganizationRequest
        {
            Name = "Tenant Alpha Corp",
            Slug = "tenant-alpha-" + Guid.NewGuid().ToString("N")[..6]
        });
        orgAlphaRes.EnsureSuccessStatusCode();
        var orgAlpha = (await orgAlphaRes.Content.ReadFromJsonAsync<ApiResponse<OrganizationDto>>())!.Data!;

        // Create Application Alpha in Org Alpha
        _client.DefaultRequestHeaders.Remove("X-Organization-Id");
        _client.DefaultRequestHeaders.Add("X-Organization-Id", orgAlpha.Id.ToString());
        var appAlphaRes = await _client.PostAsJsonAsync("/api/applications", new CreateApplicationRequest
        {
            Name = "app-alpha-" + Guid.NewGuid().ToString("N")[..6],
            DisplayName = "App Alpha Services",
            Description = "Core telemetry producer for Tenant Alpha"
        });
        appAlphaRes.EnsureSuccessStatusCode();
        var appAlpha = (await appAlphaRes.Content.ReadFromJsonAsync<ApiResponse<ApplicationDto>>())!.Data!;

        // Create API Key for App Alpha
        var keyAlphaRes = await _client.PostAsJsonAsync($"/api/applications/{appAlpha.Id}/keys", new CreateApiKeyRequest
        {
            Name = "Key Alpha Production"
        });
        keyAlphaRes.EnsureSuccessStatusCode();
        var keyAlphaDoc = await keyAlphaRes.Content.ReadFromJsonAsync<ApiResponse<ApiKeyCreatedResponse>>();
        var rawKeyAlpha = keyAlphaDoc!.Data!.RawApiKey;

        // 2. Create User Beta and Organization Beta
        var (tokenBeta, _) = await CreateUserWithAdminRoleAsync("owner_beta");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenBeta);
        _client.DefaultRequestHeaders.Remove("X-Organization-Id");

        var orgBetaRes = await _client.PostAsJsonAsync("/api/organizations", new CreateOrganizationRequest
        {
            Name = "Tenant Beta Corp",
            Slug = "tenant-beta-" + Guid.NewGuid().ToString("N")[..6]
        });
        orgBetaRes.EnsureSuccessStatusCode();
        var orgBeta = (await orgBetaRes.Content.ReadFromJsonAsync<ApiResponse<OrganizationDto>>())!.Data!;

        // Create Application Beta in Org Beta
        _client.DefaultRequestHeaders.Remove("X-Organization-Id");
        _client.DefaultRequestHeaders.Add("X-Organization-Id", orgBeta.Id.ToString());
        var appBetaRes = await _client.PostAsJsonAsync("/api/applications", new CreateApplicationRequest
        {
            Name = "app-beta-" + Guid.NewGuid().ToString("N")[..6],
            DisplayName = "App Beta Services",
            Description = "Core telemetry producer for Tenant Beta"
        });
        appBetaRes.EnsureSuccessStatusCode();
        var appBeta = (await appBetaRes.Content.ReadFromJsonAsync<ApiResponse<ApplicationDto>>())!.Data!;

        // Create API Key for App Beta
        var keyBetaRes = await _client.PostAsJsonAsync($"/api/applications/{appBeta.Id}/keys", new CreateApiKeyRequest
        {
            Name = "Key Beta Production"
        });
        keyBetaRes.EnsureSuccessStatusCode();
        var keyBetaDoc = await keyBetaRes.Content.ReadFromJsonAsync<ApiResponse<ApiKeyCreatedResponse>>();
        var rawKeyBeta = keyBetaDoc!.Data!.RawApiKey;

        // 3. Ingest unique logs into App Alpha using Key Alpha
        var clientIngest = _factory.CreateClient();
        clientIngest.DefaultRequestHeaders.Add("X-API-Key", rawKeyAlpha);
        var alphaLogPayload = new OtlpLogsPayload
        {
            ResourceLogs =
            [
                new()
                {
                    Resource = new OtlpResource
                    {
                        Attributes = [new() { Key = "service.name", Value = new() { StringValue = "Service-Alpha" } }]
                    },
                    ScopeLogs =
                    [
                        new()
                        {
                            LogRecords =
                            [
                                new()
                                {
                                    SeverityText = "Information",
                                    Body = new OtlpAnyValue { StringValue = "Confidential Alpha Telemetry Record #99" }
                                }
                            ]
                        }
                    ]
                }
            ]
        };
        var logAlphaRes = await clientIngest.PostAsync("/v1/logs",
            new StringContent(JsonSerializer.Serialize(alphaLogPayload), Encoding.UTF8, "application/json"));
        logAlphaRes.StatusCode.Should().Be(HttpStatusCode.OK);

        // 4. Ingest unique logs into App Beta using Key Beta
        clientIngest.DefaultRequestHeaders.Remove("X-API-Key");
        clientIngest.DefaultRequestHeaders.Add("X-API-Key", rawKeyBeta);
        var betaLogPayload = new OtlpLogsPayload
        {
            ResourceLogs =
            [
                new()
                {
                    Resource = new OtlpResource
                    {
                        Attributes = [new() { Key = "service.name", Value = new() { StringValue = "Service-Beta" } }]
                    },
                    ScopeLogs =
                    [
                        new()
                        {
                            LogRecords =
                            [
                                new()
                                {
                                    SeverityText = "Critical",
                                    Body = new OtlpAnyValue { StringValue = "Confidential Beta Financial Payload #77" }
                                }
                            ]
                        }
                    ]
                }
            ]
        };
        var logBetaRes = await clientIngest.PostAsync("/v1/logs",
            new StringContent(JsonSerializer.Serialize(betaLogPayload), Encoding.UTF8, "application/json"));
        logBetaRes.StatusCode.Should().Be(HttpStatusCode.OK);

        // Allow background worker buffer to flush into database
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        for (var i = 0; i < 30; i++)
        {
            await Task.Delay(100);
            if (await db.LogEntries.AnyAsync(l => l.Message.Contains("Confidential Beta Financial Payload #77")))
                break;
        }

        // 5. Query Logs Explorer as Tenant Alpha
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenAlpha);
        _client.DefaultRequestHeaders.Remove("X-Organization-Id");
        _client.DefaultRequestHeaders.Add("X-Organization-Id", orgAlpha.Id.ToString());

        var queryAlphaRes = await _client.GetAsync($"/api/logs?applicationId={appAlpha.Id}");
        queryAlphaRes.EnsureSuccessStatusCode();
        var logsAlphaDoc = await queryAlphaRes.Content.ReadFromJsonAsync<ApiResponse<PagedResponse<LogEntryDto>>>();

        logsAlphaDoc!.Success.Should().BeTrue();
        logsAlphaDoc.Data!.Items.Should().Contain(l => l.Message.Contains("Confidential Alpha Telemetry Record #99"));
        // Tenant Alpha must NEVER see Tenant Beta logs
        logsAlphaDoc.Data.Items.Should().NotContain(l => l.Message.Contains("Confidential Beta Financial Payload #77"));

        // 6. Tenant Alpha attempts to query App Beta's logs directly by ID -> must NOT leak data
        var crossQueryRes = await _client.GetAsync($"/api/logs?applicationId={appBeta.Id}");
        // Controller filters by tenant ownership or returns empty items
        if (crossQueryRes.IsSuccessStatusCode)
        {
            var crossDoc = await crossQueryRes.Content.ReadFromJsonAsync<ApiResponse<PagedResponse<LogEntryDto>>>();
            crossDoc!.Data!.Items.Should().BeEmpty();
        }
        else
        {
            crossQueryRes.StatusCode.Should().BeOneOf(HttpStatusCode.Forbidden, HttpStatusCode.NotFound);
        }

        // 7. Tenant Alpha attempts to tamper X-Organization-Id header to Org Beta
        _client.DefaultRequestHeaders.Remove("X-Organization-Id");
        _client.DefaultRequestHeaders.Add("X-Organization-Id", orgBeta.Id.ToString());

        var tamperedOrgRes = await _client.GetAsync($"/api/organizations/{orgBeta.Id}");
        // Since User Alpha is NOT a member of Org Beta, request must be rejected
        tamperedOrgRes.StatusCode.Should().BeOneOf(HttpStatusCode.Forbidden, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Ingestion_WithRevokedKey_IsRejected_AndNotBuffered()
    {
        var (token, _) = await CreateUserWithAdminRoleAsync("owner_revocation");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        _client.DefaultRequestHeaders.Remove("X-Organization-Id");

        var orgRes = await _client.PostAsJsonAsync("/api/organizations", new CreateOrganizationRequest
        {
            Name = "Revocation Test Org",
            Slug = "revocation-org-" + Guid.NewGuid().ToString("N")[..6]
        });
        orgRes.EnsureSuccessStatusCode();
        var org = (await orgRes.Content.ReadFromJsonAsync<ApiResponse<OrganizationDto>>())!.Data!;

        _client.DefaultRequestHeaders.Add("X-Organization-Id", org.Id.ToString());
        var appRes = await _client.PostAsJsonAsync("/api/applications", new CreateApplicationRequest
        {
            Name = "revocation-app-" + Guid.NewGuid().ToString("N")[..6],
            DisplayName = "Revocation App"
        });
        appRes.EnsureSuccessStatusCode();
        var app = (await appRes.Content.ReadFromJsonAsync<ApiResponse<ApplicationDto>>())!.Data!;

        var keyRes = await _client.PostAsJsonAsync($"/api/applications/{app.Id}/keys", new CreateApiKeyRequest
        {
            Name = "Ephemeral Key"
        });
        keyRes.EnsureSuccessStatusCode();
        var keyDoc = await keyRes.Content.ReadFromJsonAsync<ApiResponse<ApiKeyCreatedResponse>>();
        var rawKey = keyDoc!.Data!.RawApiKey;
        var keyId = keyDoc.Data.Id;

        // Revoke the key immediately
        var revokeRes = await _client.PostAsync($"/api/keys/{keyId}/revoke", null);
        revokeRes.StatusCode.Should().Be(HttpStatusCode.OK);

        // Attempt ingestion with revoked key
        var ingestClient = _factory.CreateClient();
        ingestClient.DefaultRequestHeaders.Add("X-API-Key", rawKey);
        var payload = new OtlpLogsPayload();
        var res = await ingestClient.PostAsync("/v1/logs",
            new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json"));

        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task MultiTenantPipeline_TracesMetricsAndDashboard_MustIsolateTenants()
    {
        // 1. Setup Tenant Alpha
        var (tokenAlpha, _) = await CreateUserWithAdminRoleAsync("dash_alpha");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenAlpha);
        _client.DefaultRequestHeaders.Remove("X-Organization-Id");

        var orgAlphaRes = await _client.PostAsJsonAsync("/api/organizations", new CreateOrganizationRequest
        {
            Name = "Metrics Alpha Org",
            Slug = "metrics-alpha-" + Guid.NewGuid().ToString("N")[..6]
        });
        orgAlphaRes.EnsureSuccessStatusCode();
        var orgAlpha = (await orgAlphaRes.Content.ReadFromJsonAsync<ApiResponse<OrganizationDto>>())!.Data!;

        _client.DefaultRequestHeaders.Add("X-Organization-Id", orgAlpha.Id.ToString());
        var appAlphaRes = await _client.PostAsJsonAsync("/api/applications", new CreateApplicationRequest
        {
            Name = "app-alpha-metrics-" + Guid.NewGuid().ToString("N")[..6],
            DisplayName = "Alpha Metrics Producer"
        });
        appAlphaRes.EnsureSuccessStatusCode();
        var appAlpha = (await appAlphaRes.Content.ReadFromJsonAsync<ApiResponse<ApplicationDto>>())!.Data!;

        // 2. Setup Tenant Beta
        var (tokenBeta, _) = await CreateUserWithAdminRoleAsync("dash_beta");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenBeta);
        _client.DefaultRequestHeaders.Remove("X-Organization-Id");

        var orgBetaRes = await _client.PostAsJsonAsync("/api/organizations", new CreateOrganizationRequest
        {
            Name = "Metrics Beta Org",
            Slug = "metrics-beta-" + Guid.NewGuid().ToString("N")[..6]
        });
        orgBetaRes.EnsureSuccessStatusCode();
        var orgBeta = (await orgBetaRes.Content.ReadFromJsonAsync<ApiResponse<OrganizationDto>>())!.Data!;

        _client.DefaultRequestHeaders.Add("X-Organization-Id", orgBeta.Id.ToString());
        var appBetaRes = await _client.PostAsJsonAsync("/api/applications", new CreateApplicationRequest
        {
            Name = "app-beta-metrics-" + Guid.NewGuid().ToString("N")[..6],
            DisplayName = "Beta Metrics Producer"
        });
        appBetaRes.EnsureSuccessStatusCode();
        var appBeta = (await appBetaRes.Content.ReadFromJsonAsync<ApiResponse<ApplicationDto>>())!.Data!;

        // Create API key for Beta
        var keyBetaRes = await _client.PostAsJsonAsync($"/api/applications/{appBeta.Id}/keys", new CreateApiKeyRequest
        {
            Name = "Beta Telemetry Key"
        });
        keyBetaRes.EnsureSuccessStatusCode();
        var rawKeyBeta = (await keyBetaRes.Content.ReadFromJsonAsync<ApiResponse<ApiKeyCreatedResponse>>())!.Data!.RawApiKey;

        // 3. Ingest unique traces into Beta
        var uniqueBetaTraceId = "4bf92f3577b34da6a3ce929d0e0e4736";
        var ingestClient = _factory.CreateClient();
        ingestClient.DefaultRequestHeaders.Add("X-API-Key", rawKeyBeta);

        var tracePayload = new OtlpTracesPayload
        {
            ResourceSpans =
            [
                new()
                {
                    Resource = new OtlpResource
                    {
                        Attributes = [new() { Key = "service.name", Value = new() { StringValue = "Beta-Internal-Service" } }]
                    },
                    ScopeSpans =
                    [
                        new()
                        {
                            Spans =
                            [
                                new()
                                {
                                    TraceId = uniqueBetaTraceId,
                                    SpanId = "00f067aa0ba902b7",
                                    Name = "GET /internal/secret-ledger",
                                    StartTimeUnixNano = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString() + "000000",
                                    EndTimeUnixNano = (DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + 150).ToString() + "000000",
                                    Status = new OtlpStatus { Code = 2 } // Error
                                }
                            ]
                        }
                    ]
                }
            ]
        };

        var traceIngestRes = await ingestClient.PostAsync("/v1/traces",
            new StringContent(JsonSerializer.Serialize(tracePayload), Encoding.UTF8, "application/json"));
        traceIngestRes.StatusCode.Should().Be(HttpStatusCode.OK);

        // Wait for buffer flush
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            for (var i = 0; i < 30; i++)
            {
                await Task.Delay(100);
                if (await db.TraceRecords.AnyAsync(t => t.TraceId == uniqueBetaTraceId))
                    break;
            }
        }

        // 4. Authenticate as Tenant Alpha and verify cross-tenant boundaries
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenAlpha);
        _client.DefaultRequestHeaders.Remove("X-Organization-Id");
        _client.DefaultRequestHeaders.Add("X-Organization-Id", orgAlpha.Id.ToString());

        // A. Traces Explorer: List queries must not contain Beta traces
        var alphaTracesRes = await _client.GetAsync($"/api/traces?applicationId={appAlpha.Id}");
        alphaTracesRes.EnsureSuccessStatusCode();
        var alphaTraces = (await alphaTracesRes.Content.ReadFromJsonAsync<ApiResponse<PagedResponse<TraceDto>>>())!.Data!;
        alphaTraces.Items.Should().NotContain(t => t.TraceId == uniqueBetaTraceId);

        // B. Cross-tenant App ID query must return empty list
        var crossTracesRes = await _client.GetAsync($"/api/traces?applicationId={appBeta.Id}");
        crossTracesRes.EnsureSuccessStatusCode();
        var crossTraces = (await crossTracesRes.Content.ReadFromJsonAsync<ApiResponse<PagedResponse<TraceDto>>>())!.Data!;
        crossTraces.Items.Should().BeEmpty();

        // C. Direct trace lookup by TraceId of Beta trace must return 404
        var directTraceRes = await _client.GetAsync($"/api/traces/{uniqueBetaTraceId}");
        directTraceRes.StatusCode.Should().Be(HttpStatusCode.NotFound);

        // D. Metrics TimeSeries cross-tenant query
        var crossMetricsRes = await _client.GetAsync($"/api/metrics/timeseries?applicationId={appBeta.Id}");
        crossMetricsRes.EnsureSuccessStatusCode();
        var crossMetrics = (await crossMetricsRes.Content.ReadFromJsonAsync<ApiResponse<MetricSeriesResponse>>())!.Data!;
        crossMetrics.TotalRequests.Should().Be(0);
        crossMetrics.Points.Should().BeEmpty();

        // E. Exceptions cross-tenant query
        var crossExceptionsRes = await _client.GetAsync($"/api/exceptions?applicationId={appBeta.Id}");
        crossExceptionsRes.EnsureSuccessStatusCode();
        var crossExceptions = (await crossExceptionsRes.Content.ReadFromJsonAsync<ApiResponse<IReadOnlyList<ExceptionGroupDto>>>())!.Data!;
        crossExceptions.Should().BeEmpty();

        // F. Dashboard summary: Must reflect only Alpha's 1 application, not Beta's
        var dashRes = await _client.GetAsync("/api/dashboard/summary");
        dashRes.EnsureSuccessStatusCode();
        var dashData = (await dashRes.Content.ReadFromJsonAsync<ApiResponse<DashboardSummaryDto>>())!.Data!;
        dashData.TotalApplications.Should().Be(1);
    }
}
