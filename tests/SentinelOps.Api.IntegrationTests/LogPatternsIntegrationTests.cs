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

public class LogPatternsIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory _factory;

    public LogPatternsIntegrationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private async Task<(string Token, Guid OrgId, Guid AppId)> SetupTenantAndLogsAsync(string prefix = "lp")
    {
        var email = $"{prefix}_{Guid.NewGuid():N}@sentinelops.local";
        var password = "P@ssword2026!Secure";

        var registerRes = await _client.PostAsJsonAsync("/api/auth/register", new RegisterRequest
        {
            UserName = $"{prefix}_{Guid.NewGuid():N}"[..20],
            Email = email,
            FullName = "Log Pattern Test User",
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
            Description = "Organization for log pattern tests"
        });
        orgRes.EnsureSuccessStatusCode();
        var orgDoc = await orgRes.Content.ReadFromJsonAsync<ApiResponse<OrganizationDto>>();
        var orgId = orgDoc!.Data!.Id;
        _client.DefaultRequestHeaders.TryAddWithoutValidation("X-Organization-Id", orgId.ToString());

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var app = new Domain.Entities.Application
        {
            Name = $"{prefix}-service",
            DisplayName = $"{prefix} Service",
            OrganizationId = orgId,
            ApiKeyHash = "hash"
        };
        db.Applications.Add(app);

        var now = DateTime.UtcNow;
        var logs = new List<LogEntry>
        {
            new() { ApplicationId = app.Id, ServiceName = "cart", EnvironmentName = "prod", Severity = Severity.Error, Message = "Redis connection timed out after 500ms for cart 101", Timestamp = now.AddMinutes(-10), TraceId = "tr-lp-1" },
            new() { ApplicationId = app.Id, ServiceName = "cart", EnvironmentName = "prod", Severity = Severity.Error, Message = "Redis connection timed out after 750ms for cart 202", Timestamp = now.AddMinutes(-5), TraceId = "tr-lp-2" },
            new() { ApplicationId = app.Id, ServiceName = "cart", EnvironmentName = "prod", Severity = Severity.Information, Message = "User 999 viewed product page https://shop.local/item/55", Timestamp = now.AddMinutes(-3), TraceId = "tr-lp-3" }
        };
        db.LogEntries.AddRange(logs);
        await db.SaveChangesAsync();

        return (token, orgId, app.Id);
    }

    [Fact]
    public async Task AnalyzePatterns_And_GetPatterns_ReturnClusteredPatterns()
    {
        var (token, orgId, appId) = await SetupTenantAndLogsAsync("clust");

        // 1. Trigger log pattern clustering analysis
        var analyzeRes = await _client.PostAsync($"/api/logs/patterns/analyze?applicationId={appId}&windowHours=24", null);
        analyzeRes.StatusCode.Should().Be(HttpStatusCode.OK);

        var analyzeBody = await analyzeRes.Content.ReadFromJsonAsync<ApiResponse<LogPatternAnalysisResult>>();
        analyzeBody.Should().NotBeNull();
        analyzeBody!.Success.Should().BeTrue();
        analyzeBody.Data!.TotalLogsAnalyzed.Should().Be(3);
        analyzeBody.Data.PatternsFound.Should().Be(2);

        var redisTimeoutPattern = analyzeBody.Data.Patterns.FirstOrDefault(p => p.Template.Contains("Redis connection timed out after {duration} for cart {number}"));
        redisTimeoutPattern.Should().NotBeNull();
        redisTimeoutPattern!.TotalOccurrences.Should().Be(2);
        redisTimeoutPattern.Severity.Should().Be("Error");

        // 2. Query persisted patterns via GET /api/logs/patterns
        var getRes = await _client.GetAsync($"/api/logs/patterns?applicationId={appId}");
        getRes.StatusCode.Should().Be(HttpStatusCode.OK);

        var getBody = await getRes.Content.ReadFromJsonAsync<ApiResponse<List<LogPatternDto>>>();
        getBody.Should().NotBeNull();
        getBody!.Data.Should().NotBeEmpty();
        getBody.Data.Should().Contain(p => p.PatternFingerprint == redisTimeoutPattern.PatternFingerprint);
    }
}
