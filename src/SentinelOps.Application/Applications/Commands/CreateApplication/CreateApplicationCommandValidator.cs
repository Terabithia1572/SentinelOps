using FluentValidation;

namespace SentinelOps.Application.Applications.Commands.CreateApplication;

public class CreateApplicationCommandValidator : AbstractValidator<CreateApplicationCommand>
{
    public CreateApplicationCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required.")
            .MaximumLength(100).WithMessage("Name must not exceed 100 characters.")
            .Matches("^[a-zA-Z0-9_-]+$").WithMessage("Name can only contain alphanumeric characters, underscores, and hyphens.");

        RuleFor(x => x.DisplayName)
            .NotEmpty().WithMessage("DisplayName is required.")
            .MaximumLength(150).WithMessage("DisplayName must not exceed 150 characters.");

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("Description must not exceed 500 characters.");
    }
}
