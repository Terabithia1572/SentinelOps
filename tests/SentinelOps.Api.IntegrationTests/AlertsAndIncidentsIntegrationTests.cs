using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using SentinelOps.Application.Common.Interfaces;
using SentinelOps.Contracts.DTOs;
using SentinelOps.Contracts.Requests;
using SentinelOps.Contracts.Responses;
using SentinelOps.Domain.Entities;
using Xunit;

namespace SentinelOps.Api.IntegrationTests;

public class AlertsAndIncidentsIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory _factory;

    public AlertsAndIncidentsIntegrationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private async Task AuthenticateAsync(string role = "Operator")
    {
        var email = $"alert_test_{Guid.NewGuid():N}@sentinelops.io";
        var userName = $"usr_{Guid.NewGuid():N}"[..12];
        const string password = "P@ssword2026!Secure";

        using (var scope = _factory.Services.CreateScope())
        {
            var identityService = scope.ServiceProvider.GetRequiredService<IIdentityService>();
            await identityService.CreateUserAsync(userName, email, "Alert Test Operator", password, role);
        }

        var response = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequest
        {
            Email = email,
            Password = password
        });

        var auth = await response.Content.ReadFromJsonAsync<ApiResponse<AuthResponse>>();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth!.Data!.Token);
    }

    private async Task<Guid> CreateTestApplicationAsync()
    {
        var appId = Guid.NewGuid();
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();

        var app = new Domain.Entities.Application
        {
            Name = $"alert-app-{Guid.NewGuid():N}"[..18],
            DisplayName = "Alerting Test Application",
            Description = "Test App for Alerting & Incidents",
            IsActive = true
        };
        context.Applications.Add(app);
        await context.SaveChangesAsync();
        return app.Id;
    }

    [Fact]
    public async Task UnauthenticatedRequests_MustReturnUnauthorized()
    {
        var unauthedClient = _factory.CreateClient();

        var rulesRes = await unauthedClient.GetAsync("/api/alert-rules");
        rulesRes.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var alertsRes = await unauthedClient.GetAsync("/api/alerts");
        alertsRes.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var incidentsRes = await unauthedClient.GetAsync("/api/incidents");
        incidentsRes.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var channelsRes = await unauthedClient.GetAsync("/api/notification-channels");
        channelsRes.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var slosRes = await unauthedClient.GetAsync("/api/slos");
        slosRes.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task AlertRules_FullLifecycle_Succeeds()
    {
        await AuthenticateAsync("Operator");
        var appId = await CreateTestApplicationAsync();

        // 1. Create Alert Rule
        var createReq = new CreateAlertRuleRequest
        {
            Name = "Order Error Rate Alert",
            Description = "Triggers when error rate exceeds 5%",
            ApplicationId = appId,
            ServiceName = "order-service",
            ConditionType = "ErrorRateAboveThreshold",
            Operator = "GreaterThan",
            MetricName = "error_rate",
            Threshold = 5.0,
            EvaluationWindowMinutes = 5,
            EvaluationFrequencyMinutes = 1,
            ConsecutiveCountRequired = 1,
            CooldownMinutes = 10,
            Severity = "Critical"
        };

        var createRes = await _client.PostAsJsonAsync("/api/alert-rules", createReq);
        createRes.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await createRes.Content.ReadFromJsonAsync<ApiResponse<AlertRuleDto>>();
        created!.Success.Should().BeTrue();
        var ruleId = created.Data!.Id;

        // 2. Get Alert Rule By ID
        var getRes = await _client.GetAsync($"/api/alert-rules/{ruleId}");
        getRes.StatusCode.Should().Be(HttpStatusCode.OK);
        var rule = await getRes.Content.ReadFromJsonAsync<ApiResponse<AlertRuleDto>>();
        rule!.Data!.Name.Should().Be("Order Error Rate Alert");
        rule.Data.Severity.Should().Be("Critical");

        // 3. Disable & Enable Rule
        var disableRes = await _client.PostAsync($"/api/alert-rules/{ruleId}/disable", null);
        disableRes.StatusCode.Should().Be(HttpStatusCode.OK);

        var enableRes = await _client.PostAsync($"/api/alert-rules/{ruleId}/enable", null);
        enableRes.StatusCode.Should().Be(HttpStatusCode.OK);

        // 4. Evaluate Rule
        var evalRes = await _client.PostAsync($"/api/alert-rules/{ruleId}/evaluate", null);
        evalRes.StatusCode.Should().Be(HttpStatusCode.OK);

        // 5. Delete Rule
        var deleteRes = await _client.DeleteAsync($"/api/alert-rules/{ruleId}");
        deleteRes.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Incidents_LifecycleAndComments_Succeeds()
    {
        await AuthenticateAsync("Operator");
        var appId = await CreateTestApplicationAsync();

        // 1. Create Incident
        var createReq = new CreateIncidentRequest
        {
            Title = "Database Outage in Billing Service",
            Description = "PostgreSQL pool connections timed out",
            Severity = "Critical",
            ApplicationId = appId,
            ServiceName = "billing-service",
            EnvironmentName = "Production"
        };

        var createRes = await _client.PostAsJsonAsync("/api/incidents", createReq);
        createRes.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await createRes.Content.ReadFromJsonAsync<ApiResponse<IncidentDto>>();
        created!.Success.Should().BeTrue();
        var incidentId = created.Data!.Id;
        created.Data.IncidentNumber.Should().StartWith("INC-");

        // 2. Assign Incident
        var assignRes = await _client.PostAsJsonAsync($"/api/incidents/{incidentId}/assign", new AssignIncidentRequest
        {
            UserId = "user_42",
            UserName = "Dave OnCall"
        });
        assignRes.StatusCode.Should().Be(HttpStatusCode.OK);

        // 3. Transition to Investigating
        var transitionRes = await _client.PostAsJsonAsync($"/api/incidents/{incidentId}/transition", new TransitionIncidentRequest
        {
            TargetStatus = "Investigating"
        });
        transitionRes.StatusCode.Should().Be(HttpStatusCode.OK);

        // 4. Add Comment
        var commentRes = await _client.PostAsJsonAsync($"/api/incidents/{incidentId}/comments", new AddIncidentCommentRequest
        {
            CommentText = "Restarted idle connection reaper on database cluster."
        });
        commentRes.StatusCode.Should().Be(HttpStatusCode.OK);

        // 5. Resolve Incident
        var resolveRes = await _client.PostAsJsonAsync($"/api/incidents/{incidentId}/transition", new TransitionIncidentRequest
        {
            TargetStatus = "Resolved",
            RootCause = "Stale pool connections blocked new queries",
            ResolutionNotes = "Cleaned up zombie connections and increased max pool size."
        });
        resolveRes.StatusCode.Should().Be(HttpStatusCode.OK);

        // 6. Verify Incident Details
        var detailRes = await _client.GetAsync($"/api/incidents/{incidentId}");
        detailRes.StatusCode.Should().Be(HttpStatusCode.OK);
        var detail = await detailRes.Content.ReadFromJsonAsync<ApiResponse<IncidentDetailDto>>();
        detail!.Data!.Status.Should().Be("Resolved");
        detail.Data.RootCauseSummary.Should().NotBeNullOrEmpty();
        detail.Data.Comments.Should().ContainSingle();
        detail.Data.TimelineEvents.Should().HaveCountGreaterOrEqualTo(3);
    }

    [Fact]
    public async Task NotificationChannels_SsrfProtection_RejectsRestrictedDestinations()
    {
        await AuthenticateAsync("Operator");

        // Loopback destination
        var loopbackReq = new CreateNotificationChannelRequest
        {
            Name = "Local Hook",
            Type = "Webhook",
            Destination = "http://localhost:8080/webhook"
        };
        var loopbackRes = await _client.PostAsJsonAsync("/api/notification-channels", loopbackReq);
        loopbackRes.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        // Cloud metadata destination
        var metadataReq = new CreateNotificationChannelRequest
        {
            Name = "Metadata Exploit",
            Type = "Webhook",
            Destination = "http://169.254.169.254/latest/meta-data"
        };
        var metadataRes = await _client.PostAsJsonAsync("/api/notification-channels", metadataReq);
        metadataRes.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ServiceLevelObjectives_FullLifecycle_Succeeds()
    {
        await AuthenticateAsync("Operator");
        var appId = await CreateTestApplicationAsync();

        // 1. Create SLO
        var createReq = new CreateSloRequest
        {
            Name = "Order Service 99.9% Availability",
            Description = "Availability objective over rolling 7 days",
            ApplicationId = appId,
            ServiceName = "order-service",
            IndicatorType = "Availability",
            TargetPercentage = 99.9,
            EvaluationWindow = "Rolling7Days"
        };

        var createRes = await _client.PostAsJsonAsync("/api/slos", createReq);
        createRes.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await createRes.Content.ReadFromJsonAsync<ApiResponse<SloDto>>();
        created!.Success.Should().BeTrue();
        var sloId = created.Data!.Id;

        // 2. Evaluate SLO
        var evalRes = await _client.PostAsync($"/api/slos/{sloId}/evaluate", null);
        evalRes.StatusCode.Should().Be(HttpStatusCode.OK);

        // 3. Query Compliance
        var compRes = await _client.GetAsync($"/api/slos/{sloId}/compliance");
        compRes.StatusCode.Should().Be(HttpStatusCode.OK);
        var comp = await compRes.Content.ReadFromJsonAsync<ApiResponse<SloComplianceHistoryDto>>();
        comp!.Data!.TargetPercentage.Should().Be(99.9);
        comp.Data.CurrentCompliance.Should().BeGreaterThan(0.0);
    }
}
