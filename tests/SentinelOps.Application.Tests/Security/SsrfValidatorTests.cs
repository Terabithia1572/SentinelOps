using System.Net;
using FluentAssertions;
using SentinelOps.Infrastructure.Security;
using Xunit;

namespace SentinelOps.Application.Tests.Security;

public class SsrfValidatorTests
{
    [Theory]
    [InlineData("http://localhost:8080/hook")]
    [InlineData("http://127.0.0.1/api")]
    [InlineData("http://169.254.169.254/latest/meta-data/")]
    [InlineData("http://10.1.2.3/notify")]
    [InlineData("http://192.168.1.50:9000/")]
    [InlineData("http://172.16.5.10/webhook")]
    [InlineData("file:///etc/passwd")]
    [InlineData("gopher://127.0.0.1:6379/")]
    public async Task ValidateDestinationUrlAsync_WithRestrictedTargets_BlocksRequest(string url)
    {
        // Act
        var (isAllowed, reason) = await SsrfValidator.ValidateDestinationUrlAsync(url);

        // Assert
        isAllowed.Should().BeFalse();
        reason.Should().NotBeNullOrWhiteSpace();
    }

    [Theory]
    [InlineData("10.0.0.1", true)]
    [InlineData("172.16.0.1", true)]
    [InlineData("172.31.255.255", true)]
    [InlineData("192.168.0.1", true)]
    [InlineData("169.254.169.254", true)]
    [InlineData("127.0.0.1", true)]
    [InlineData("8.8.8.8", false)]
    [InlineData("1.1.1.1", false)]
    public void IsPrivateOrRestrictedIp_EvaluatesCorrectly(string ipString, bool expectedRestricted)
    {
        // Arrange
        var ip = IPAddress.Parse(ipString);

        // Act
        bool isRestricted = SsrfValidator.IsPrivateOrRestrictedIp(ip);

        // Assert
        isRestricted.Should().Be(expectedRestricted);
    }
}
