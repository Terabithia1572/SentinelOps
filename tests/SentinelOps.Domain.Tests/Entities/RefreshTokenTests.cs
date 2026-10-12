using FluentAssertions;
using SentinelOps.Domain.Entities;
using Xunit;

namespace SentinelOps.Domain.Tests.Entities;

public class RefreshTokenTests
{
    [Fact]
    public void RefreshToken_WhenFresh_IsActiveShouldBeTrue()
    {
        var token = new RefreshToken
        {
            UserId = "user-123",
            TokenHash = "testhash",
            ExpiresAtUtc = DateTime.UtcNow.AddDays(7),
            CreatedByIp = "127.0.0.1"
        };

        token.IsActive.Should().BeTrue();
        token.IsExpired.Should().BeFalse();
        token.IsRevoked.Should().BeFalse();
    }

    [Fact]
    public void RefreshToken_WhenExpired_IsActiveShouldBeFalse()
    {
        var token = new RefreshToken
        {
            UserId = "user-123",
            TokenHash = "testhash",
            ExpiresAtUtc = DateTime.UtcNow.AddMinutes(-10),
            CreatedByIp = "127.0.0.1"
        };

        token.IsActive.Should().BeFalse();
        token.IsExpired.Should().BeTrue();
    }

    [Fact]
    public void RefreshToken_WhenRevokedWithReplacement_ShouldTrackAuditDetails()
    {
        var token = new RefreshToken
        {
            UserId = "user-123",
            TokenHash = "testhash",
            ExpiresAtUtc = DateTime.UtcNow.AddDays(7),
            CreatedByIp = "127.0.0.1"
        };

        token.Revoke("192.168.1.100", "newhash123");

        token.IsActive.Should().BeFalse();
        token.IsRevoked.Should().BeTrue();
        token.RevokedByIp.Should().Be("192.168.1.100");
        token.ReplacedByTokenHash.Should().Be("newhash123");
        token.RevokedAtUtc.Should().NotBeNull();
    }
}
