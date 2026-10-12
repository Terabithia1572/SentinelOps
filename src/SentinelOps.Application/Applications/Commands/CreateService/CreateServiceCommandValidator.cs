using FluentValidation;

namespace SentinelOps.Application.Applications.Commands.CreateService;

public class CreateServiceCommandValidator : AbstractValidator<CreateServiceCommand>
{
    public CreateServiceCommandValidator()
    {
        RuleFor(x => x.ApplicationId)
            .NotEmpty().WithMessage("Application Id is required.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Service name is required.")
            .MaximumLength(50).WithMessage("Service name must not exceed 50 characters.")
            .Matches("^[a-zA-Z0-9_-]+$").WithMessage("Service name can only contain alphanumeric characters, underscores, and hyphens.");

        RuleFor(x => x.DisplayName)
            .NotEmpty().WithMessage("Display name is required.")
            .MaximumLength(100).WithMessage("Display name must not exceed 100 characters.");
    }
}
