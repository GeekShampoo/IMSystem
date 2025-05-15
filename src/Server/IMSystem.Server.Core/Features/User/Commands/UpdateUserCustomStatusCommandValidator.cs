using FluentValidation;

namespace IMSystem.Server.Core.Features.User.Commands;

public class UpdateUserCustomStatusCommandValidator : AbstractValidator<UpdateUserCustomStatusCommand>
{
    public UpdateUserCustomStatusCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("User ID is required.");

        RuleFor(x => x.CustomStatus)
            .MaximumLength(100).WithMessage("Custom status cannot exceed 100 characters.");
        // CustomStatus can be null or empty, so no NotEmpty() rule unless business logic dictates otherwise.
    }
}