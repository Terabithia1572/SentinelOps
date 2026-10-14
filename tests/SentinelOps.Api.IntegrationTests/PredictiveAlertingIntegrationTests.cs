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

public class PredictiveAlertingIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory _factory;

    public PredictiveAlertingIntegrationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private async Task<(string Token, Guid OrgId, Guid AppId)> SetupTenantAsync(string prefix = "fore")
    {
        var email = $"{prefix}_{Guid.NewGuid():N}@sentinelops.local";
        var password = "P@ssword2026!Secure";

        var registerRes = await _client.PostAsJsonAsync("/api/auth/register", new RegisterRequest
        {
            UserName = $"{prefix}_{Guid.NewGuid():N}"[..20],
            Email = email,
            FullName = "Forecast Test User",
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
            Description = "Organization for forecast tests"
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
    public async Task Evaluate_Endpoint_ComputesAndReturnsForecast()
    {
        var (token, orgId, appId) = await SetupTenantAsync("fc-eval");

        // Seed metric data points
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var now = DateTime.UtcNow;
            for (int i = 0; i < 8; i++)
            {
                db.MetricRecords.Add(new MetricRecord
                {
                    ApplicationId = appId,
                    ServiceName = "database-pool",
                    EnvironmentName = "Production",
                    MetricName = "connection_count",
                    MetricType = MetricType.Gauge,
                    Value = 20.0 + (i * 10.0), // 20, 30, 40, 50, 60, 70, 80, 90
                    Tags = "{}",
                    Timestamp = now.AddMinutes(-35 + (i * 5))
                });
            }
            await db.SaveChangesAsync();
        }

        var request = new CapacityForecastRequest
        {
            OrganizationId = orgId,
            ApplicationId = appId,
            ServiceName = "database-pool",
            MetricName = "connection_count",
            HorizonMinutes = 30,
            AlertThreshold = 120.0
        };

        var response = await _client.PostAsJsonAsync("/api/forecasts/evaluate", request);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<ApiResponse<CapacityForecastResult>>();
        body.Should().NotBeNull();
        body!.Success.Should().BeTrue();
        body.Data!.SampleCount.Should().Be(8);
        body.Data.CalculationMethod.Should().Be("LinearRegressionOrdinaryLeastSquares");
        body.Data.CurrentValue.Should().Be(90.0);
        body.Data.TrendSlope.Should().BeGreaterThan(0);
        body.Data.WillBreachThreshold.Should().BeTrue();
        body.Data.ProjectedBreachTimeUtc.Should().NotBeNull();
    }

    [Fact]
    public async Task GetActive_Forecasts_ReturnsPersistedForecasts()
    {
        var (token, orgId, appId) = await SetupTenantAsync("fc-get");

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            db.PredictiveForecasts.Add(new PredictiveForecast(
                orgId,
                appId,
                "web-tier",
                "memory_usage_mb",
                512.0,
                768.0,
                2.5,
                60,
                DateTime.UtcNow.AddMinutes(45),
                700.0,
                836.0,
                "LinearRegressionOrdinaryLeastSquares",
                12,
                DateTime.UtcNow.AddMinutes(60)));
            await db.SaveChangesAsync();
        }

        var response = await _client.GetAsync($"/api/forecasts?applicationId={appId}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<ApiResponse<List<PredictiveForecastDto>>>();
        body.Should().NotBeNull();
        body!.Success.Should().BeTrue();
        body.Data.Should().NotBeEmpty();
        var forecast = body.Data!.First();
        forecast.ServiceName.Should().Be("web-tier");
        forecast.MetricName.Should().Be("memory_usage_mb");
        forecast.OrganizationId.Should().Be(orgId);
    }
}
