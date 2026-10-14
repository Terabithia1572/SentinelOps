using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SentinelOps.Application.Common.Interfaces;
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

public class EntitlementsAndUsageIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public EntitlementsAndUsageIntegrationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetEntitlements_ReturnsValidTenantMatrix()
    {
        var client = _factory.CreateClient();
        var unique = Guid.NewGuid().ToString("N")[..8];
        var email = $"ent-{unique}@sentinelops.test";
        var password = "Password123!";

        var (token, orgId) = await SetupTenantAsync(client, email, password, unique, "Starter");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        client.DefaultRequestHeaders.Add("X-Organization-Id", orgId.ToString());

        var res = await client.GetAsync("/api/billing/entitlements");
        res.StatusCode.Should().Be(HttpStatusCode.OK);

        var data = await res.Content.ReadFromJsonAsync<ApiResponse<TenantEntitlementsDto>>();
        data.Should().NotBeNull();
        data!.Data!.IsSubscriptionActive.Should().BeTrue();
        data.Data.PlanTier.Should().Be("Starter");
        data.Data.CanRegisterApplication.Should().BeTrue();
        data.Data.CanInviteMember.Should().BeTrue();
        data.Data.CanCreateDashboard.Should().BeTrue();
        data.Data.AllowApiAccess.Should().BeTrue();
        data.Data.AllowCustomReports.Should().BeFalse(); // Starter doesn't have custom reports
    }

    [Fact]
    public async Task GetUsageSummary_CalculatesPercentagesAndWarningsAccurately()
    {
        var client = _factory.CreateClient();
        var unique = Guid.NewGuid().ToString("N")[..8];
        var email = $"use-{unique}@sentinelops.test";
        var password = "Password123!";

        var (token, orgId) = await SetupTenantAsync(client, email, password, unique, "Starter");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        client.DefaultRequestHeaders.Add("X-Organization-Id", orgId.ToString());

        // Increment usage
        using (var scope = _factory.Services.CreateScope())
        {
            var entitlementService = scope.ServiceProvider.GetRequiredService<IEntitlementService>();
            await entitlementService.RecordUsageAsync(orgId, MeteredMetric.Logs, 50_000);
            await entitlementService.RecordUsageAsync(orgId, MeteredMetric.Traces, 10_000);
            await entitlementService.RecordUsageAsync(orgId, MeteredMetric.AiTokens, 2_000);
        }

        var res = await client.GetAsync("/api/billing/usage");
        res.StatusCode.Should().Be(HttpStatusCode.OK);

        var data = await res.Content.ReadFromJsonAsync<ApiResponse<TenantUsageSummaryDto>>();
        data.Should().NotBeNull();
        var usage = data!.Data!;
        usage.LogsUsed.Should().Be(50_000);
        usage.TracesUsed.Should().Be(10_000);
        usage.AiTokensUsed.Should().Be(2_000);
        usage.DaysRemainingInPeriod.Should().BeGreaterThan(0);
        usage.LogsPercent.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task EnforceQuota_ApplicationsLimit_RejectsWhenQuotaReached()
    {
        var client = _factory.CreateClient();
        var unique = Guid.NewGuid().ToString("N")[..8];
        var email = $"app-limit-{unique}@sentinelops.test";
        var password = "Password123!";

        var (token, orgId) = await SetupTenantAsync(client, email, password, unique, "Starter");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        client.DefaultRequestHeaders.Add("X-Organization-Id", orgId.ToString());

        // Set MaxApplications to 1 (which was already consumed during onboarding)
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var settings = await db.OrganizationSettings.FirstAsync(s => s.OrganizationId == orgId);
            settings.MaxApplications = 1;
            await db.SaveChangesAsync();
        }

        // Try creating a second application -> should fail
        var createRes = await client.PostAsJsonAsync("/api/applications", new CreateApplicationRequest
        {
            Name = $"app-blocked-{unique}",
            DisplayName = "Blocked App",
            Description = "Should exceed quota"
        });

        createRes.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var errText = await createRes.Content.ReadAsStringAsync();
        errText.Should().Contain("Application limit reached");
    }

    [Fact]
    public async Task EnforceQuota_MemberSeats_RejectsWhenQuotaReached()
    {
        var client = _factory.CreateClient();
        var unique = Guid.NewGuid().ToString("N")[..8];
        var email = $"mem-limit-{unique}@sentinelops.test";
        var password = "Password123!";

        var (token, orgId) = await SetupTenantAsync(client, email, password, unique, "Starter");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        client.DefaultRequestHeaders.Add("X-Organization-Id", orgId.ToString());

        // Set MaxMembers to 1 (the owner is already member 1)
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var settings = await db.OrganizationSettings.FirstAsync(s => s.OrganizationId == orgId);
            settings.MaxMembers = 1;
            await db.SaveChangesAsync();
        }

        // Try inviting another member
        var inviteRes = await client.PostAsJsonAsync($"/api/organizations/{orgId}/members/invite", new InviteMemberRequest
        {
            Email = $"invite-fail-{unique}@sentinelops.test",
            Role = "Member"
        });

        inviteRes.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var errText = await inviteRes.Content.ReadAsStringAsync();
        errText.Should().Contain("Member seat limit reached");
    }

    [Fact]
    public async Task EnforceQuota_CustomDashboards_RejectsWhenQuotaReached()
    {
        var client = _factory.CreateClient();
        var unique = Guid.NewGuid().ToString("N")[..8];
        var email = $"dash-limit-{unique}@sentinelops.test";
        var password = "Password123!";

        var (token, orgId) = await SetupTenantAsync(client, email, password, unique, "Starter");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        client.DefaultRequestHeaders.Add("X-Organization-Id", orgId.ToString());

        // Set MaxCustomDashboards to 1
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var settings = await db.OrganizationSettings.FirstAsync(s => s.OrganizationId == orgId);
            settings.MaxCustomDashboards = 1;
            await db.SaveChangesAsync();
        }

        // Create 1st dashboard -> OK
        var dash1Res = await client.PostAsJsonAsync("/api/dashboards", new CreateDashboardRequest
        {
            Title = "First Dashboard",
            Description = "Allowed"
        });
        dash1Res.StatusCode.Should().Be(HttpStatusCode.Created);

        // Create 2nd dashboard -> Should fail
        var dash2Res = await client.PostAsJsonAsync("/api/dashboards", new CreateDashboardRequest
        {
            Title = "Second Dashboard",
            Description = "Should be rejected"
        });
        dash2Res.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var errText = await dash2Res.Content.ReadAsStringAsync();
        errText.Should().Contain("Custom dashboard limit reached");
    }

    [Fact]
    public async Task EnforceQuota_SubscriptionSuspended_BlocksResourceCreation()
    {
        var client = _factory.CreateClient();
        var unique = Guid.NewGuid().ToString("N")[..8];
        var email = $"susp-{unique}@sentinelops.test";
        var password = "Password123!";

        var (token, orgId) = await SetupTenantAsync(client, email, password, unique, "Starter");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        client.DefaultRequestHeaders.Add("X-Organization-Id", orgId.ToString());

        // Suspend subscription
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var sub = await db.OrganizationSubscriptions.FirstAsync(s => s.OrganizationId == orgId);
            sub.Status = SubscriptionStatus.Suspended;
            await db.SaveChangesAsync();
        }

        // Verify entitlement matrix reflects inactive status
        var entRes = await client.GetAsync("/api/billing/entitlements");
        var entData = await entRes.Content.ReadFromJsonAsync<ApiResponse<TenantEntitlementsDto>>();
        entData!.Data!.IsSubscriptionActive.Should().BeFalse();
        entData.Data.CanRegisterApplication.Should().BeFalse();
        entData.Data.CanCreateDashboard.Should().BeFalse();

        // Try creating application -> rejected
        var appRes = await client.PostAsJsonAsync("/api/applications", new CreateApplicationRequest
        {
            Name = $"app-susp-{unique}",
            DisplayName = "Suspended App"
        });
        appRes.StatusCode.Should().NotBe(HttpStatusCode.Created);
    }

    [Fact]
    public async Task UsageMetering_MonthlyRollover_ResetsMonthToDateCounters()
    {
        var client = _factory.CreateClient();
        var unique = Guid.NewGuid().ToString("N")[..8];
        var email = $"roll-{unique}@sentinelops.test";
        var password = "Password123!";

        var (_, orgId) = await SetupTenantAsync(client, email, password, unique, "Starter");

        using (var scope = _factory.Services.CreateScope())
        {
            var entitlementService = scope.ServiceProvider.GetRequiredService<IEntitlementService>();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            // 1. Record usage in "previous month"
            var usage = await db.OrganizationUsages.FirstAsync(u => u.OrganizationId == orgId);
            usage.LogsIngestedMonthToDate = 90_000;
            usage.TracesIngestedMonthToDate = 45_000;
            usage.LastResetAtUtc = DateTime.UtcNow.AddMonths(-1); // previous month
            await db.SaveChangesAsync();

            // 2. Record new usage in current month -> triggers automatic rollover
            await entitlementService.RecordUsageAsync(orgId, MeteredMetric.Logs, 100);

            var rolledOver = await db.OrganizationUsages.FirstAsync(u => u.OrganizationId == orgId);
            rolledOver.LogsIngestedMonthToDate.Should().Be(100); // 90k was reset and 100 was added
            rolledOver.TracesIngestedMonthToDate.Should().Be(0); // reset
            rolledOver.LastResetAtUtc.Month.Should().Be(DateTime.UtcNow.Month);
        }
    }

    private async Task<(string Token, Guid OrgId)> SetupTenantAsync(HttpClient client, string email, string password, string unique, string tier)
    {
        using (var scope = _factory.Services.CreateScope())
        {
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

            if (!await roleManager.RoleExistsAsync(Roles.Operator))
            {
                await roleManager.CreateAsync(new IdentityRole(Roles.Operator));
            }

            var user = new ApplicationUser
            {
                Id = Guid.NewGuid().ToString(),
                UserName = email,
                Email = email,
                FullName = "Entitlement Test User"
            };
            await userManager.CreateAsync(user, password);
            await userManager.AddToRoleAsync(user, Roles.Operator);
        }

        var loginRes = await client.PostAsJsonAsync("/api/auth/login", new { Email = email, Password = password });
        var authData = await loginRes.Content.ReadFromJsonAsync<ApiResponse<AuthResponse>>();
        var token = authData!.Data!.Token;

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var provisionRes = await client.PostAsJsonAsync("/api/onboarding/provision", new OnboardingProvisionRequest
        {
            OrganizationName = $"Ent Org {unique}",
            PlanTier = tier,
            ApplicationName = $"app-ent-{unique}"
        });
        var provisioned = await provisionRes.Content.ReadFromJsonAsync<ApiResponse<OnboardingProvisionedResponse>>();
        return (token, provisioned!.Data!.OrganizationId);
    }
}
