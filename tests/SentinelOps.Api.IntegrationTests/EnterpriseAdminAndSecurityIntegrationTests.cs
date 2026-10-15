using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using SentinelOps.Application.Common.Interfaces;
using SentinelOps.Contracts.DTOs;
using SentinelOps.Contracts.Requests;
using SentinelOps.Contracts.Responses;
using SentinelOps.Domain.Constants;
using SentinelOps.Infrastructure.Identity;
using Xunit;

namespace SentinelOps.Api.IntegrationTests;

public class EnterpriseAdminAndSecurityIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public EnterpriseAdminAndSecurityIntegrationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private async Task<(string Token, string UserId)> CreateUserWithRoleAsync(string role)
    {
        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var tokenService = scope.ServiceProvider.GetRequiredService<ITokenService>();

        if (!await roleManager.RoleExistsAsync(role))
        {
            await roleManager.CreateAsync(new IdentityRole(role));
        }

        var unique = Guid.NewGuid().ToString("N")[..8];
        var user = new ApplicationUser
        {
            UserName = $"user_{role}_{unique}",
            Email = $"user_{role}_{unique}@sentinelops.local",
            FullName = $"Test {role} User",
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow
        };

        var result = await userManager.CreateAsync(user, "P@ssword2026!Secure");
        result.Succeeded.Should().BeTrue();

        await userManager.AddToRoleAsync(user, role);

        var (token, _) = await tokenService.GenerateAccessTokenAsync(user.Id, user.UserName, user.Email, [role]);
        return (token, user.Id);
    }

    [Fact]
    public async Task UsersEndpoint_WhenAnonymous_ShouldReturnUnauthorized()
    {
        var response = await _client.GetAsync("/api/users");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task UsersEndpoint_WhenViewer_ShouldReturnForbidden()
    {
        var (viewerToken, _) = await CreateUserWithRoleAsync(Roles.Viewer);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", viewerToken);

        var response = await _client.GetAsync("/api/users");
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task UsersEndpoint_WhenAdmin_ShouldReturnPagedUserDirectory()
    {
        var (adminToken, _) = await CreateUserWithRoleAsync(Roles.Admin);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        var response = await _client.GetAsync("/api/users?page=1&pageSize=10");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<ApiResponse<PagedResponse<UserDto>>>();
        body.Should().NotBeNull();
        body!.Success.Should().BeTrue();
        body.Data!.Items.Should().NotBeEmpty();
    }

    [Fact]
    public async Task UpdateRoles_WhenDemotingLastAdmin_ShouldEnforceGuardrailAndReject()
    {
        // 1. Create a solitary admin
        var (adminToken, adminId) = await CreateUserWithRoleAsync(Roles.Admin);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        // Ensure we check other admins count
        using (var scope = _factory.Services.CreateScope())
        {
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var allAdmins = await userManager.GetUsersInRoleAsync(Roles.Admin);
            foreach (var otherAdmin in allAdmins.Where(a => a.Id != adminId))
            {
                otherAdmin.IsActive = false;
                await userManager.UpdateAsync(otherAdmin);
            }
        }

        // 2. Attempt to remove Admin role from the last active admin
        var updateRequest = new UpdateUserRoleRequest { Roles = [Roles.Viewer] };
        var response = await _client.PutAsJsonAsync($"/api/users/{adminId}/roles", updateRequest);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<bool>>();
        body.Should().NotBeNull();
        body!.Success.Should().BeFalse();
        body.Message.Should().Contain("last active Administrator");
    }

    [Fact]
    public async Task UpdateRoles_WhenViewerOrOperator_ShouldReturnForbidden()
    {
        var (viewerToken, viewerId) = await CreateUserWithRoleAsync(Roles.Viewer);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", viewerToken);

        // Viewer attempts to self-escalate to Admin
        var updateRequest = new UpdateUserRoleRequest { Roles = [Roles.Admin] };
        var response = await _client.PutAsJsonAsync($"/api/users/{viewerId}/roles", updateRequest);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task AuditCenter_WhenAdmin_ShouldReturnAuditEventsAndSummary()
    {
        var (adminToken, _) = await CreateUserWithRoleAsync(Roles.Admin);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        var response = await _client.GetAsync("/api/audit");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<ApiResponse<PagedResponse<AuditEventDto>>>();
        body.Should().NotBeNull();
        body!.Success.Should().BeTrue();

        var summaryResponse = await _client.GetAsync("/api/audit/summary");
        summaryResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var summaryBody = await summaryResponse.Content.ReadFromJsonAsync<ApiResponse<AuditSummaryDto>>();
        summaryBody.Should().NotBeNull();
        summaryBody!.Success.Should().BeTrue();
    }

    [Fact]
    public async Task ReportingCenter_WhenAuthenticated_ShouldGeneratePreviewAndExportCsv()
    {
        var (viewerToken, _) = await CreateUserWithRoleAsync(Roles.Viewer);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", viewerToken);

        // 1. Report types
        var typesRes = await _client.GetAsync("/api/reports/types");
        typesRes.StatusCode.Should().Be(HttpStatusCode.OK);

        var typesBody = await typesRes.Content.ReadFromJsonAsync<ApiResponse<List<ReportTypeOptionDto>>>();
        typesBody.Should().NotBeNull();
        typesBody!.Data.Should().HaveCount(6);

        // 2. Report preview
        var previewReq = new GenerateReportRequest { ReportType = "Performance", TimeRange = "24h" };
        var previewRes = await _client.PostAsJsonAsync("/api/reports/preview", previewReq);
        previewRes.StatusCode.Should().Be(HttpStatusCode.OK);

        var previewBody = await previewRes.Content.ReadFromJsonAsync<ApiResponse<ReportPreviewDto>>();
        previewBody.Should().NotBeNull();
        previewBody!.Data!.ReportType.Should().Be("Performance");
        previewBody.Data.ColumnHeaders.Should().NotBeEmpty();

        // 3. Export CSV
        var exportReq = new ExportReportRequest { ReportType = "Performance", TimeRange = "24h", Format = "csv" };
        var exportRes = await _client.PostAsJsonAsync("/api/reports/export/csv", exportReq);
        exportRes.StatusCode.Should().Be(HttpStatusCode.OK);
        exportRes.Content.Headers.ContentType!.MediaType.Should().Be("text/csv");
    }

    [Fact]
    public async Task RetentionAndHealth_ShouldReturnDryRunEstimatesAndLiveHealth()
    {
        var (adminToken, _) = await CreateUserWithRoleAsync(Roles.Admin);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        // 1. System Health
        var healthRes = await _client.GetAsync("/api/system/health");
        healthRes.StatusCode.Should().Be(HttpStatusCode.OK);

        var healthBody = await healthRes.Content.ReadFromJsonAsync<ApiResponse<SystemHealthDto>>();
        healthBody.Should().NotBeNull();
        healthBody!.Data!.Database.Status.Should().Be("Healthy");
        healthBody.Data.IngestionEngine.Status.Should().Be("Healthy");

        // 2. Retention dry run preview
        var previewRes = await _client.PostAsync("/api/retention/preview", null);
        previewRes.StatusCode.Should().Be(HttpStatusCode.OK);

        var previewBody = await previewRes.Content.ReadFromJsonAsync<ApiResponse<RetentionDryRunResultDto>>();
        previewBody.Should().NotBeNull();
        previewBody!.Data!.Logs.TableName.Should().Be("LogEntries");

        // 3. Retention execution without confirmation should fail
        var execFailRes = await _client.PostAsJsonAsync("/api/retention/execute", new ExecuteRetentionCleanupRequest { Confirm = false });
        execFailRes.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        // 4. Retention execution with confirmation should succeed
        var execSuccessRes = await _client.PostAsJsonAsync("/api/retention/execute", new ExecuteRetentionCleanupRequest { Confirm = true });
        execSuccessRes.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
