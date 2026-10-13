using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using SentinelOps.Contracts.DTOs;
using SentinelOps.Contracts.Requests;
using SentinelOps.Contracts.Responses;
using Xunit;

namespace SentinelOps.Api.IntegrationTests;

public class OrganizationMultiTenantIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public OrganizationMultiTenantIntegrationTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    private async Task<string> RegisterAndLoginUserAsync(string prefix = "user")
    {
        var email = $"{prefix}_{Guid.NewGuid():N}@sentinelops.local";
        var password = "P@ssword2026!Secure";

        var registerRes = await _client.PostAsJsonAsync("/api/auth/register", new RegisterRequest
        {
            UserName = $"{prefix}_{Guid.NewGuid():N}"[..20],
            Email = email,
            FullName = "MultiTenant Test User",
            Password = password,
            ConfirmPassword = password
        });
        registerRes.EnsureSuccessStatusCode();

        var regDoc = await registerRes.Content.ReadFromJsonAsync<ApiResponse<AuthResponse>>();
        return regDoc!.Data!.Token;
    }

    [Fact]
    public async Task CreateOrganization_ShouldSucceedAndAssignOwnerRole()
    {
        var token = await RegisterAndLoginUserAsync("org_owner");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var request = new CreateOrganizationRequest
        {
            Name = "Enterprise Cloud Services",
            Slug = "enterprise-cloud-" + Guid.NewGuid().ToString("N")[..6],
            Description = "Multi-tenant enterprise cloud monitoring."
        };

        var response = await _client.PostAsJsonAsync("/api/organizations", request);
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var body = await response.Content.ReadFromJsonAsync<ApiResponse<OrganizationDto>>();
        body.Should().NotBeNull();
        body!.Success.Should().BeTrue();
        body.Data.Should().NotBeNull();
        body.Data!.Name.Should().Be("Enterprise Cloud Services");
        body.Data.UserRole.Should().Be("Owner");
        body.Data.MembersCount.Should().Be(1);
    }

    [Fact]
    public async Task GetUserOrganizations_ShouldReturnOnlyUserJoinedOrganizations()
    {
        var token1 = await RegisterAndLoginUserAsync("user_one");
        var token2 = await RegisterAndLoginUserAsync("user_two");

        // User 1 creates Org A
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token1);
        var orgARes = await _client.PostAsJsonAsync("/api/organizations", new CreateOrganizationRequest
        {
            Name = "Org Alpha",
            Slug = "org-alpha-" + Guid.NewGuid().ToString("N")[..6]
        });
        orgARes.EnsureSuccessStatusCode();

        // User 2 lists orgs
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token2);
        var listRes = await _client.GetAsync("/api/organizations");
        listRes.EnsureSuccessStatusCode();

        var body = await listRes.Content.ReadFromJsonAsync<ApiResponse<IEnumerable<OrganizationDto>>>();
        body!.Success.Should().BeTrue();
        // User 2 should NOT see Org Alpha
        body.Data.Should().NotContain(o => o.Name == "Org Alpha");
    }

    [Fact]
    public async Task InviteMember_AndAcceptInvitation_ShouldAddMemberWithRole()
    {
        var ownerToken = await RegisterAndLoginUserAsync("org_inviter");
        var inviteeToken = await RegisterAndLoginUserAsync("org_invitee");

        // 1. Owner creates org
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", ownerToken);
        var createRes = await _client.PostAsJsonAsync("/api/organizations", new CreateOrganizationRequest
        {
            Name = "Collaboration Org",
            Slug = "collab-" + Guid.NewGuid().ToString("N")[..6]
        });
        createRes.EnsureSuccessStatusCode();
        var org = (await createRes.Content.ReadFromJsonAsync<ApiResponse<OrganizationDto>>())!.Data!;

        // 2. Owner invites a user
        var inviteEmail = "collaborator@sentinelops.local";
        var inviteRes = await _client.PostAsJsonAsync($"/api/organizations/{org.Id}/members/invite", new InviteMemberRequest
        {
            Email = inviteEmail,
            Role = "Admin"
        });
        inviteRes.EnsureSuccessStatusCode();
        var inviteData = (await inviteRes.Content.ReadFromJsonAsync<ApiResponse<InvitationCreatedResponse>>())!.Data!;
        inviteData.RawToken.Should().NotBeNullOrWhiteSpace();

        // 3. Invitee accepts the invitation using raw token
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", inviteeToken);
        var acceptRes = await _client.PostAsJsonAsync("/api/organizations/invitations/accept", new AcceptInvitationRequest
        {
            Token = inviteData.RawToken
        });
        acceptRes.EnsureSuccessStatusCode();

        // 4. Verify invitee now sees the organization as Admin
        var listRes = await _client.GetAsync("/api/organizations");
        var listData = (await listRes.Content.ReadFromJsonAsync<ApiResponse<IEnumerable<OrganizationDto>>>())!.Data!;
        listData.Should().Contain(o => o.Id == org.Id && o.UserRole == "Admin");
    }

    [Fact]
    public async Task CrossTenantAccess_ShouldReturnNotFoundOrForbidden()
    {
        var ownerToken = await RegisterAndLoginUserAsync("owner_isolation");
        var strangerToken = await RegisterAndLoginUserAsync("stranger_isolation");

        // Owner creates org
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", ownerToken);
        var createRes = await _client.PostAsJsonAsync("/api/organizations", new CreateOrganizationRequest
        {
            Name = "Private Enclave Org",
            Slug = "private-enclave-" + Guid.NewGuid().ToString("N")[..6]
        });
        createRes.EnsureSuccessStatusCode();
        var org = (await createRes.Content.ReadFromJsonAsync<ApiResponse<OrganizationDto>>())!.Data!;

        // Stranger tries to get organization details
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", strangerToken);
        var getRes = await _client.GetAsync($"/api/organizations/{org.Id}");
        getRes.StatusCode.Should().Be(HttpStatusCode.NotFound);

        // Stranger tries to update organization
        var putRes = await _client.PutAsJsonAsync($"/api/organizations/{org.Id}", new UpdateOrganizationRequest
        {
            Name = "Hacked Name"
        });
        putRes.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        // Stranger tries to invite members
        var inviteRes = await _client.PostAsJsonAsync($"/api/organizations/{org.Id}/members/invite", new InviteMemberRequest
        {
            Email = "intruder@sentinelops.local",
            Role = "Owner"
        });
        inviteRes.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
