using FluentValidation;
using System;

namespace IMSystem.Server.Core.Features.Groups.Commands;

public class LeaveGroupCommandValidator : AbstractValidator<LeaveGroupCommand>
{
    public LeaveGroupCommandValidator()
    {
        RuleFor(x => x.GroupId)
            .NotEmpty().WithMessage("Group ID cannot be empty.");

        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("User ID cannot be empty.");
    }
}