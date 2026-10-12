using FluentAssertions;
using FluentValidation.TestHelper;
using SentinelOps.Application.Applications.Commands.CreateApplication;
using Xunit;

namespace SentinelOps.Application.Tests.Validators;

public class CreateApplicationCommandValidatorTests
{
    private readonly CreateApplicationCommandValidator _validator = new();

    [Fact]
    public void Validate_WhenCommandIsValid_ShouldNotHaveValidationError()
    {
        // Arrange
        var command = new CreateApplicationCommand
        {
            Name = "order-service",
            DisplayName = "Order Processing Service",
            Description = "Handles customer orders and checkouts"
        };

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_WhenNameIsEmpty_ShouldHaveValidationError(string invalidName)
    {
        // Arrange
        var command = new CreateApplicationCommand
        {
            Name = invalidName,
            DisplayName = "Test App",
            Description = "Valid Description"
        };

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void Validate_WhenNameContainsInvalidCharacters_ShouldHaveValidationError()
    {
        // Arrange
        var command = new CreateApplicationCommand
        {
            Name = "invalid name with spaces!",
            DisplayName = "Test App",
            Description = "Valid Description"
        };

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Name)
            .WithErrorMessage("Name can only contain alphanumeric characters, underscores, and hyphens.");
    }

    [Fact]
    public void Validate_WhenDisplayNameIsEmpty_ShouldHaveValidationError()
    {
        // Arrange
        var command = new CreateApplicationCommand
        {
            Name = "valid-name",
            DisplayName = "",
            Description = "Valid Description"
        };

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.DisplayName)
            .WithErrorMessage("DisplayName is required.");
    }
}
