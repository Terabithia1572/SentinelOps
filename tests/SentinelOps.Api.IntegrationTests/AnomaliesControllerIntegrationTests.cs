using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using SentinelOps.Contracts.DTOs;
using SentinelOps.Contracts.Requests;
using SentinelOps.Contracts.Responses;
using SentinelOps.Domain.Entities;
using SentinelOps.Domain.Enums;
using SentinelOps.Infrastructure.Persistence;
using Xunit;

namespace SentinelOps.Api.IntegrationTests;

public class AnomaliesControllerIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory _factory;

    public AnomaliesControllerIntegrationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private async Task<(string Token, Guid OrgId, Guid AppId)> SetupTenantAsync(string prefix = "anom")
    {
        var email = $"{prefix}_{Guid.NewGuid():N}@sentinelops.local";
        var password = "P@ssword2026!Secure";

        var registerRes = await _client.PostAsJsonAsync("/api/auth/register", new RegisterRequest
        {
            UserName = $"{prefix}_{Guid.NewGuid():N}"[..20],
            Email = email,
            FullName = "Anomaly Test User",
            Password = password,
            ConfirmPassword = password
        });
        registerRes.EnsureSuccessStatusCode();

        var regDoc = await registerRes.Content.ReadFromJsonAsync<ApiResponse<AuthResponse>>();
        var token = regDoc!.Data!.Token;

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var orgRes = await _client.PostAsJsonAsync("/api/organizations", new CreateOrganizationRequest
        {
            Name = $"{prefix} Org {Guid.NewGuid():N}"[..25],
            Slug = $"org-{Guid.NewGuid():N}"[..20],
            Description = "Organization for anomaly tests"
        });
        orgRes.EnsureSuccessStatusCode();
        var orgDoc = await orgRes.Content.ReadFromJsonAsync<ApiResponse<OrganizationDto>>();
        var orgId = orgDoc!.Data!.Id;

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var app = new Domain.Entities.Application
        {
            Name = $"{prefix}-app",
            DisplayName = $"{prefix} App",
            OrganizationId = orgId,
            ApiKeyHash = "hash"
        };
        db.Applications.Add(app);
        await db.SaveChangesAsync();

        return (token, orgId, app.Id);
    }

    [Fact]
    public async Task Evaluate_Endpoint_ReturnsValidAnomalyEvaluation()
    {
        var (token, orgId, appId) = await SetupTenantAsync("eval");

        var request = new AnomalyEvaluationRequest
        {
            OrganizationId = orgId,
            ApplicationId = appId,
            ServiceName = "checkout",
            SignalType = "ErrorRate",
            CurrentValue = 45.0,
            HistoricalValues = [1.0, 1.2, 0.9, 1.1, 1.0, 0.8, 1.3, 1.0, 1.1, 1.0]
        };

        var response = await _client.PostAsJsonAsync("/api/anomalies/evaluate", request);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<ApiResponse<AnomalyDetectionResult>>();
        body.Should().NotBeNull();
        body!.Success.Should().BeTrue();
        body.Data!.IsAnomaly.Should().BeTrue();
        body.Data.Severity.Should().Be("Critical");
    }

    [Fact]
    public async Task GetActive_And_Resolve_Endpoints_WorkAcrossTenantLifecycle()
    {
        var (token, orgId, appId) = await SetupTenantAsync("active");

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var anomaly = new TelemetryAnomaly(
                orgId,
                appId,
                "payments",
                "Latency",
                50.0,
                350.0,
                6.0,
                0.95,
                Severity.Critical,
                "RobustMedianMAD",
                "Critical latency spike");
            db.TelemetryAnomalies.Add(anomaly);
            await db.SaveChangesAsync();
        }

        // 1. Fetch active anomalies
        var listRes = await _client.GetAsync($"/api/anomalies?applicationId={appId}");
        listRes.StatusCode.Should().Be(HttpStatusCode.OK);

        var listBody = await listRes.Content.ReadFromJsonAsync<ApiResponse<List<TelemetryAnomalyDto>>>();
        listBody.Should().NotBeNull();
        listBody!.Data.Should().NotBeEmpty();
        var target = listBody.Data!.First();
        target.ServiceName.Should().Be("payments");

        // 2. Resolve the anomaly
        var resolveRes = await _client.PostAsync($"/api/anomalies/{target.Id}/resolve", null);
        resolveRes.StatusCode.Should().Be(HttpStatusCode.OK);

        // 3. Verify no longer in active list
        var updatedListRes = await _client.GetAsync($"/api/anomalies?applicationId={appId}");
        var updatedListBody = await updatedListRes.Content.ReadFromJsonAsync<ApiResponse<List<TelemetryAnomalyDto>>>();
        updatedListBody!.Data!.Should().NotContain(a => a.Id == target.Id);
    }
}
