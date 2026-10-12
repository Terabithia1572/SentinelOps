using FluentAssertions;
using SentinelOps.Domain.Entities;
using Xunit;

namespace SentinelOps.Domain.Tests.Entities;

public class ApiKeyTests
{
    [Fact]
    public void ApiKey_WhenCreatedWithFutureExpiration_IsActiveShouldBeTrue()
    {
        var key = new ApiKey
        {
            ApplicationId = Guid.NewGuid(),
            Name = "Production Key",
            KeyPrefix = "sent_live_abc12345",
            KeyHash = "somehash",
            ExpiresAtUtc = DateTime.UtcNow.AddDays(30)
        };

        key.IsActive.Should().BeTrue();
    }

    [Fact]
    public void ApiKey_WhenExpired_IsActiveShouldBeFalse()
    {
        var key = new ApiKey
        {
            ApplicationId = Guid.NewGuid(),
            Name = "Expired Key",
            KeyPrefix = "sent_live_abc12345",
            KeyHash = "somehash",
            ExpiresAtUtc = DateTime.UtcNow.AddDays(-1)
        };

        key.IsActive.Should().BeFalse();
    }

    [Fact]
    public void ApiKey_WhenRevoked_IsActiveShouldBeFalse()
    {
        var key = new ApiKey
        {
            ApplicationId = Guid.NewGuid(),
            Name = "Revoked Key",
            KeyPrefix = "sent_live_abc12345",
            KeyHash = "somehash"
        };

        key.Revoke();

        key.IsActive.Should().BeFalse();
        key.RevokedAtUtc.Should().NotBeNull();
    }
}
