using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using SentinelOps.Application.Common.Interfaces;
using SentinelOps.Contracts.DTOs;
using SentinelOps.Contracts.Requests;
using SentinelOps.Contracts.Responses;
using SentinelOps.Domain.Constants;
using SentinelOps.Domain.Entities;
using SentinelOps.Domain.Enums;
using Xunit;
using ApplicationEntity = SentinelOps.Domain.Entities.Application;

namespace SentinelOps.Api.IntegrationTests;

public class Phase19AdvancedObservabilityTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public Phase19AdvancedObservabilityTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private async Task<(string Token, Guid OrgId, string UserId)> RegisterAndCreateOrgAsync(string prefix = "p19")
    {
        var email = $"{prefix}_{Guid.NewGuid():N}@sentinelops.local";
        var userName = $"{prefix}_{Guid.NewGuid():N}"[..18];
        const string password = "P@ssword2026!Secure";

        string userId;
        using (var scope = _factory.Services.CreateScope())
        {
            var identityService = scope.ServiceProvider.GetRequiredService<IIdentityService>();
            var (_, id, _) = await identityService.CreateUserAsync(userName, email, "Phase 19 User", password, Roles.Admin);
            userId = id;
        }

        var loginRes = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequest
        {
            Email = email,
            Password = password
        });
        loginRes.EnsureSuccessStatusCode();

        var authDoc = await loginRes.Content.ReadFromJsonAsync<ApiResponse<AuthResponse>>();
        var token = authDoc!.Data!.Token;

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var orgRes = await _client.PostAsJsonAsync("/api/organizations", new CreateOrganizationRequest
        {
            Name = $"{prefix} Org {Guid.NewGuid():N}"[..24],
            Slug = $"org-{Guid.NewGuid():N}"[..18],
            Description = "Organization for Phase 19 tests"
        });
        orgRes.EnsureSuccessStatusCode();
        var orgDoc = await orgRes.Content.ReadFromJsonAsync<ApiResponse<OrganizationDto>>();
        var orgId = orgDoc!.Data!.Id;

        return (token, orgId, userId);
    }

    [Fact]
    public async Task SavedLogQuery_Lifecycle_And_Tenant_Isolation()
    {
        // 1. Create Org A and User A
        var (tokenA, orgIdA, _) = await RegisterAndCreateOrgAsync("log_qa");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenA);
        _client.DefaultRequestHeaders.Remove("X-Organization-Id");
        _client.DefaultRequestHeaders.Add("X-Organization-Id", orgIdA.ToString());

        // Create saved query in Org A
        var createReq = new CreateSavedLogQueryRequest
        {
            Name = "Critical Auth Errors",
            Description = "Tracks authentication failures",
            SearchTerm = "Failed login attempt",
            Severity = "Error",
            TimeRange = "24h",
            IsShared = true,
            Tags = ["auth", "security"]
        };

        var postRes = await _client.PostAsJsonAsync("/api/logs/saved", createReq);
        postRes.StatusCode.Should().Be(HttpStatusCode.Created);

        var created = (await postRes.Content.ReadFromJsonAsync<ApiResponse<SavedLogQueryDto>>())!.Data!;
        created.Name.Should().Be("Critical Auth Errors");
        created.SearchTerm.Should().Be("Failed login attempt");
        created.Tags.Should().Contain("auth");

        // List queries in Org A
        var listRes = await _client.GetAsync("/api/logs/saved");
        listRes.StatusCode.Should().Be(HttpStatusCode.OK);
        var listDoc = await listRes.Content.ReadFromJsonAsync<ApiResponse<List<SavedLogQueryDto>>>();
        var list = listDoc!.Data!;
        list.Should().Contain(q => q.Id == created.Id);

        // 2. Create Org B and verify tenant isolation
        var (tokenB, orgIdB, _) = await RegisterAndCreateOrgAsync("log_qb");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenB);
        _client.DefaultRequestHeaders.Remove("X-Organization-Id");
        _client.DefaultRequestHeaders.Add("X-Organization-Id", orgIdB.ToString());

        var crossTenantGet = await _client.GetAsync($"/api/logs/saved/{created.Id}");
        crossTenantGet.StatusCode.Should().Be(HttpStatusCode.NotFound);

        // 3. Update query in Org A
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenA);
        _client.DefaultRequestHeaders.Remove("X-Organization-Id");
        _client.DefaultRequestHeaders.Add("X-Organization-Id", orgIdA.ToString());

        var updateReq = new UpdateSavedLogQueryRequest
        {
            Name = "Critical Auth Failures - Updated",
            Description = "Updated description",
            SearchTerm = "auth_fail",
            Severity = "Critical",
            TimeRange = "7d",
            IsShared = false,
            Tags = ["auth", "updated"]
        };

        var putRes = await _client.PutAsJsonAsync($"/api/logs/saved/{created.Id}", updateReq);
        putRes.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = (await putRes.Content.ReadFromJsonAsync<ApiResponse<SavedLogQueryDto>>())!.Data!;
        updated.Name.Should().Be("Critical Auth Failures - Updated");
        updated.TimeRange.Should().Be("7d");

        // 4. Delete query
        var delRes = await _client.DeleteAsync($"/api/logs/saved/{created.Id}");
        delRes.StatusCode.Should().Be(HttpStatusCode.OK);

        var verifyDel = await _client.GetAsync($"/api/logs/saved/{created.Id}");
        verifyDel.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task LogExport_CsvAndJson_Bounded()
    {
        var (token, orgId, _) = await RegisterAndCreateOrgAsync("export_test");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        _client.DefaultRequestHeaders.Remove("X-Organization-Id");
        _client.DefaultRequestHeaders.Add("X-Organization-Id", orgId.ToString());

        // Test CSV Export
        var csvRes = await _client.GetAsync("/api/logs/export?format=csv&timeRange=24h&maxRecords=100");
        csvRes.StatusCode.Should().Be(HttpStatusCode.OK);
        csvRes.Content.Headers.ContentType!.MediaType.Should().Be("text/csv");
        var csvText = await csvRes.Content.ReadAsStringAsync();
        csvText.Should().Contain("Timestamp");

        // Test JSON Export
        var jsonRes = await _client.GetAsync("/api/logs/export?format=json&timeRange=24h&maxRecords=100");
        jsonRes.StatusCode.Should().Be(HttpStatusCode.OK);
        jsonRes.Content.Headers.ContentType!.MediaType.Should().Be("application/json");
    }

    [Fact]
    public async Task Dashboard_SetDefault_Transitions()
    {
        var (token, orgId, _) = await RegisterAndCreateOrgAsync("def_dash");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        _client.DefaultRequestHeaders.Remove("X-Organization-Id");
        _client.DefaultRequestHeaders.Add("X-Organization-Id", orgId.ToString());

        // Create Dashboard A
        var postResA = await _client.PostAsJsonAsync("/api/dashboards", new CreateDashboardRequest
        {
            Title = "Dashboard Alpha",
            LayoutJson = "[]"
        });
        postResA.EnsureSuccessStatusCode();
        var dashA = (await postResA.Content.ReadFromJsonAsync<ApiResponse<DashboardDetailDto>>())!.Data!;

        // Create Dashboard B
        var postResB = await _client.PostAsJsonAsync("/api/dashboards", new CreateDashboardRequest
        {
            Title = "Dashboard Beta",
            LayoutJson = "[]"
        });
        postResB.EnsureSuccessStatusCode();
        var dashB = (await postResB.Content.ReadFromJsonAsync<ApiResponse<DashboardDetailDto>>())!.Data!;

        // Set A as Default
        var setDefaultARes = await _client.PutAsync($"/api/dashboards/{dashA.Id}/default", null);
        setDefaultARes.StatusCode.Should().Be(HttpStatusCode.OK);
        var resA = (await setDefaultARes.Content.ReadFromJsonAsync<ApiResponse<DashboardDetailDto>>())!.Data!;
        resA.IsDefault.Should().BeTrue();

        // Set B as Default (transitions default from A to B)
        var setDefaultBRes = await _client.PutAsync($"/api/dashboards/{dashB.Id}/default", null);
        setDefaultBRes.StatusCode.Should().Be(HttpStatusCode.OK);
        var resB = (await setDefaultBRes.Content.ReadFromJsonAsync<ApiResponse<DashboardDetailDto>>())!.Data!;
        resB.IsDefault.Should().BeTrue();

        // Verify A is no longer default
        var getDashA = await _client.GetAsync($"/api/dashboards/{dashA.Id}");
        var freshA = (await getDashA.Content.ReadFromJsonAsync<ApiResponse<DashboardDetailDto>>())!.Data!;
        freshA.IsDefault.Should().BeFalse();
    }

    [Fact]
    public async Task NotificationChannel_SsrfProtection_RejectsLoopbackAndMetadata()
    {
        var (token, orgId, _) = await RegisterAndCreateOrgAsync("ssrf_test");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        _client.DefaultRequestHeaders.Remove("X-Organization-Id");
        _client.DefaultRequestHeaders.Add("X-Organization-Id", orgId.ToString());

        // 1. Loopback IP attempt
        var loopbackReq = new CreateNotificationChannelRequest
        {
            Name = "Local Loopback Webhook",
            Type = "Webhook",
            Destination = "http://127.0.0.1:8080/internal/admin"
        };
        var loopbackRes = await _client.PostAsJsonAsync("/api/notification-channels", loopbackReq);
        loopbackRes.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var loopbackBody = await loopbackRes.Content.ReadAsStringAsync();
        loopbackBody.Should().Contain("Invalid webhook destination URL");

        // 2. Cloud metadata IP attempt (169.254.169.254)
        var metadataReq = new CreateNotificationChannelRequest
        {
            Name = "Cloud Metadata Webhook",
            Type = "Webhook",
            Destination = "http://169.254.169.254/latest/meta-data"
        };
        var metadataRes = await _client.PostAsJsonAsync("/api/notification-channels", metadataReq);
        metadataRes.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var metadataBody = await metadataRes.Content.ReadAsStringAsync();
        metadataBody.Should().Contain("Invalid webhook destination URL");

        // 3. Localhost hostname attempt
        var localhostReq = new CreateNotificationChannelRequest
        {
            Name = "Localhost Slack Webhook",
            Type = "Slack",
            Destination = "http://localhost:5000/api/hook"
        };
        var localhostRes = await _client.PostAsJsonAsync("/api/notification-channels", localhostReq);
        localhostRes.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task SlosController_TenantIsolation_EnforcesOwnership()
    {
        var (tokenA, orgIdA, _) = await RegisterAndCreateOrgAsync("slo_iso_a");
        var (tokenB, orgIdB, _) = await RegisterAndCreateOrgAsync("slo_iso_b");

        // Seed an application and SLO for Org A
        Guid sloId;
        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
            var appA = new ApplicationEntity
            {
                Id = Guid.NewGuid(),
                OrganizationId = orgIdA,
                Name = "slo-app-a",
                DisplayName = "SLO App A",
                CreatedAtUtc = DateTime.UtcNow
            };
            context.Applications.Add(appA);

            var sloA = new ServiceLevelObjective
            {
                Id = Guid.NewGuid(),
                ApplicationId = appA.Id,
                ServiceName = "orders-service",
                Name = "Orders Service 99.9% Availability",
                TargetPercentage = 99.9,
                EvaluationWindow = SloEvaluationWindow.Rolling30Days,
                IndicatorType = SloIndicatorType.Availability,
                CreatedAtUtc = DateTime.UtcNow
            };
            context.ServiceLevelObjectives.Add(sloA);
            await context.SaveChangesAsync(CancellationToken.None);
            sloId = sloA.Id;
        }

        // Access with Org A (Owner) -> OK
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenA);
        _client.DefaultRequestHeaders.Remove("X-Organization-Id");
        _client.DefaultRequestHeaders.Add("X-Organization-Id", orgIdA.ToString());

        var getResA = await _client.GetAsync($"/api/slos/{sloId}");
        getResA.StatusCode.Should().Be(HttpStatusCode.OK);

        // Access with Org B (Cross-Tenant Intruder) -> 404 Not Found
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenB);
        _client.DefaultRequestHeaders.Remove("X-Organization-Id");
        _client.DefaultRequestHeaders.Add("X-Organization-Id", orgIdB.ToString());

        var getResB = await _client.GetAsync($"/api/slos/{sloId}");
        getResB.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Usage_And_Quotas_ReturnsCurrentConsumption()
    {
        var (token, orgId, _) = await RegisterAndCreateOrgAsync("usage_test");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        _client.DefaultRequestHeaders.Remove("X-Organization-Id");
        _client.DefaultRequestHeaders.Add("X-Organization-Id", orgId.ToString());

        var usageRes = await _client.GetAsync("/api/organizations/current/usage");
        usageRes.StatusCode.Should().Be(HttpStatusCode.OK);

        var doc = await usageRes.Content.ReadFromJsonAsync<ApiResponse<OrganizationUsageDto>>();
        doc!.Success.Should().BeTrue();
        doc.Data.Should().NotBeNull();
        doc.Data!.LogsIngestedMonthToDate.Should().BeGreaterThanOrEqualTo(0);
        doc.Data.TracesIngestedMonthToDate.Should().BeGreaterThanOrEqualTo(0);
        doc.Data.MetricsIngestedMonthToDate.Should().BeGreaterThanOrEqualTo(0);
    }
}
