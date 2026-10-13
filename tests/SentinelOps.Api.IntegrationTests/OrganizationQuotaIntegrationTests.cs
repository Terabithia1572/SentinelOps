using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using SentinelOps.Contracts.DTOs;
using SentinelOps.Contracts.Requests;
using SentinelOps.Contracts.Responses;
using Xunit;

namespace SentinelOps.Api.IntegrationTests;

public class OrganizationQuotaIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public OrganizationQuotaIntegrationTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    private async Task<(string Token, Guid OrgId)> SeedAuthAndOrgAsync(string prefix = "quota")
    {
        var email = $"{prefix}_{Guid.NewGuid():N}@sentinelops.local";
        var password = "P@ssword2026!Secure";

        var registerRes = await _client.PostAsJsonAsync("/api/auth/register", new RegisterRequest
        {
            UserName = $"{prefix}_{Guid.NewGuid():N}"[..20],
            Email = email,
            FullName = "Quota Test User",
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
            Description = "Organization for quota tracking tests"
        });
        orgRes.EnsureSuccessStatusCode();
        var orgDoc = await orgRes.Content.ReadFromJsonAsync<ApiResponse<OrganizationDto>>();
        var orgId = orgDoc!.Data!.Id;

        _client.DefaultRequestHeaders.Remove("X-Organization-Id");
        _client.DefaultRequestHeaders.Add("X-Organization-Id", orgId.ToString());

        return (token, orgId);
    }

    [Fact]
    public async Task GetQuotaStatus_ReturnsValidUsageAndQuotas()
    {
        var (_, orgId) = await SeedAuthAndOrgAsync("status");

        var res = await _client.GetAsync($"/api/organizations/{orgId}/quota");
        res.StatusCode.Should().Be(HttpStatusCode.OK);

        var doc = await res.Content.ReadFromJsonAsync<ApiResponse<OrganizationQuotaStatusDto>>();
        doc.Should().NotBeNull();
        doc!.Success.Should().BeTrue();
        var quota = doc.Data!;
        quota.OrganizationId.Should().Be(orgId);
        quota.PlanTier.Should().Be("Pro");
        quota.LogsQuota.Should().Be(1_000_000);
        quota.TracesQuota.Should().Be(500_000);
        quota.ApplicationsQuota.Should().Be(20);
        quota.MembersQuota.Should().Be(25);
        quota.DashboardsQuota.Should().Be(15);
        quota.HasAnyQuotaExceeded.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateQuota_UpdatesLimitsAndAuditLogged()
    {
        var (_, orgId) = await SeedAuthAndOrgAsync("update");

        var updateReq = new UpdateOrganizationQuotaRequest
        {
            PlanTier = "Enterprise",
            MaxApplications = 50,
            MaxMembers = 100,
            MaxCustomDashboards = 30,
            MaxMonthlyLogs = 10_000_000,
            MaxMonthlyTraces = 5_000_000
        };

        var putRes = await _client.PutAsJsonAsync($"/api/organizations/{orgId}/quota", updateReq);
        putRes.StatusCode.Should().Be(HttpStatusCode.OK);

        var getRes = await _client.GetAsync($"/api/organizations/{orgId}/quota");
        getRes.StatusCode.Should().Be(HttpStatusCode.OK);

        var doc = await getRes.Content.ReadFromJsonAsync<ApiResponse<OrganizationQuotaStatusDto>>();
        var updated = doc!.Data!;
        updated.PlanTier.Should().Be("Enterprise");
        updated.ApplicationsQuota.Should().Be(50);
        updated.MembersQuota.Should().Be(100);
        updated.DashboardsQuota.Should().Be(30);
        updated.LogsQuota.Should().Be(10_000_000);
        updated.TracesQuota.Should().Be(5_000_000);
    }

    [Fact]
    public async Task GetAllQuotasSummary_ReturnsAggregatedTenants()
    {
        var (_, orgId) = await SeedAuthAndOrgAsync("summary");

        var res = await _client.GetAsync("/api/organizations/quotas/summary");
        res.StatusCode.Should().Be(HttpStatusCode.OK);

        var doc = await res.Content.ReadFromJsonAsync<ApiResponse<List<OrganizationQuotaStatusDto>>>();
        doc.Should().NotBeNull();
        doc!.Data.Should().Contain(q => q.OrganizationId == orgId);
    }

    [Fact]
    public async Task CrossTenant_QuotaAccess_Blocked()
    {
        var (_, orgAId) = await SeedAuthAndOrgAsync("tenantA");

        // Switch to Tenant B
        await SeedAuthAndOrgAsync("tenantB");

        var getRes = await _client.GetAsync($"/api/organizations/{orgAId}/quota");
        getRes.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var putRes = await _client.PutAsJsonAsync($"/api/organizations/{orgAId}/quota", new UpdateOrganizationQuotaRequest
        {
            PlanTier = "Starter"
        });
        putRes.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
