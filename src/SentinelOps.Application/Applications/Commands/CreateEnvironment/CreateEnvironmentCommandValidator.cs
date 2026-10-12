using FluentValidation;

namespace SentinelOps.Application.Applications.Commands.CreateEnvironment;

public class CreateEnvironmentCommandValidator : AbstractValidator<CreateEnvironmentCommand>
{
    public CreateEnvironmentCommandValidator()
    {
        RuleFor(x => x.ApplicationId)
            .NotEmpty().WithMessage("Application Id is required.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Environment name is required.")
            .MaximumLength(50).WithMessage("Environment name must not exceed 50 characters.")
            .Matches("^[a-zA-Z0-9_-]+$").WithMessage("Environment name can only contain alphanumeric characters, underscores, and hyphens.");
    }
}
