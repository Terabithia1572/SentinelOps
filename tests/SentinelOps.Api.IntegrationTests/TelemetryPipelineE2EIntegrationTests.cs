using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SentinelOps.Application.Common.Interfaces;
using SentinelOps.Contracts.DTOs;
using SentinelOps.Contracts.Otlp;
using SentinelOps.Contracts.Requests;
using SentinelOps.Contracts.Responses;
using SentinelOps.Domain.Constants;
using SentinelOps.Infrastructure.Identity;
using SentinelOps.Infrastructure.Persistence;
using Xunit;

namespace SentinelOps.Api.IntegrationTests;

public class TelemetryPipelineE2EIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory _factory;

    public TelemetryPipelineE2EIntegrationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task TelemetryPipeline_ParentChildSpans_AndCorrelatedLogs_PersistedAndRetrievedCorrectly()
    {
        // 1. Setup Tenant and Application
        var (token, _) = await CreateUserWithAdminRoleAsync("telemetry_e2e_user");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        _client.DefaultRequestHeaders.Remove("X-Organization-Id");

        var orgRes = await _client.PostAsJsonAsync("/api/organizations", new CreateOrganizationRequest
        {
            Name = "Telemetry E2E Org " + Guid.NewGuid().ToString("N")[..6],
            Slug = "telemetry-e2e-" + Guid.NewGuid().ToString("N")[..6]
        });
        orgRes.EnsureSuccessStatusCode();
        var org = (await orgRes.Content.ReadFromJsonAsync<ApiResponse<OrganizationDto>>())!.Data!;

        _client.DefaultRequestHeaders.Add("X-Organization-Id", org.Id.ToString());

        var appRes = await _client.PostAsJsonAsync("/api/applications", new CreateApplicationRequest
        {
            Name = "app-telemetry-e2e-" + Guid.NewGuid().ToString("N")[..6],
            DisplayName = "Telemetry E2E App"
        });
        appRes.EnsureSuccessStatusCode();
        var app = (await appRes.Content.ReadFromJsonAsync<ApiResponse<ApplicationDto>>())!.Data!;

        // 2. Generate API Key
        var keyRes = await _client.PostAsJsonAsync($"/api/applications/{app.Id}/keys", new CreateApiKeyRequest
        {
            Name = "Telemetry E2E Ingestion Key"
        });
        keyRes.EnsureSuccessStatusCode();
        var rawApiKey = (await keyRes.Content.ReadFromJsonAsync<ApiResponse<ApiKeyCreatedResponse>>())!.Data!.RawApiKey;

        // 3. Ingest OTLP Traces with Parent/Child Hierarchy
        var traceId = Guid.NewGuid().ToString("N");
        var rootSpanId = Guid.NewGuid().ToString("N")[..16];
        var childSpanId = Guid.NewGuid().ToString("N")[..16];
        var nowMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        var tracePayload = new OtlpTracesPayload
        {
            ResourceSpans =
            [
                new()
                {
                    Resource = new OtlpResource
                    {
                        Attributes = [new() { Key = "service.name", Value = new() { StringValue = "OrderService" } }]
                    },
                    ScopeSpans =
                    [
                        new()
                        {
                            Spans =
                            [
                                new()
                                {
                                    TraceId = traceId,
                                    SpanId = rootSpanId,
                                    ParentSpanId = null,
                                    Name = "POST /api/orders/checkout",
                                    StartTimeUnixNano = (nowMs * 1_000_000).ToString(),
                                    EndTimeUnixNano = ((nowMs + 120) * 1_000_000).ToString(),
                                    Status = new OtlpStatus { Code = 1 } // Ok
                                },
                                new()
                                {
                                    TraceId = traceId,
                                    SpanId = childSpanId,
                                    ParentSpanId = rootSpanId,
                                    Name = "SELECT * FROM inventory WHERE item_id = @id",
                                    StartTimeUnixNano = ((nowMs + 10) * 1_000_000).ToString(),
                                    EndTimeUnixNano = ((nowMs + 45) * 1_000_000).ToString(),
                                    Status = new OtlpStatus { Code = 1 } // Ok
                                }
                            ]
                        }
                    ]
                }
            ]
        };

        var ingestClient = _factory.CreateClient();
        ingestClient.DefaultRequestHeaders.Add("X-API-Key", rawApiKey);

        var traceIngestRes = await ingestClient.PostAsync("/v1/traces",
            new StringContent(JsonSerializer.Serialize(tracePayload), Encoding.UTF8, "application/json"));
        traceIngestRes.StatusCode.Should().Be(HttpStatusCode.OK);

        // 4. Ingest Correlated Log Entry referencing TraceId and ChildSpanId
        var logPayload = new OtlpLogsPayload
        {
            ResourceLogs =
            [
                new()
                {
                    Resource = new OtlpResource
                    {
                        Attributes = [new() { Key = "service.name", Value = new() { StringValue = "OrderService" } }]
                    },
                    ScopeLogs =
                    [
                        new()
                        {
                            LogRecords =
                            [
                                new()
                                {
                                    TraceId = traceId,
                                    SpanId = childSpanId,
                                    SeverityText = "Information",
                                    Body = new OtlpAnyValue { StringValue = "Inventory check passed for order #9821" }
                                }
                            ]
                        }
                    ]
                }
            ]
        };

        var logIngestRes = await ingestClient.PostAsync("/v1/logs",
            new StringContent(JsonSerializer.Serialize(logPayload), Encoding.UTF8, "application/json"));
        logIngestRes.StatusCode.Should().Be(HttpStatusCode.OK);

        // 5. Wait for consumer flush
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            for (var i = 0; i < 30; i++)
            {
                var traceExists = await db.TraceRecords.AnyAsync(t => t.TraceId == traceId);
                var spanCount = await db.SpanRecords.CountAsync(s => s.TraceId == traceId);
                var logExists = await db.LogEntries.AnyAsync(l => l.TraceId == traceId);
                if (traceExists && spanCount >= 2 && logExists) break;
                await Task.Delay(100);
            }

            var persistedTrace = await db.TraceRecords.FirstOrDefaultAsync(t => t.TraceId == traceId);
            persistedTrace.Should().NotBeNull();
            persistedTrace!.ApplicationId.Should().Be(app.Id);

            var persistedSpans = await db.SpanRecords.Where(s => s.TraceId == traceId).ToListAsync();
            persistedSpans.Should().HaveCount(2);
            persistedSpans.Should().Contain(s => s.SpanId == rootSpanId && s.ParentSpanId == null);
            persistedSpans.Should().Contain(s => s.SpanId == childSpanId && s.ParentSpanId == rootSpanId);

            var persistedLog = await db.LogEntries.FirstOrDefaultAsync(l => l.TraceId == traceId);
            persistedLog.Should().NotBeNull();
            persistedLog!.SpanId.Should().Be(childSpanId);
            persistedLog.Message.Should().Contain("Inventory check passed");
        }

        // 6. Query API for Trace Details and Correlated Logs
        var traceDetailRes = await _client.GetAsync($"/api/traces/{traceId}");
        traceDetailRes.EnsureSuccessStatusCode();
        var traceDetail = (await traceDetailRes.Content.ReadFromJsonAsync<ApiResponse<TraceDetailDto>>())!.Data!;
        traceDetail.Trace.TraceId.Should().Be(traceId);
        traceDetail.Spans.Should().HaveCount(2);
        traceDetail.CorrelatedLogs.Should().ContainSingle(l => l.TraceId == traceId && l.SpanId == childSpanId);

        var logQueryRes = await _client.GetAsync($"/api/logs?applicationId={app.Id}&traceId={traceId}");
        logQueryRes.EnsureSuccessStatusCode();
        var logsDoc = (await logQueryRes.Content.ReadFromJsonAsync<ApiResponse<PagedResponse<LogEntryDto>>>())!.Data!;
        logsDoc.Items.Should().ContainSingle(l => l.TraceId == traceId && l.SpanId == childSpanId);
    }

    [Fact]
    public async Task TelemetryPipeline_RevokedKey_CannotInjectTelemetry()
    {
        var (token, _) = await CreateUserWithAdminRoleAsync("telemetry_revocation");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        _client.DefaultRequestHeaders.Remove("X-Organization-Id");

        var orgRes = await _client.PostAsJsonAsync("/api/organizations", new CreateOrganizationRequest
        {
            Name = "Revocation Org " + Guid.NewGuid().ToString("N")[..6],
            Slug = "revocation-org-" + Guid.NewGuid().ToString("N")[..6]
        });
        orgRes.EnsureSuccessStatusCode();
        var org = (await orgRes.Content.ReadFromJsonAsync<ApiResponse<OrganizationDto>>())!.Data!;

        _client.DefaultRequestHeaders.Add("X-Organization-Id", org.Id.ToString());

        var appRes = await _client.PostAsJsonAsync("/api/applications", new CreateApplicationRequest
        {
            Name = "app-revocation-" + Guid.NewGuid().ToString("N")[..6],
            DisplayName = "Revocation App"
        });
        appRes.EnsureSuccessStatusCode();
        var app = (await appRes.Content.ReadFromJsonAsync<ApiResponse<ApplicationDto>>())!.Data!;

        var keyRes = await _client.PostAsJsonAsync($"/api/applications/{app.Id}/keys", new CreateApiKeyRequest
        {
            Name = "Ephemeral Key"
        });
        keyRes.EnsureSuccessStatusCode();
        var keyCreated = (await keyRes.Content.ReadFromJsonAsync<ApiResponse<ApiKeyCreatedResponse>>())!.Data!;

        // Revoke the key
        var revokeRes = await _client.PostAsync($"/api/keys/{keyCreated.Id}/revoke", null);
        revokeRes.EnsureSuccessStatusCode();

        // Attempt ingestion with revoked key -> Must return 401
        var ingestClient = _factory.CreateClient();
        ingestClient.DefaultRequestHeaders.Add("X-API-Key", keyCreated.RawApiKey);

        var logPayload = new OtlpLogsPayload
        {
            ResourceLogs =
            [
                new()
                {
                    ScopeLogs = [new() { LogRecords = [new() { SeverityText = "Error", Body = new OtlpAnyValue { StringValue = "Should be rejected" } }] }]
                }
            ]
        };

        var postRes = await ingestClient.PostAsync("/v1/logs",
            new StringContent(JsonSerializer.Serialize(logPayload), Encoding.UTF8, "application/json"));
        postRes.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
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
            FullName = $"{prefix} User",
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow
        };

        var result = await userManager.CreateAsync(user, "P@ssword2026!Secure");
        result.Succeeded.Should().BeTrue();

        await userManager.AddToRoleAsync(user, Roles.Admin);

        var (token, _) = await tokenService.GenerateAccessTokenAsync(user.Id, user.UserName, user.Email, [Roles.Admin]);
        return (token, email);
    }
}
