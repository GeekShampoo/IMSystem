using FluentValidation;

namespace IMSystem.Server.Core.Features.Messages.Commands
{
    /// <summary>
    /// Validator for the <see cref="EditMessageCommand"/>.
    /// </summary>
    public class EditMessageCommandValidator : AbstractValidator<EditMessageCommand>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EditMessageCommandValidator"/> class.
        /// </summary>
        public EditMessageCommandValidator()
        {
            RuleFor(v => v.MessageId)
                .NotEmpty().WithMessage("Message ID is required.");

            RuleFor(v => v.NewContent)
                .NotEmpty().WithMessage("New content cannot be empty.")
                .MaximumLength(2000).WithMessage("Message content cannot exceed 2000 characters."); // Example length, adjust as needed

            RuleFor(v => v.UserId)
                .NotEmpty().WithMessage("User ID is required.");
        }
    }
}