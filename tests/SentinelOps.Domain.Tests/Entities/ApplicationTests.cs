using FluentAssertions;
using SentinelOps.Domain.Entities;
using Xunit;
using Environment = SentinelOps.Domain.Entities.Environment;

namespace SentinelOps.Domain.Tests.Entities;

public class ApplicationTests
{
    [Fact]
    public void Application_WhenInstantiated_ShouldHaveDefaultValues()
    {
        // Act
        var app = new Application();

        // Assert
        app.Id.Should().NotBeEmpty();
        app.IsActive.Should().BeTrue();
        app.CreatedAtUtc.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        app.Environments.Should().BeEmpty();
        app.Services.Should().BeEmpty();
        app.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void Application_WhenPropertiesSet_ShouldRetainValues()
    {
        // Arrange
        var app = new Application
        {
            Name = "payments-api",
            DisplayName = "Payments API",
            Description = "Processes billing and payments",
            ApiKeyHash = "hash-123456"
        };

        // Assert
        app.Name.Should().Be("payments-api");
        app.DisplayName.Should().Be("Payments API");
        app.Description.Should().Be("Processes billing and payments");
        app.ApiKeyHash.Should().Be("hash-123456");
    }

    [Fact]
    public void Application_WhenAddingEnvironmentsAndServices_ShouldContainThem()
    {
        // Arrange
        var app = new Application
        {
            Name = "auth-service",
            DisplayName = "Auth Service"
        };

        var env = new Environment
        {
            Name = "production",
            ApplicationId = app.Id
        };

        var service = new Service
        {
            Name = "auth-worker",
            DisplayName = "Auth Background Worker",
            ApplicationId = app.Id
        };

        // Act
        app.Environments.Add(env);
        app.Services.Add(service);

        // Assert
        app.Environments.Should().ContainSingle().Which.Name.Should().Be("production");
        app.Services.Should().ContainSingle().Which.Name.Should().Be("auth-worker");
    }
}
