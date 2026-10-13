using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using NSubstitute;
using SentinelOps.Application.Common.Interfaces;
using SentinelOps.Domain.Entities;
using SentinelOps.Infrastructure.Services;
using Xunit;

namespace SentinelOps.Application.Tests.Services;

public class AuditServiceSanitizationTests
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<AuditService> _logger;
    private readonly AuditService _auditService;

    public AuditServiceSanitizationTests()
    {
        _context = Substitute.For<IApplicationDbContext>();
        _currentUserService = Substitute.For<ICurrentUserService>();
        _httpContextAccessor = Substitute.For<IHttpContextAccessor>();
        _logger = Substitute.For<ILogger<AuditService>>();

        _currentUserService.UserId.Returns("admin-1");
        _currentUserService.UserName.Returns("admin");

        _auditService = new AuditService(_context, _currentUserService, _httpContextAccessor, _logger);
    }

    [Fact]
    public async Task RecordAsync_WhenMetadataContainsSensitiveCredentials_ShouldRedactThem()
    {
        AuditEvent? capturedEvent = null;
        _context.AuditEvents.Add(Arg.Do<AuditEvent>(e => capturedEvent = e));

        var sensitivePayload = new
        {
            UserName = "johndoe",
            Password = "SuperSecretPassword123!",
            ApiKey = "sent_live_abcdef123456",
            AccessToken = "eyJhbGciOi...",
            Details = "User password changed"
        };

        await _auditService.RecordAsync(
            action: "User.PasswordUpdated",
            targetEntityType: "User",
            targetEntityId: "user-123",
            outcome: "Success",
            metadata: sensitivePayload);

        capturedEvent.Should().NotBeNull();
        capturedEvent!.MetadataJson.Should().NotBeNull();
        capturedEvent.MetadataJson.Should().NotContain("SuperSecretPassword123!");
        capturedEvent.MetadataJson.Should().NotContain("sent_live_abcdef123456");
        capturedEvent.MetadataJson.Should().NotContain("eyJhbGciOi...");
        capturedEvent.MetadataJson.Should().Contain("[REDACTED]");
    }
}
