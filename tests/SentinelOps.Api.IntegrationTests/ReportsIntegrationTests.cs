using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
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

public class ReportsIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public ReportsIntegrationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private async Task<string> GetViewerTokenAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var tokenService = scope.ServiceProvider.GetRequiredService<ITokenService>();

        if (!await roleManager.RoleExistsAsync(Roles.Viewer))
        {
            await roleManager.CreateAsync(new IdentityRole(Roles.Viewer));
        }

        var unique = Guid.NewGuid().ToString("N")[..8];
        var user = new ApplicationUser
        {
            UserName = $"rep_user_{unique}",
            Email = $"rep_user_{unique}@sentinelops.local",
            FullName = "Report Viewer User",
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow
        };

        var result = await userManager.CreateAsync(user, "P@ssword2026!Secure");
        result.Succeeded.Should().BeTrue();

        await userManager.AddToRoleAsync(user, Roles.Viewer);

        var (token, _) = await tokenService.GenerateAccessTokenAsync(user.Id, user.UserName, user.Email, [Roles.Viewer]);
        return token;
    }

    [Fact]
    public async Task GetReportTypes_ReturnsCanonicalReportTypes()
    {
        var token = await GetViewerTokenAsync();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/reports/types");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<ApiResponse<List<ReportTypeOptionDto>>>();
        body.Should().NotBeNull();
        body!.Data.Should().NotBeEmpty();

        var ids = body.Data!.Select(t => t.Id).ToList();
        ids.Should().Contain("ExecutiveSummary");
        ids.Should().Contain("IncidentPostmortem");
        ids.Should().Contain("SloCompliance");
        ids.Should().Contain("InfrastructureHealth");
        ids.Should().Contain("SecurityAudit");
        ids.Should().Contain("TelemetryVolume");
    }

    [Fact]
    public async Task PreviewReport_ForExecutiveSummary_ReturnsSummaryMetricsAndColumns()
    {
        var token = await GetViewerTokenAsync();
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/reports/preview")
        {
            Headers = { Authorization = new AuthenticationHeaderValue("Bearer", token) },
            Content = JsonContent.Create(new GenerateReportRequest
            {
                ReportType = "ExecutiveSummary",
                TimeRange = "24h"
            })
        };

        var response = await _client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<ApiResponse<ReportPreviewDto>>();
        body.Should().NotBeNull();
        body!.Data!.ReportType.Should().Be("ExecutiveSummary");
        body.Data.SummaryMetrics.Should().ContainKey("SLO Compliance");
        body.Data.SummaryMetrics.Should().ContainKey("Open Incidents");
        body.Data.ColumnHeaders.Should().NotBeEmpty();
    }

    [Fact]
    public async Task ExportPdf_ViaHttpPost_ReturnsValidPdfBytes()
    {
        var token = await GetViewerTokenAsync();
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/reports/export/pdf")
        {
            Headers = { Authorization = new AuthenticationHeaderValue("Bearer", token) },
            Content = JsonContent.Create(new ExportReportRequest
            {
                ReportType = "ExecutiveSummary",
                TimeRange = "24h"
            })
        };

        var response = await _client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/pdf");

        var bytes = await response.Content.ReadAsByteArrayAsync();
        bytes.Should().NotBeEmpty();
        bytes.Length.Should().BeGreaterThan(500);

        // Verify PDF Header magic bytes (%PDF-1.4)
        string pdfHeader = Encoding.ASCII.GetString(bytes[..8]);
        pdfHeader.Should().StartWith("%PDF-");

        // Verify PDF EOF marker
        string pdfEnd = Encoding.ASCII.GetString(bytes[^64..]);
        pdfEnd.Should().Contain("%%EOF");
    }

    [Fact]
    public async Task ExportPdf_ViaHttpGet_ReturnsValidPdfBytes()
    {
        var token = await GetViewerTokenAsync();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/reports/export/pdf?type=SecurityAudit&timeRange=7d");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/pdf");

        var bytes = await response.Content.ReadAsByteArrayAsync();
        bytes.Should().NotBeEmpty();
        string pdfHeader = Encoding.ASCII.GetString(bytes[..8]);
        pdfHeader.Should().StartWith("%PDF-");
    }

    [Theory]
    [InlineData("ExecutiveSummary")]
    [InlineData("IncidentPostmortem")]
    [InlineData("SloCompliance")]
    [InlineData("InfrastructureHealth")]
    [InlineData("SecurityAudit")]
    [InlineData("TelemetryVolume")]
    public async Task ExportPdf_ForAllReportTypes_GeneratesValidPdfs(string reportType)
    {
        var token = await GetViewerTokenAsync();
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/reports/export/pdf")
        {
            Headers = { Authorization = new AuthenticationHeaderValue("Bearer", token) },
            Content = JsonContent.Create(new ExportReportRequest
            {
                ReportType = reportType,
                TimeRange = "24h"
            })
        };

        var response = await _client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/pdf");

        var bytes = await response.Content.ReadAsByteArrayAsync();
        bytes.Length.Should().BeGreaterThan(300);

        string header = Encoding.ASCII.GetString(bytes[..8]);
        header.Should().StartWith("%PDF-");
    }
}
