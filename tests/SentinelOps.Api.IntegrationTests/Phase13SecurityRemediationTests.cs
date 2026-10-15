using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using SentinelOps.Contracts.DTOs;
using SentinelOps.Contracts.Requests;
using SentinelOps.Contracts.Responses;
using SentinelOps.Domain.Constants;
using SentinelOps.Domain.Entities;
using SentinelOps.Domain.Enums;
using SentinelOps.Infrastructure.Identity;
using SentinelOps.Infrastructure.Persistence;
using Xunit;

namespace SentinelOps.Api.IntegrationTests;

public class Phase13SecurityRemediationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public Phase13SecurityRemediationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private async Task<(string Token, string UserId)> CreateTestUserAsync(string prefix)
    {
        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var tokenService = scope.ServiceProvider.GetRequiredService<SentinelOps.Application.Common.Interfaces.ITokenService>();

        var unique = Guid.NewGuid().ToString("N")[..8];
        var email = $"{prefix}_{unique}@sentinelops.test";
        var user = new ApplicationUser
        {
            UserName = $"{prefix}_{unique}",
            Email = email,
            FullName = $"{prefix} Test User",
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow
        };

        var result = await userManager.CreateAsync(user, "P@ssword2026!Secure");
        result.Succeeded.Should().BeTrue();

        var (token, _) = await tokenService.GenerateAccessTokenAsync(user.Id, user.UserName, user.Email, [Roles.Viewer]);
        return (token, user.Id);
    }

    private async Task<(Guid OrgId, Guid AppId)> CreateOrgWithApplicationAsync(string orgName, string appName, string ownerUserId)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var org = new Organization
        {
            Name = orgName,
            Slug = orgName.ToLower().Replace(" ", "-") + "-" + Guid.NewGuid().ToString("N")[..6],
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow
        };
        db.Organizations.Add(org);

        var member = new OrganizationMember
        {
            OrganizationId = org.Id,
            UserId = ownerUserId,
            Role = OrganizationRole.Owner,
            JoinedAtUtc = DateTime.UtcNow
        };
        db.OrganizationMembers.Add(member);

        var app = new Domain.Entities.Application
        {
            Name = appName,
            DisplayName = appName,
            OrganizationId = org.Id,
            IsActive = true,
            ApiKeyHash = Guid.NewGuid().ToString("N")
        };
        db.Applications.Add(app);

        await db.SaveChangesAsync();
        return (org.Id, app.Id);
    }

    [Fact]
    public async Task GetApplications_WithForgedOrganizationHeader_MustBeRejectedOrIsolated()
    {
        // Arrange: User Alpha belongs to Org Alpha, User Beta belongs to Org Beta
        var (tokenAlpha, userAlphaId) = await CreateTestUserAsync("user_alpha");
        var (_, userBetaId) = await CreateTestUserAsync("user_beta");

        var (orgAlphaId, appAlphaId) = await CreateOrgWithApplicationAsync("Tenant Alpha", "App Alpha", userAlphaId);
        var (orgBetaId, appBetaId) = await CreateOrgWithApplicationAsync("Tenant Beta", "App Beta", userBetaId);

        // Act: User Alpha sends request with X-Organization-Id: Org Beta
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/applications");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenAlpha);
        request.Headers.Add("X-Organization-Id", orgBetaId.ToString());

        var response = await _client.SendAsync(request);

        // Assert: Must either be 401/403 or return zero apps from Org Beta
        if (response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadFromJsonAsync<ApiResponse<List<ApplicationDto>>>();
            body!.Data.Should().NotContain(a => a.Id == appBetaId);
        }
        else
        {
            response.StatusCode.Should().Match(s => s == HttpStatusCode.Unauthorized || s == HttpStatusCode.Forbidden || s == HttpStatusCode.BadRequest);
        }
    }

    [Fact]
    public async Task GetApplications_WithoutOrganizationHeader_MustNotReturnOtherTenantsApplications()
    {
        // Arrange
        var (tokenAlpha, userAlphaId) = await CreateTestUserAsync("user_alpha_no_hdr");
        var (_, userBetaId) = await CreateTestUserAsync("user_beta_no_hdr");

        var (orgAlphaId, appAlphaId) = await CreateOrgWithApplicationAsync("Tenant Alpha 2", "App Alpha 2", userAlphaId);
        var (orgBetaId, appBetaId) = await CreateOrgWithApplicationAsync("Tenant Beta 2", "App Beta 2", userBetaId);

        // Act: User Alpha requests applications with NO header
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/applications");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenAlpha);

        var response = await _client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<ApiResponse<List<ApplicationDto>>>();
        body.Should().NotBeNull();
        body!.Data.Should().NotContain(a => a.Id == appBetaId);
        body.Data.Should().Contain(a => a.Id == appAlphaId);
    }

    [Fact]
    public async Task GetApplicationById_BelongingToAnotherTenant_MustReturnNotFound()
    {
        // Arrange
        var (tokenAlpha, userAlphaId) = await CreateTestUserAsync("user_alpha_idor");
        var (_, userBetaId) = await CreateTestUserAsync("user_beta_idor");

        var (orgAlphaId, appAlphaId) = await CreateOrgWithApplicationAsync("Tenant Alpha IDOR", "App Alpha IDOR", userAlphaId);
        var (orgBetaId, appBetaId) = await CreateOrgWithApplicationAsync("Tenant Beta IDOR", "App Beta IDOR", userBetaId);

        // Act: User Alpha tries to fetch App Beta by direct GUID
        var request = new HttpRequestMessage(HttpMethod.Get, $"/api/applications/{appBetaId}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenAlpha);

        var response = await _client.SendAsync(request);

        // Assert: Must return 404 Not Found
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task BillingEntitlements_ForDifferentTenant_MustReturnNotFoundOrUnauthorized()
    {
        // Arrange
        var (tokenAlpha, userAlphaId) = await CreateTestUserAsync("user_alpha_bill");
        var (_, userBetaId) = await CreateTestUserAsync("user_beta_bill");

        var (orgAlphaId, _) = await CreateOrgWithApplicationAsync("Tenant Alpha Bill", "App Alpha Bill", userAlphaId);
        var (orgBetaId, _) = await CreateOrgWithApplicationAsync("Tenant Beta Bill", "App Beta Bill", userBetaId);

        // Act: User Alpha queries entitlements for Org Beta
        var request = new HttpRequestMessage(HttpMethod.Get, $"/api/billing/entitlements?organizationId={orgBetaId}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenAlpha);

        var response = await _client.SendAsync(request);

        // Assert: Must be NotFound or Unauthorized/Forbidden
        response.StatusCode.Should().Match(s => s == HttpStatusCode.NotFound || s == HttpStatusCode.Unauthorized || s == HttpStatusCode.Forbidden);
    }
}
