using FluentValidation;
using System;

namespace IMSystem.Server.Core.Features.Groups.Commands;

public class CancelGroupInvitationCommandValidator : AbstractValidator<CancelGroupInvitationCommand>
{
    public CancelGroupInvitationCommandValidator()
    {
        RuleFor(x => x.InvitationId)
            .NotEmpty().WithMessage("邀请ID不能为空。")
            .NotEqual(Guid.Empty).WithMessage("邀请ID格式无效。");

        RuleFor(x => x.CancellerUserId)
            .NotEmpty().WithMessage("取消者用户ID不能为空。")
            .NotEqual(Guid.Empty).WithMessage("取消者用户ID格式无效。");
    }
}