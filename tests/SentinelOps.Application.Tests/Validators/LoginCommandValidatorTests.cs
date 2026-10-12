using FluentAssertions;
using SentinelOps.Application.Identity.Commands.Login;
using Xunit;

namespace SentinelOps.Application.Tests.Validators;

public class LoginCommandValidatorTests
{
    private readonly LoginCommandValidator _validator = new();

    [Fact]
    public void Validate_WhenCommandIsValid_ShouldNotHaveValidationError()
    {
        var command = new LoginCommand("admin@sentinelops.io", "Pass1234!");
        var result = _validator.Validate(command);
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Validate_WhenEmailOrUserNameIsEmpty_ShouldHaveValidationError(string? input)
    {
        var command = new LoginCommand(input!, "Pass1234!");
        var result = _validator.Validate(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(LoginCommand.EmailOrUserName));
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Validate_WhenPasswordIsEmpty_ShouldHaveValidationError(string? password)
    {
        var command = new LoginCommand("admin", password!);
        var result = _validator.Validate(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(LoginCommand.Password));
    }
}
