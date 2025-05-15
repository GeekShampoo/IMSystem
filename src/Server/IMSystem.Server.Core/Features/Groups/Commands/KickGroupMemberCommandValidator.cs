using FluentValidation;
using System;

namespace IMSystem.Server.Core.Features.Groups.Commands;

public class KickGroupMemberCommandValidator : AbstractValidator<KickGroupMemberCommand>
{
    public KickGroupMemberCommandValidator()
    {
        RuleFor(x => x.GroupId)
            .NotEmpty().WithMessage("群组ID不能为空。")
            .NotEqual(Guid.Empty).WithMessage("群组ID格式无效。");

        RuleFor(x => x.MemberUserIdToKick)
            .NotEmpty().WithMessage("要踢出的成员用户ID不能为空。")
            .NotEqual(Guid.Empty).WithMessage("要踢出的成员用户ID格式无效。");

        RuleFor(x => x.ActorUserId)
            .NotEmpty().WithMessage("操作用户ID不能为空。")
            .NotEqual(Guid.Empty).WithMessage("操作用户ID格式无效。");

        RuleFor(x => x)
            .Must(command => command.ActorUserId != command.MemberUserIdToKick)
            .WithMessage("不能将自己踢出群组。")
            .When(command => command.ActorUserId != Guid.Empty && command.MemberUserIdToKick != Guid.Empty); // Only apply if both IDs are valid Guids
    }
}