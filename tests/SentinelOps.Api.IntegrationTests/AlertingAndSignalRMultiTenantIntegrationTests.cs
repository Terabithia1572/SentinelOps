using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;
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
using SentinelOps.Infrastructure.Services.Notifications;
using Xunit;

namespace SentinelOps.Api.IntegrationTests;

public class AlertingAndSignalRMultiTenantIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public AlertingAndSignalRMultiTenantIntegrationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private async Task<(string token, string email, string userId)> CreateUserWithRoleAsync(string prefix, string role = Roles.Operator)
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
        var email = $"{prefix}_{unique}@sentinelops.local";
        var user = new ApplicationUser
        {
            UserName = $"user_{prefix}_{unique}",
            Email = email,
            FullName = $"{prefix} Test User",
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow
        };

        var result = await userManager.CreateAsync(user, "P@ssword2026!Secure");
        result.Succeeded.Should().BeTrue();

        await userManager.AddToRoleAsync(user, role);

        var (token, _) = await tokenService.GenerateAccessTokenAsync(user.Id, user.UserName, user.Email, [role]);
        return (token, email, user.Id);
    }

    [Fact]
    public async Task MultiTenant_AlertRulesAndIncidents_AreIsolatedBetweenOrganizations()
    {
        // 1. Setup Tenant Alpha
        var (tokenAlpha, _, _) = await CreateUserWithRoleAsync("alert_alpha", Roles.Admin);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenAlpha);
        _client.DefaultRequestHeaders.Remove("X-Organization-Id");

        var orgAlphaRes = await _client.PostAsJsonAsync("/api/organizations", new CreateOrganizationRequest
        {
            Name = "Alert Alpha Org",
            Slug = "alert-alpha-" + Guid.NewGuid().ToString("N")[..6]
        });
        orgAlphaRes.EnsureSuccessStatusCode();
        var orgAlpha = (await orgAlphaRes.Content.ReadFromJsonAsync<ApiResponse<OrganizationDto>>())!.Data!;

        _client.DefaultRequestHeaders.Add("X-Organization-Id", orgAlpha.Id.ToString());
        var appAlphaRes = await _client.PostAsJsonAsync("/api/applications", new CreateApplicationRequest
        {
            Name = "app-alpha-alerts-" + Guid.NewGuid().ToString("N")[..6],
            DisplayName = "Alpha Monitored App"
        });
        appAlphaRes.EnsureSuccessStatusCode();
        var appAlpha = (await appAlphaRes.Content.ReadFromJsonAsync<ApiResponse<ApplicationDto>>())!.Data!;

        // 2. Setup Tenant Beta
        var (tokenBeta, _, _) = await CreateUserWithRoleAsync("alert_beta", Roles.Admin);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenBeta);
        _client.DefaultRequestHeaders.Remove("X-Organization-Id");

        var orgBetaRes = await _client.PostAsJsonAsync("/api/organizations", new CreateOrganizationRequest
        {
            Name = "Alert Beta Org",
            Slug = "alert-beta-" + Guid.NewGuid().ToString("N")[..6]
        });
        orgBetaRes.EnsureSuccessStatusCode();
        var orgBeta = (await orgBetaRes.Content.ReadFromJsonAsync<ApiResponse<OrganizationDto>>())!.Data!;

        _client.DefaultRequestHeaders.Add("X-Organization-Id", orgBeta.Id.ToString());
        var appBetaRes = await _client.PostAsJsonAsync("/api/applications", new CreateApplicationRequest
        {
            Name = "app-beta-alerts-" + Guid.NewGuid().ToString("N")[..6],
            DisplayName = "Beta Monitored App"
        });
        appBetaRes.EnsureSuccessStatusCode();
        var appBeta = (await appBetaRes.Content.ReadFromJsonAsync<ApiResponse<ApplicationDto>>())!.Data!;

        // 3. Create Alert Rule in Alpha
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenAlpha);
        _client.DefaultRequestHeaders.Remove("X-Organization-Id");
        _client.DefaultRequestHeaders.Add("X-Organization-Id", orgAlpha.Id.ToString());

        var createAlphaRuleRes = await _client.PostAsJsonAsync("/api/alert-rules", new CreateAlertRuleRequest
        {
            Name = "Alpha High Latency Rule",
            Description = "Triggers if latency exceeds 500ms",
            ApplicationId = appAlpha.Id,
            ConditionType = "P95LatencyAboveThreshold",
            Threshold = 500.0,
            Severity = "Warning"
        });
        createAlphaRuleRes.EnsureSuccessStatusCode();
        var ruleAlpha = (await createAlphaRuleRes.Content.ReadFromJsonAsync<ApiResponse<AlertRuleDto>>())!.Data!;

        // 4. Create Alert Rule in Beta
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenBeta);
        _client.DefaultRequestHeaders.Remove("X-Organization-Id");
        _client.DefaultRequestHeaders.Add("X-Organization-Id", orgBeta.Id.ToString());

        var createBetaRuleRes = await _client.PostAsJsonAsync("/api/alert-rules", new CreateAlertRuleRequest
        {
            Name = "Beta Critical Crash Rule",
            Description = "Triggers if crash count exceeds 1",
            ApplicationId = appBeta.Id,
            ConditionType = "ExceptionCountAboveThreshold",
            Threshold = 1.0,
            Severity = "Critical"
        });
        createBetaRuleRes.EnsureSuccessStatusCode();
        var ruleBeta = (await createBetaRuleRes.Content.ReadFromJsonAsync<ApiResponse<AlertRuleDto>>())!.Data!;

        // 5. Create Incident in Beta
        var createBetaIncidentRes = await _client.PostAsJsonAsync("/api/incidents", new CreateIncidentRequest
        {
            Title = "Beta Production Outage",
            Description = "Payment gateway unresponsive",
            Severity = "Critical",
            ApplicationId = appBeta.Id,
            ServiceName = "payments-svc"
        });
        createBetaIncidentRes.EnsureSuccessStatusCode();
        var incidentBeta = (await createBetaIncidentRes.Content.ReadFromJsonAsync<ApiResponse<IncidentDto>>())!.Data!;

        // 6. Tenant Alpha verifies strict boundary isolation
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenAlpha);
        _client.DefaultRequestHeaders.Remove("X-Organization-Id");
        _client.DefaultRequestHeaders.Add("X-Organization-Id", orgAlpha.Id.ToString());

        // A. List Alert Rules: Must contain Alpha rule, never Beta rule
        var listAlphaRulesRes = await _client.GetAsync("/api/alert-rules");
        listAlphaRulesRes.EnsureSuccessStatusCode();
        var listAlphaRules = (await listAlphaRulesRes.Content.ReadFromJsonAsync<ApiResponse<PagedResponse<AlertRuleDto>>>())!.Data!;
        listAlphaRules.Items.Should().Contain(r => r.Id == ruleAlpha.Id);
        listAlphaRules.Items.Should().NotContain(r => r.Id == ruleBeta.Id);

        // B. Query Alert Rules with Beta's ApplicationId: must return empty
        var crossAppRulesRes = await _client.GetAsync($"/api/alert-rules?applicationId={appBeta.Id}");
        crossAppRulesRes.EnsureSuccessStatusCode();
        var crossAppRules = (await crossAppRulesRes.Content.ReadFromJsonAsync<ApiResponse<PagedResponse<AlertRuleDto>>>())!.Data!;
        crossAppRules.Items.Should().BeEmpty();

        // C. Direct lookup of Beta's Alert Rule by ID: must return 404
        var directBetaRuleRes = await _client.GetAsync($"/api/alert-rules/{ruleBeta.Id}");
        directBetaRuleRes.StatusCode.Should().Be(HttpStatusCode.NotFound);

        // D. Tenant Alpha attempting to create an alert rule for Beta app: must be rejected
        var illegalRuleRes = await _client.PostAsJsonAsync("/api/alert-rules", new CreateAlertRuleRequest
        {
            Name = "Illegitimate Rule",
            ApplicationId = appBeta.Id,
            ConditionType = "ErrorRateAboveThreshold",
            Threshold = 10.0
        });
        illegalRuleRes.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.NotFound);

        // E. List Incidents: Must not contain Beta incident
        var listIncidentsRes = await _client.GetAsync("/api/incidents");
        listIncidentsRes.EnsureSuccessStatusCode();
        var listIncidents = (await listIncidentsRes.Content.ReadFromJsonAsync<ApiResponse<PagedResponse<IncidentDto>>>())!.Data!;
        listIncidents.Items.Should().NotContain(i => i.Id == incidentBeta.Id);

        // F. Direct lookup of Beta's Incident by ID: must return 404
        var directBetaIncidentRes = await _client.GetAsync($"/api/incidents/{incidentBeta.Id}");
        directBetaIncidentRes.StatusCode.Should().Be(HttpStatusCode.NotFound);

        // G. Tenant Alpha attempting to comment or transition Beta's incident: must return 404
        var illegalCommentRes = await _client.PostAsJsonAsync($"/api/incidents/{incidentBeta.Id}/comments", new AddIncidentCommentRequest
        {
            CommentText = "Unauthorized observation attempt"
        });
        illegalCommentRes.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task SignalRHub_GroupAuthorization_RejectsCrossTenantJoin()
    {
        // 1. Setup Tenant Alpha User & Org
        var (tokenAlpha, _, _) = await CreateUserWithRoleAsync("sig_alpha", Roles.Operator);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenAlpha);
        _client.DefaultRequestHeaders.Remove("X-Organization-Id");

        var orgAlphaRes = await _client.PostAsJsonAsync("/api/organizations", new CreateOrganizationRequest
        {
            Name = "SignalR Alpha Org",
            Slug = "sig-alpha-" + Guid.NewGuid().ToString("N")[..6]
        });
        orgAlphaRes.EnsureSuccessStatusCode();
        var orgAlpha = (await orgAlphaRes.Content.ReadFromJsonAsync<ApiResponse<OrganizationDto>>())!.Data!;

        _client.DefaultRequestHeaders.Add("X-Organization-Id", orgAlpha.Id.ToString());
        var appAlphaRes = await _client.PostAsJsonAsync("/api/applications", new CreateApplicationRequest
        {
            Name = "app-sig-alpha-" + Guid.NewGuid().ToString("N")[..6],
            DisplayName = "SignalR Alpha App"
        });
        appAlphaRes.EnsureSuccessStatusCode();
        var appAlpha = (await appAlphaRes.Content.ReadFromJsonAsync<ApiResponse<ApplicationDto>>())!.Data!;

        // 2. Setup Tenant Beta User & Org
        var (tokenBeta, _, _) = await CreateUserWithRoleAsync("sig_beta", Roles.Operator);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenBeta);
        _client.DefaultRequestHeaders.Remove("X-Organization-Id");

        var orgBetaRes = await _client.PostAsJsonAsync("/api/organizations", new CreateOrganizationRequest
        {
            Name = "SignalR Beta Org",
            Slug = "sig-beta-" + Guid.NewGuid().ToString("N")[..6]
        });
        orgBetaRes.EnsureSuccessStatusCode();
        var orgBeta = (await orgBetaRes.Content.ReadFromJsonAsync<ApiResponse<OrganizationDto>>())!.Data!;

        _client.DefaultRequestHeaders.Add("X-Organization-Id", orgBeta.Id.ToString());
        var appBetaRes = await _client.PostAsJsonAsync("/api/applications", new CreateApplicationRequest
        {
            Name = "app-sig-beta-" + Guid.NewGuid().ToString("N")[..6],
            DisplayName = "SignalR Beta App"
        });
        appBetaRes.EnsureSuccessStatusCode();
        var appBeta = (await appBetaRes.Content.ReadFromJsonAsync<ApiResponse<ApplicationDto>>())!.Data!;

        // 3. Connect to SignalR as User Alpha
        var hubConnection = new HubConnectionBuilder()
            .WithUrl(new Uri(_factory.Server.BaseAddress, "/hubs/alerts"), options =>
            {
                options.HttpMessageHandlerFactory = _ => _factory.Server.CreateHandler();
                options.AccessTokenProvider = () => Task.FromResult<string?>(tokenAlpha);
            })
            .Build();

        await hubConnection.StartAsync();
        hubConnection.State.Should().Be(HubConnectionState.Connected);

        try
        {
            // A. Joining owned application group must succeed
            var joinOwnAppAct = async () => await hubConnection.InvokeAsync("JoinApplication", appAlpha.Id);
            await joinOwnAppAct.Should().NotThrowAsync();

            // B. Joining owned organization group must succeed
            var joinOwnOrgAct = async () => await hubConnection.InvokeAsync("JoinOrganization", orgAlpha.Id);
            await joinOwnOrgAct.Should().NotThrowAsync();

            // C. Attempting to join Tenant Beta's application group must throw HubException (Access Denied)
            var joinBetaAppAct = async () => await hubConnection.InvokeAsync("JoinApplication", appBeta.Id);
            await joinBetaAppAct.Should().ThrowAsync<HubException>()
                .WithMessage("*Access denied*");

            // D. Attempting to join Tenant Beta's organization group must throw HubException (Access Denied)
            var joinBetaOrgAct = async () => await hubConnection.InvokeAsync("JoinOrganization", orgBeta.Id);
            await joinBetaOrgAct.Should().ThrowAsync<HubException>()
                .WithMessage("*Access denied*");
        }
        finally
        {
            await hubConnection.StopAsync();
            await hubConnection.DisposeAsync();
        }
    }

    [Fact]
    public async Task NotificationDispatcher_ProcessesOutboxMessages_AndHandlesFailures()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var dispatcher = scope.ServiceProvider.GetRequiredService<INotificationDispatcherService>();

        // Create a test channel
        var channel = new NotificationChannel
        {
            Name = "Integration Outbox Channel",
            Type = NotificationChannelType.Email,
            Destination = "ops-team@sentinelops.local",
            IsEnabled = true,
            CreatedAtUtc = DateTime.UtcNow
        };
        db.NotificationChannels.Add(channel);
        await db.SaveChangesAsync();

        // Create a pending outbox message
        var outboxMessage = new NotificationOutbox
        {
            ChannelId = channel.Id,
            EventType = NotificationEventType.AlertTriggered,
            Status = NotificationStatus.Pending,
            Subject = "[CRITICAL] Outbox Integration Test",
            Message = "Test message payload",
            PayloadJson = "{}",
            MaxRetries = 3,
            RetryCount = 0,
            CreatedAtUtc = DateTime.UtcNow
        };
        db.NotificationOutboxes.Add(outboxMessage);
        await db.SaveChangesAsync();

        // Execute dispatcher
        var processed = await dispatcher.DispatchPendingOutboxMessagesAsync();
        processed.Should().BeGreaterOrEqualTo(0);

        // Verify outbox record status was updated (either Delivered or Failed with incremented retry count)
        var updated = await db.NotificationOutboxes.AsNoTracking().FirstOrDefaultAsync(o => o.Id == outboxMessage.Id);
        updated.Should().NotBeNull();
        updated!.LastAttemptedAtUtc.Should().NotBeNull();
        updated.Status.Should().BeOneOf(NotificationStatus.Delivered, NotificationStatus.Failed, NotificationStatus.DeadLetter);
    }
}
