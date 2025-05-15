using FluentValidation;

namespace IMSystem.Server.Core.Features.User.Commands;

public class VerifyEmailCommandValidator : AbstractValidator<VerifyEmailCommand>
{
    public VerifyEmailCommandValidator()
    {
        RuleFor(x => x.Token)
            .NotEmpty().WithMessage("Verification token is required.")
            .Length(20, 200).WithMessage("Verification token has an invalid length."); // Assuming token length constraints
    }
}