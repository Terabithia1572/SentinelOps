using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
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

public class TenantSelfServiceIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public TenantSelfServiceIntegrationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task TransferOwnership_AsOwner_TransfersOwnershipAndDemotesPreviousOwner()
    {
        var client = _factory.CreateClient();
        var unique = Guid.NewGuid().ToString("N")[..8];
        var ownerEmail = $"owner-{unique}@sentinelops.test";
        var newOwnerEmail = $"newowner-{unique}@sentinelops.test";
        var password = "Password123!";

        var (ownerToken, orgId, ownerUserId) = await SetupTenantWithOwnerAsync(client, ownerEmail, password, unique);
        var newOwnerUserId = await CreateUserAsync(newOwnerEmail, password);

        // Add second user as member
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            db.OrganizationMembers.Add(new OrganizationMember
            {
                OrganizationId = orgId,
                UserId = newOwnerUserId,
                Role = OrganizationRole.Member,
                JoinedAtUtc = DateTime.UtcNow
            });
            await db.SaveChangesAsync();
        }

        // Transfer ownership
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", ownerToken);
        var res = await client.PostAsJsonAsync($"/api/organizations/{orgId}/ownership-transfer", new TransferOwnershipRequest
        {
            NewOwnerUserId = newOwnerUserId
        });

        res.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify in DB
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var newOwnerMember = await db.OrganizationMembers.FirstAsync(m => m.OrganizationId == orgId && m.UserId == newOwnerUserId);
            var prevOwnerMember = await db.OrganizationMembers.FirstAsync(m => m.OrganizationId == orgId && m.UserId == ownerUserId);

            newOwnerMember.Role.Should().Be(OrganizationRole.Owner);
            prevOwnerMember.Role.Should().Be(OrganizationRole.Admin);
        }
    }

    [Fact]
    public async Task TransferOwnership_AsNonOwner_ReturnsBadRequest()
    {
        var client = _factory.CreateClient();
        var unique = Guid.NewGuid().ToString("N")[..8];
        var ownerEmail = $"owner2-{unique}@sentinelops.test";
        var adminEmail = $"admin2-{unique}@sentinelops.test";
        var password = "Password123!";

        var (_, orgId, _) = await SetupTenantWithOwnerAsync(client, ownerEmail, password, unique);
        var adminUserId = await CreateUserAsync(adminEmail, password);

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            db.OrganizationMembers.Add(new OrganizationMember
            {
                OrganizationId = orgId,
                UserId = adminUserId,
                Role = OrganizationRole.Admin,
                JoinedAtUtc = DateTime.UtcNow
            });
            await db.SaveChangesAsync();
        }

        // Login as admin
        var adminLoginRes = await client.PostAsJsonAsync("/api/auth/login", new { Email = adminEmail, Password = password });
        var adminAuth = await adminLoginRes.Content.ReadFromJsonAsync<ApiResponse<AuthResponse>>();
        var adminToken = adminAuth!.Data!.Token;

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
        var res = await client.PostAsJsonAsync($"/api/organizations/{orgId}/ownership-transfer", new TransferOwnershipRequest
        {
            NewOwnerUserId = adminUserId
        });

        res.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var content = await res.Content.ReadAsStringAsync();
        content.Should().Contain("Only the organization Owner can transfer ownership");
    }

    [Fact]
    public async Task TransferOwnership_ToNonMember_ReturnsBadRequest()
    {
        var client = _factory.CreateClient();
        var unique = Guid.NewGuid().ToString("N")[..8];
        var ownerEmail = $"owner3-{unique}@sentinelops.test";
        var nonMemberEmail = $"nonmember-{unique}@sentinelops.test";
        var password = "Password123!";

        var (ownerToken, orgId, _) = await SetupTenantWithOwnerAsync(client, ownerEmail, password, unique);
        var nonMemberUserId = await CreateUserAsync(nonMemberEmail, password);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", ownerToken);
        var res = await client.PostAsJsonAsync($"/api/organizations/{orgId}/ownership-transfer", new TransferOwnershipRequest
        {
            NewOwnerUserId = nonMemberUserId
        });

        res.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var content = await res.Content.ReadAsStringAsync();
        content.Should().Contain("Target user is not a member of this organization");
    }

    [Fact]
    public async Task TransferOwnership_ToSelf_ReturnsBadRequest()
    {
        var client = _factory.CreateClient();
        var unique = Guid.NewGuid().ToString("N")[..8];
        var ownerEmail = $"owner4-{unique}@sentinelops.test";
        var password = "Password123!";

        var (ownerToken, orgId, ownerUserId) = await SetupTenantWithOwnerAsync(client, ownerEmail, password, unique);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", ownerToken);
        var res = await client.PostAsJsonAsync($"/api/organizations/{orgId}/ownership-transfer", new TransferOwnershipRequest
        {
            NewOwnerUserId = ownerUserId
        });

        res.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var content = await res.Content.ReadAsStringAsync();
        content.Should().Contain("Cannot transfer ownership to yourself");
    }

    [Fact]
    public async Task RemoveMember_LastOwner_BlockedByGuardrail()
    {
        var client = _factory.CreateClient();
        var unique = Guid.NewGuid().ToString("N")[..8];
        var ownerEmail = $"owner5-{unique}@sentinelops.test";
        var password = "Password123!";

        var (ownerToken, orgId, ownerUserId) = await SetupTenantWithOwnerAsync(client, ownerEmail, password, unique);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", ownerToken);
        var res = await client.DeleteAsync($"/api/organizations/{orgId}/members/{ownerUserId}");

        res.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var content = await res.Content.ReadAsStringAsync();
        content.Should().Contain("Cannot remove the only Owner");
    }

    [Fact]
    public async Task ExportTenantData_ReturnsCompleteTenantArchive()
    {
        var client = _factory.CreateClient();
        var unique = Guid.NewGuid().ToString("N")[..8];
        var ownerEmail = $"export-{unique}@sentinelops.test";
        var password = "Password123!";

        var (ownerToken, orgId, _) = await SetupTenantWithOwnerAsync(client, ownerEmail, password, unique);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", ownerToken);
        var res = await client.PostAsync($"/api/organizations/{orgId}/export", null);

        res.StatusCode.Should().Be(HttpStatusCode.OK);
        var data = await res.Content.ReadFromJsonAsync<ApiResponse<TenantDataExportDto>>();
        data.Should().NotBeNull();
        data!.Data.Should().NotBeNull();
        data.Data!.OrganizationId.Should().Be(orgId);
        data.Data.Name.Should().Contain("SelfService Org");
        data.Data.Settings.Should().NotBeNull();
        data.Data.Subscription.Should().NotBeNull();
        data.Data.Subscription!.Status.Should().Be("Trialing");
        data.Data.Members.Should().NotBeEmpty();
        data.Data.Applications.Should().NotBeEmpty();
    }

    [Fact]
    public async Task DeleteAccount_WithIncorrectConfirmationPhrase_Fails()
    {
        var client = _factory.CreateClient();
        var unique = Guid.NewGuid().ToString("N")[..8];
        var ownerEmail = $"del-fail-{unique}@sentinelops.test";
        var password = "Password123!";

        var (ownerToken, orgId, _) = await SetupTenantWithOwnerAsync(client, ownerEmail, password, unique);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", ownerToken);
        var res = await client.SendAsync(new HttpRequestMessage(HttpMethod.Delete, $"/api/organizations/{orgId}/account")
        {
            Content = JsonContent.Create(new DeleteAccountRequest
            {
                ConfirmationPhrase = "DELETE NOW"
            })
        });

        res.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var content = await res.Content.ReadAsStringAsync();
        content.Should().Contain("Invalid confirmation phrase");

        // Verify organization still active
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var org = await db.Organizations.FirstAsync(o => o.Id == orgId);
            org.IsActive.Should().BeTrue();
        }
    }

    [Fact]
    public async Task DeleteAccount_WithCorrectPhrase_SoftDeletesOrganizationAndCancelsSubscription()
    {
        var client = _factory.CreateClient();
        var unique = Guid.NewGuid().ToString("N")[..8];
        var ownerEmail = $"del-ok-{unique}@sentinelops.test";
        var orgName = $"SelfService Org {unique}";
        var password = "Password123!";

        var (ownerToken, orgId, _) = await SetupTenantWithOwnerAsync(client, ownerEmail, password, unique, orgName);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", ownerToken);
        var res = await client.SendAsync(new HttpRequestMessage(HttpMethod.Delete, $"/api/organizations/{orgId}/account")
        {
            Content = JsonContent.Create(new DeleteAccountRequest
            {
                ConfirmationPhrase = $"CONFIRM DELETE {orgName}"
            })
        });

        res.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify soft-deletion in DB
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var org = await db.Organizations.FirstAsync(o => o.Id == orgId);
            org.IsActive.Should().BeFalse();

            var apps = await db.Applications.Where(a => a.OrganizationId == orgId).ToListAsync();
            apps.Should().AllSatisfy(a => a.IsActive.Should().BeFalse());

            var sub = await db.OrganizationSubscriptions.FirstOrDefaultAsync(s => s.OrganizationId == orgId);
            sub.Should().NotBeNull();
            sub!.Status.Should().Be(SubscriptionStatus.Canceled);
        }
    }

    private async Task<(string Token, Guid OrgId, string UserId)> SetupTenantWithOwnerAsync(
        HttpClient client,
        string email,
        string password,
        string unique,
        string? orgNameOverride = null)
    {
        var userId = await CreateUserAsync(email, password);

        var loginRes = await client.PostAsJsonAsync("/api/auth/login", new { Email = email, Password = password });
        var authData = await loginRes.Content.ReadFromJsonAsync<ApiResponse<AuthResponse>>();
        var token = authData!.Data!.Token;

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var orgName = orgNameOverride ?? $"SelfService Org {unique}";
        var provisionRes = await client.PostAsJsonAsync("/api/onboarding/provision", new OnboardingProvisionRequest
        {
            OrganizationName = orgName,
            PlanTier = "Pro",
            ApplicationName = $"app-ss-{unique}"
        });
        var provisioned = await provisionRes.Content.ReadFromJsonAsync<ApiResponse<OnboardingProvisionedResponse>>();
        return (token, provisioned!.Data!.OrganizationId, userId);
    }

    private async Task<string> CreateUserAsync(string email, string password)
    {
        using var scope = _factory.Services.CreateScope();
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
            FullName = "SelfService Test User"
        };
        await userManager.CreateAsync(user, password);
        await userManager.AddToRoleAsync(user, Roles.Operator);
        return user.Id;
    }
}
