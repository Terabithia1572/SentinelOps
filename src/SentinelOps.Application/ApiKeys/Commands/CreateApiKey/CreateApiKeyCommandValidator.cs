using FluentValidation;

namespace SentinelOps.Application.ApiKeys.Commands.CreateApiKey;

public class CreateApiKeyCommandValidator : AbstractValidator<CreateApiKeyCommand>
{
    public CreateApiKeyCommandValidator()
    {
        RuleFor(x => x.ApplicationId)
            .NotEmpty().WithMessage("Application Id is required.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("API Key name is required.")
            .MaximumLength(100).WithMessage("API Key name must not exceed 100 characters.");

        RuleFor(x => x.ExpiresInDays)
            .GreaterThan(0).When(x => x.ExpiresInDays.HasValue)
            .WithMessage("Expiration days must be greater than zero.");
    }
}
