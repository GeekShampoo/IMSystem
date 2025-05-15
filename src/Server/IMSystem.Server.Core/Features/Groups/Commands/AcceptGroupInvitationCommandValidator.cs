using FluentValidation;
using System;

namespace IMSystem.Server.Core.Features.Groups.Commands;

public class AcceptGroupInvitationCommandValidator : AbstractValidator<AcceptGroupInvitationCommand>
{
    public AcceptGroupInvitationCommandValidator()
    {
        RuleFor(x => x.InvitationId)
            .NotEmpty().WithMessage("邀请ID不能为空。")
            .NotEqual(Guid.Empty).WithMessage("邀请ID格式无效。");

        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("用户ID不能为空。")
            .NotEqual(Guid.Empty).WithMessage("用户ID格式无效。");
    }
}