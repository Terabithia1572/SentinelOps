using FluentAssertions;
using SentinelOps.Application.Identity.Commands.Register;
using Xunit;

namespace SentinelOps.Application.Tests.Validators;

public class RegisterCommandValidatorTests
{
    private readonly RegisterCommandValidator _validator = new();

    [Fact]
    public void Validate_WhenCommandIsValid_ShouldNotHaveValidationError()
    {
        var command = new RegisterCommand(
            "validuser",
            "user@sentinelops.io",
            "Valid User",
            "Str0ng!Pass#2026",
            "Str0ng!Pass#2026",
            "Viewer");

        var result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("ab")]
    [InlineData("user name with spaces")]
    public void Validate_WhenUserNameIsInvalid_ShouldHaveValidationError(string userName)
    {
        var command = new RegisterCommand(
            userName,
            "user@sentinelops.io",
            "Valid User",
            "Str0ng!Pass#2026",
            "Str0ng!Pass#2026");

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(RegisterCommand.UserName));
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-an-email")]
    public void Validate_WhenEmailIsInvalid_ShouldHaveValidationError(string email)
    {
        var command = new RegisterCommand(
            "validuser",
            email,
            "Valid User",
            "Str0ng!Pass#2026",
            "Str0ng!Pass#2026");

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(RegisterCommand.Email));
    }

    [Fact]
    public void Validate_WhenPasswordsDoNotMatch_ShouldHaveValidationError()
    {
        var command = new RegisterCommand(
            "validuser",
            "user@sentinelops.io",
            "Valid User",
            "Str0ng!Pass#2026",
            "DifferentPassword123!");

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(RegisterCommand.ConfirmPassword));
    }

    [Theory]
    [InlineData("short1!")] // < 8 chars
    [InlineData("alllowercase1!")] // No uppercase
    [InlineData("ALLUPPERCASE1!")] // No lowercase
    [InlineData("NoDigitsInPassword!")] // No digit
    [InlineData("NoSpecialChars1234")] // No special
    public void Validate_WhenPasswordFailsComplexity_ShouldHaveValidationError(string weakPassword)
    {
        var command = new RegisterCommand(
            "validuser",
            "user@sentinelops.io",
            "Valid User",
            weakPassword,
            weakPassword);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(RegisterCommand.Password));
    }
}
