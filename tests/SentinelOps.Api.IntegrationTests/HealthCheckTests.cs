using System.Net;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using StackExchange.Redis;
using Xunit;

namespace SentinelOps.Api.IntegrationTests;

public class HealthCheckTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public HealthCheckTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Ensure Redis doesn't attempt network connection in test environment
                var redisDescriptor = services.FirstOrDefault(d => d.ServiceType == typeof(IConnectionMultiplexer));
                if (redisDescriptor != null)
                {
                    services.Remove(redisDescriptor);
                    var mockRedis = Substitute.For<IConnectionMultiplexer>();
                    var mockDb = Substitute.For<IDatabase>();
                    mockRedis.GetDatabase(Arg.Any<int>(), Arg.Any<object>()).Returns(mockDb);
                    services.AddSingleton(mockRedis);
                }
            });
        });
    }

    [Fact]
    public async Task Get_HealthLive_ReturnsOk()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/health/live");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Get_ApiHealth_ReturnsOkWithOperationalStatus()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/health");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("System is operational");
    }
}
