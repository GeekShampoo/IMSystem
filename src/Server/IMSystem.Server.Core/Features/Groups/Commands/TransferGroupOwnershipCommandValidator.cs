using FluentValidation;
using System;

namespace IMSystem.Server.Core.Features.Groups.Commands;

public class TransferGroupOwnershipCommandValidator : AbstractValidator<TransferGroupOwnershipCommand>
{
    public TransferGroupOwnershipCommandValidator()
    {
        RuleFor(x => x.GroupId)
            .NotEmpty().WithMessage("Group ID cannot be empty.");

        RuleFor(x => x.CurrentOwnerId) // Corrected property name
            .NotEmpty().WithMessage("Current owner's User ID cannot be empty.");
        
        RuleFor(x => x.NewOwnerId) // Corrected property name
            .NotEmpty().WithMessage("New owner's User ID cannot be empty.");

        RuleFor(x => x)
            .Must(x => x.CurrentOwnerId != x.NewOwnerId) // Corrected property names
            .WithMessage("New owner cannot be the same as the current owner.");
    }
}