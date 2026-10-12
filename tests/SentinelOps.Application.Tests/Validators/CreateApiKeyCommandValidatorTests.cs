using FluentAssertions;
using SentinelOps.Application.ApiKeys.Commands.CreateApiKey;
using Xunit;

namespace SentinelOps.Application.Tests.Validators;

public class CreateApiKeyCommandValidatorTests
{
    private readonly CreateApiKeyCommandValidator _validator = new();

    [Fact]
    public void Validate_WhenCommandIsValid_ShouldNotHaveValidationError()
    {
        var command = new CreateApiKeyCommand(Guid.NewGuid(), "Production Ingestion Key", 90);
        var result = _validator.Validate(command);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WhenApplicationIdIsEmpty_ShouldHaveValidationError()
    {
        var command = new CreateApiKeyCommand(Guid.Empty, "Key 1");
        var result = _validator.Validate(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateApiKeyCommand.ApplicationId));
    }

    [Fact]
    public void Validate_WhenNameIsEmpty_ShouldHaveValidationError()
    {
        var command = new CreateApiKeyCommand(Guid.NewGuid(), "");
        var result = _validator.Validate(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateApiKeyCommand.Name));
    }

    [Fact]
    public void Validate_WhenExpiresInDaysIsZeroOrNegative_ShouldHaveValidationError()
    {
        var command = new CreateApiKeyCommand(Guid.NewGuid(), "Key 1", 0);
        var result = _validator.Validate(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateApiKeyCommand.ExpiresInDays));
    }
}
